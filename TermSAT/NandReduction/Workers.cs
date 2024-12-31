using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction;

/// <summary>
/// This class is a collection of methods that implement the worker set described in doc/rule-database.md.  
/// Each method in this class will loop and continuously process formulas until the task is cancelled.  
/// 
/// The workers are all controlled by a *master*.
/// The master is started by calling RunBuildMaster.
/// Only one process should call RunBuildMaster.
/// 
/// There is a corresponding method named StartAllWorkers that is meant to be used by processes that 
/// host a pool of workers.
/// 
/// </summary>
public static class Workers
{
    public interface Build
    {
        static readonly string VARIABLE_NUMBER_KEY = $"{typeof(Build).FullName}.{nameof(Build.VariableNumber)}";
        static readonly string FORMULA_LENGTH_KEY = $"{typeof(Build).FullName}.{nameof(Build.FormulaLength)}";
        public string VariableNumber { get; set; }
        public string FormulaLength { get; set; }
    }


    public static async Task RunBuildMaster(CancellationToken cancellationToken, RuleDatabaseContext ctx)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // first, we have to wait for previous variable to complete
            var hasUncompleted = await ctx.FormulaRecords.Where(_ => _.Subsumed == 0 && _.Evaluated == 1 && _.Closed < 0).AnyAsync();
            if (hasUncompleted)
            {
                var msg = $"Waiting for all canonical formulas to be closed.";
                Trace.WriteLine(msg);
                await Task.Delay(10 * 1000);
                continue;
            }

            await IncrementVariable(cancellationToken, ctx);

            await Close(cancellationToken, ctx);

            using (var transaction = await ctx.Database.BeginTransactionAsync())
            {

                var sql = $"SELECT * FROM {nameof(ctx.FormulaRecords)} FOR UPDATE SKIP LOCKED ";
                var locked = ctx.FormulaRecords.FromSql($"{sql}")
                    .InFormulaOrder()
                    .Where(_ => _.Evaluated < 0)
                    .Take(100)
                    .AsAsyncEnumerable();


                var metaVariableNumber = await ctx.Meta
                    .Where(_ => _.Key == Build.VARIABLE_NUMBER_KEY)
                    .FirstOrDefaultAsync();
                var metaFormulaLength = await ctx.Meta
                    .Where(_ => _.Key == Build.FORMULA_LENGTH_KEY)
                    .FirstOrDefaultAsync();


                await foreach (var record in locked)
                {
                    await ctx.AddGeneralizationAsync(record);
                    record.Indexed = 1;
                }

                await ctx.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            ctx.ChangeTracker.Clear();
        }

    }


    /// <summary>
    /// Add another variable to the system.
    /// The variable is not closed, other workers will compute its closure.
    /// 
    /// The build variable, in the Meta table, is incremented and the formula length is set to 3.
    /// 
    /// All the formulas in the database need to be closed in order to increment the build variable.
    /// If they're not then you'll get an error message.
    /// </summary>
    public static async Task IncrementVariable(CancellationToken cancellationToken, RuleDatabaseContext ctx)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using (var transaction = await ctx.Database.BeginTransactionAsync())
            {
                var unfinishedCanonical = await ctx.FormulaRecords
                .Where(_ => _.Subsumed == 0 && _.TruthValue != null && _.Closed < 0)
                .FirstOrDefaultAsync();

                if (unfinishedCanonical != null)
                {
                    var msg = $"Waiting until all canonical formulas have been closed: ({unfinishedCanonical.Id}){unfinishedCanonical.Text}";
                    Trace.WriteLine(msg);
                    await Task.Delay(10 * 1000);
                }
                else
                {
                    var metaVariableNumber = await ctx.Meta
                        .Where(_ => _.Key == Build.VARIABLE_NUMBER_KEY)
                        .FirstOrDefaultAsync();
                    var metaFormulaLength = await ctx.Meta
                        .Where(_ => _.Key == Build.FORMULA_LENGTH_KEY)
                        .FirstOrDefaultAsync();

                    var nextVariableNumber = 1;
                    if (metaVariableNumber == null)
                    {
                        metaVariableNumber = new(Build.VARIABLE_NUMBER_KEY, $"1");
                        await ctx.AddAsync(metaVariableNumber);
                    }
                    else
                    {
                        nextVariableNumber = int.Parse(metaVariableNumber.Value) + 1;
                        metaVariableNumber.Value = $"{nextVariableNumber}";
                    }

                    var nextVariable = Variable.NewVariable(nextVariableNumber);
                    var nextRecord = new FormulaRecord(id: 0, nextVariable, nextVariableNumber, TruthTable.GetTruthTable(nextVariable).ToString());
                    await ctx.FormulaRecords.AddAsync(nextRecord);
                    metaFormulaLength.Value = "3";

                    await ctx.SaveChangesAsync();
                    await transaction.CommitAsync();

                    Trace.WriteLine($"Increment build variable to : {nextVariable}");
                    Trace.WriteLine($"Build formula length set to 3");
                }
            }
        }
    }

    /// <summary>
    /// For formulas where Subsumed == -1 (undefined or NULL)
    ///     Set Subsumed = 1 (true) for formulas that are reducible by 
    ///     a non-canonical formula that appears earlier in the formula ordering.  
    ///     Otherwise set Subsumed = 0 (false).
    ///     
    /// This should be the first worker to process a formula after its added, 
    /// because if a formula is subsumed then we can ignore it, 
    /// and we definitely don't want to generate any new formulas from it.  
    /// 
    /// This is a standard step in the [Knuth-Bendix completion method](https://en.wikipedia.org/wiki/Knuth%E2%80%93Bendix_completion_algorithm).  
    /// </summary>
    public static async Task Subsume(CancellationToken cancellationToken, RuleDatabaseContext ctx)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using (var transaction = await ctx.Database.BeginTransactionAsync())
            {
                // probably only works with PostgreSQL, I see no reason to care
                var sql = $"SELECT * FROM {nameof(ctx.FormulaRecords)} FOR UPDATE SKIP LOCKED "; 
                var locked = ctx.FormulaRecords.FromSql($"{sql}")
                    .Where(_ => _.Subsumed < 0)
                    .InFormulaOrder()
                    .Take(100)
                    .AsAsyncEnumerable();

                await foreach (var record in locked)
                {
                    var formula = Formula.Parse(record.Text);


                    // A formula can be reduced if it is a substitution instance of
                    // a previously generated non-canonical formula.
                    // Note that, since new formulas are assembled from previously generated canonical formulas, 
                    // all sub-formulas of the given formula are guaranteed to be non-reducible.
                    var searchResults = ctx.Lookup.FindGeneralizationsAsync(formula, maxMatchCount: 1);
                    await foreach (var searchResult in searchResults)
                    {
                        // This check is required because applying reduction rules without checking that formulas
                        // respect the formula order doesn't necessarily produce shorter formulas.  
                        // That is, a rule like |.2|.1.2 => |.2|.1.1 doesn't produce a shorter formula if you use formulas where .1 > .2.
                        // Instead of only applying the rule when the formulas respect the formula order,
                        // Its easier (I think) to just apply the rule and then confirm that the result is reduced.  
                        var substitutions = new Dictionary<Variable, Formula>();
                        foreach (var substitution in searchResult.Substitutions)
                        {
                            substitutions.Add(Variable.NewVariable(substitution.Key), substitution.Value);
                        }
                        var nonCanonicalRecord = await ctx.FindByIdAsync(searchResult.Node.Value);
                        var canonicalRecord = await ctx.FormulaRecords.GetCanonicalRecordByTruthTable(nonCanonicalRecord.TruthValue).FirstAsync();
                        Formula canonicalFormula = Formula.Parse(canonicalRecord.Text);
                        var reducedFormula = canonicalFormula.CreateSubstitutionInstance(substitutions);
                        record.Subsumed = (formula.CompareTo(reducedFormula) <= 0) ? 0 : 1;
                    }
                }

                await ctx.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            ctx.ChangeTracker.Clear();
        }
    }


    /// <summary>
    /// Calculate TruthValue for all formulas.
    /// </summary>
    public static async Task Evaluate(CancellationToken cancellationToken, RuleDatabaseContext ctx)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using (var transaction = await ctx.Database.BeginTransactionAsync())
            {
                // probably only works with PostgreSQL, I see no reason to care
                var sql = $"SELECT * FROM {nameof(ctx.FormulaRecords)} FOR UPDATE SKIP LOCKED ";
                var locked = ctx.FormulaRecords.FromSql($"{sql}")
                    .InFormulaOrder()
                    .Where(_ => _.Evaluated < 0)
                    .Take(100)
                    .AsAsyncEnumerable();

                await foreach (var record in locked)
                {
                    var formula = Formula.Parse(record.Text);
                    var truthValue = TruthTable.GetTruthTable(formula);
                    record.TruthValue = truthValue.ToString();
                    record.Evaluated = 1;
                }

                await ctx.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            ctx.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Add non-canonical formulas to the Lookup database
    /// </summary>
    public static async Task Index(CancellationToken cancellationToken, RuleDatabaseContext ctx)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using (var transaction = await ctx.Database.BeginTransactionAsync())
            {
                var sql = $"SELECT * FROM {nameof(ctx.FormulaRecords)} FOR UPDATE SKIP LOCKED ";
                var locked = ctx.FormulaRecords.FromSql($"{sql}")
                    .InFormulaOrder()
                    .Where(_ => 0 < _.Evaluated && _.Indexed < 0)
                    .Take(100)
                    .AsAsyncEnumerable();

                await foreach (var record in locked)
                {
                    await ctx.AddGeneralizationAsync(record);
                    record.Indexed = 1;
                }

                await ctx.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            ctx.ChangeTracker.Clear();
        }
    }


    /// <summary>
    /// In the Knuth-Bendix method, *closing* a formula means constructing all possible formulas in the 'closure' of the new formula 
    /// and all the canonical formulas previously added to the database.
    /// 
    /// But this method doesn't do that :-).
    /// That's because we cant know if a formula is canonical until all formulas of the same length or less have been evaluated.  
    /// Therefore, to complete the TermSAT rule database, all formulas of length N must be evaluated before formulas of length N + 1.
    /// 
    /// So, this method generates new formulas, of Length == Meta.Where(BuildProcess.CurrentLength), from existing canonical formulas.  
    /// And BuildProcess.CurrentLength is increased until no new rules are generated.  
    /// This method is equivalent to plain Knuth-Bendix but is more efficient since we can be sure that, for formulas of length N, 
    /// that all non-canonical formulas of length < N have been already indexed.
    /// New formulas are not checked to see if they're reducible before they're inserted into the database.  
    /// Note, new formulas that are reducible will eventually be removed by a Reducer.
    /// A closer...
    /// ...selects FormulaRecords.InFormulaOrder().Where(IsReducible == false && TruthValue != null && IsClosed == null).First();
    /// AND
    /// ...pairs the selected formula with all previous canonical formulas and adds them to the db
    /// ...sets IsClosed to true
    /// 
    /// </summary>
    public static async Task Close(CancellationToken cancellationToken, RuleDatabaseContext ctx)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using (var transaction = await ctx.Database.BeginTransactionAsync())
            {
                // only works with PostgreSQL, I see no reason to care
                var sql = $"SELECT * FROM {nameof(ctx.FormulaRecords)} FOR UPDATE SKIP LOCKED ";
                var locked = ctx.FormulaRecords.FromSql($"{sql}")
                    .InFormulaOrder()
                    .Where(_ => _.Evaluated < 0)
                    .Take(100)
                    .AsAsyncEnumerable();

                await foreach (var record in locked)
                {
                    await ctx.AddGeneralizationAsync(record);
                    record.Indexed = 1;
                }

                await ctx.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            ctx.ChangeTracker.Clear();
        }
    }

}
