using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using TermSAT.Common;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction;

public static class Scripts
{
    const int CONCURRENCY_LIMIT = 32; // by comparison, the postgresql default max connections is 100
    const int CHUNK_SIZE = 128; // # of records in a single transaction
    const int GC_COUNT = 500; // do a full garbage collect every GC_COUNT chunks




    /// <summary>
    /// Demonstrates that the basic reduction scheme is subsumes all reduction rules of 3 variables or less.  
    /// This is done by using the basic reduction scheme to reduce all non-canonical formulas of 3 variables 
    /// or less to their canonical formula.
    /// </summary>
    public static async Task<bool> DiscoverRulesSubsumedBySchemeAsync(string dataSource)
    {
        bool isEquivalent = true;
        try
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(dataSource))
            {
                await foreach (var formula in ctx.FormulaRecords.GetAllNonCanonicalFormulas().AsAsyncEnumerable())
                {
                    var truthTable = TruthTable.GetTruthTable(formula).ToString();
                    var canonicalFormulas = await ctx.FormulaRecords.GetAllCanonicalRecords().Where(_ => _.TruthValue ==  truthTable).ToListAsync();
                    Debug.Assert(canonicalFormulas.Count == 1, "there is never more than 1 canonical formula");
                    var canonicalFormula = canonicalFormulas[0];

                    Debug.Assert(canonicalFormula.Length <= formula.Length, "non-canonical formulas are never shorter than canonical formulas");
                    //if (canonicalFormula.Length == formula.Length)
                    //{
                    //    Trace.WriteLine($"Skipping {formula}, canonical form: {canonicalFormula}");
                    //    continue;
                    //}

                    try
                    {

                        Formula reducedFormula = formula.Reduce();

                        if (reducedFormula.Equals(canonicalFormula))
                        {
                            await ctx.IsSubsumedBySchemeAsync(formula, "yes");
                            Trace.WriteLine($"nand reduction reduces {formula} to canonical form: {canonicalFormula}");
                        }
                        else
                        {
                            isEquivalent = false;
                            await ctx.IsSubsumedBySchemeAsync(formula, "");
                            Trace.WriteLine("nand reduction does not reduce this formula to its canonical form...");
                            Trace.WriteLine("=== non-canonical form ===");
                            Trace.WriteLine(formula.ToString());
                            Trace.WriteLine("=== canonical form ===");
                            Trace.WriteLine(canonicalFormula.ToString());

                            //break;
                        }

                    }
                    catch (Exception ex)
                    {
                        await ctx.IsSubsumedBySchemeAsync(formula, "error");
                        Trace.WriteLine($"error: {ex.Message}, {formula}");
                    }
                }
            }
        }
        catch
        {
            isEquivalent = false;
        }

        return isEquivalent;
    }


    /*
     * A formula can be reduced if it is a substitution instance of 
     * a previously generated non-canonical formula.
     * Since new formulas are assembled from previously generated canonical 
     * formulas all subformulas of the given formula are guaranteed
     * to be non-reducible.   
     */
    public static async Task<bool> FormulaCanBeReducedAsync(this RuleDatabaseContext ctx, Formula formula)
    {
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
            var nonCanonicalRecord = await ctx.FormulaRecords.AsNoTracking()
                .Where(_ => _.Id == searchResult.Node.Value)
                .FirstAsync();
            var canonicalRecord = await ctx.FormulaRecords.AsNoTracking()
                .OrderBy(_ => _.VarCount).ThenBy(_ => _.Length).ThenBy(_ => _.Text)
                .Where(_ => _.TruthValue == nonCanonicalRecord.TruthValue)
                .FirstAsync();

            Formula canonicalFormula = Formula.GetOrParse(canonicalRecord.Text);
            var reducedFormula = canonicalFormula.CreateSubstitutionInstance(substitutions);
            if (formula.CompareTo(reducedFormula) <= 0)
                return false;

            return true;
        }
        return false;
    }

    /**
     * This method generates a database of rewrite 'rules' for reducing TermSAT formulas.  
     * Its really just a table of formulas broken down into canonical and non-canonical formulas.
     * The first formula in the formula order with a particular truth value is canonical, others are non-canonical.  
     * Each formula has a 'truth value' that identifies equivalent formulas.
     * Thus, the database is also a table of rewrite rules, 
     * where each non-canonical formula is the left side of a rewrite rule, 
     * and the right-side of the rule is the corresponding canonical formula.
     * 
     * This method takes a different approach than Scripts_RuleGenerator_KnuthBendix.RunNandRuleGenerator.
     * Instead of constructing the closure of all formulas as they're added to the database of rules, this method calculates 
     * all possible rules directly.
     * This method...
     *  - First, all rules for all formulas of Length == 1 are added to the database and marked as completed, canonical formulas.
     *  - Then, all all formulas of Length == 2 are calculated from the previously completed, canonical formulas are generated.
     *  - And so on, until no more formulas are added to the database.
     *  This method calculates these formulas very efficiently, its much less work than looking for formulas 
     *  in the enumeration of the transitive closure of a variable.
     * 
     * First, all formulas with just one variable are constructed.  
     *      First, the formula .1 is added to the database.
     *      Then .1 is closed, resulting |.1.1 being added to the database. 
     *      Then |.1.1 is closed, resulting in |.1|.1.1, ||.1.1.1, and ||.1.1|.1.1 being added to the database.
     *      And so on...
     * Then two variables, then three, and so on, until adding a new variable results in no new formulas/rules being added to the database.  
     *  
     *  
     * Conceptually, I consider this method to be a homegrown implementation of the [Knuth-Bendix completion method](https://en.wikipedia.org/wiki/Knuth%E2%80%93Bendix_completion_algorithm#:~:text=The%20Knuth%E2%80%93Bendix%20completion%20algorithm,problem%20for%20the%20specified%20algebra.).  
     * The idea behind both is to calculate the *deductive closure* of a set of rules.  
     * The difference being that this method is specifically designed for TermSAT formulas 
     * and calculates the closure much more efficiently than unoptimized Knuth-Bendix.
     * The database produced by this method contains all the formulas included in the deductive closure. 
     * 
     * Notes...   
     * Since all reductions are guaranteed to converge to the same singular canonical formula, these rules are locally confluent.  
     * The closure process guarantees that these rules are also globally confluent.
     * 
     * As I write this, I have not run this method to termination yet, I hope to, though its proving to be difficult.  
     * I expect it to stop producing new rules after 6 variables.  
     * But actually running this method over 7 variables seems like too tough of a challenge right now.
     * However, showing that 
     */

    [TestMethod]
    public static async Task RunNandRuleGenerator(string formulaDataSource, string indexDataSource)
    {
        int lastFormulaId = 0;
        int gcCounter= 0;

        var ruleOptions =
            new DbContextOptionsBuilder()
            .UseNpgsql("Server=localhost;Database=ruledb;Port=5432;User Id=postgres;Password=password;Pooling=true;")
            //.UseSqlite("Data Source=file:rules?mode=memory&cache=shared;Pooling=False;")
            //.UseSqlite("Data Source=file:rules?mode=memory&cache=shared")
            .Options;


        {   // initialization
            using (var ruleDb = new RuleDatabaseContext(ruleOptions))
            {
                //ruleDb.Database.ExecuteSqlRaw($"PRAGMA cache_size = 1000000;"); // 2000 is the default, ~8mb.  1000000 is ~4gb
                //ruleDb.Database.ExecuteSqlRaw($"PRAGMA synchronous = OFF;"); // https://stackoverflow.com/questions/1711631/improve-insert-per-second-performance-of-sqlite

                ////// https://news.ycombinator.com/item?id=35547819
                //ruleDb.Database.ExecuteSqlRaw($"PRAGMA journal_mode = WAL;");
                ////ruleDb.Database.ExecuteSqlRaw($"PRAGMA synchronous = normal;");
                ////ruleDb.Database.ExecuteSqlRaw($"PRAGMA temp_store = memory;");
                ////ruleDb.Database.ExecuteSqlRaw($"PRAGMA mmap_size = 30000000000;");
                ////ruleDb.Database.ExecuteSqlRaw($"PRAGMA cache_size = 1000000;"); // 2000 is the default, ~8mb.  1000000 is ~4gb

                if (!(await ruleDb.Database.EnsureDeletedAsync()))
                {
                    throw new TermSatException("!ruleDb.Database.EnsureDeletedAsync()");
                }

                if (!(await ruleDb.Database.EnsureCreatedAsync()))
                {
                    throw new TermSatException("!ruleDb.Database.EnsureCreatedAsync()");
                }

                if (await ruleDb.FormulaRecords.AsNoTracking().AnyAsync())
                {
                    lastFormulaId = ruleDb.FormulaRecords.AsNoTracking().OrderByDescending(_ => _.Id).First().Id;
                }


                // prime the prefix tree used to recognize substitution instances of reducible formulas.
                {
                    await ruleDb.Lookup.AddRootAsync();

                    await foreach (var nonCanonical in ruleDb.FormulaRecords.AsNoTracking().GetAllNonCanonicalRecords().AsAsyncEnumerable())
                    {
                        await ruleDb.AddGeneralizationAsync(nonCanonical);
                    }
                    await ruleDb.SaveChangesAsync();
                }
            }
        }


        // Enumerate formulas starting with formulas of just one variable, then two variables, and so on.
        // It's hoped that completing formulas with 7 variables will not produce any new rules,
        // and thus the rules will be complete, in the Knuth-Bendix sense.
        for (int variableNumber = 1; variableNumber <= 7; variableNumber++)
        {
            Trace.WriteLine($"Start generating all rules with exactly {variableNumber} variables.");

            {   // start by adding variable formula to database
                using (var ruleDb = new RuleDatabaseContext(ruleOptions))
                {
                    Trace.WriteLine($"Start by adding a new variable, {variableNumber}.");

                    var variableFormula = Variable.NewVariable(variableNumber);
                    var varRecord = ruleDb.FormulaRecords.AsNoTracking().Where(_ => _.Text == variableFormula.ToString()).FirstOrDefault();
                    if (varRecord != null)
                    {
                        Trace.WriteLine($"Skipping, variable already exists: {variableFormula}");
                    }
                    else
                    {
                        var variableForumulaId = Interlocked.Increment(ref lastFormulaId);
                        var record = new FormulaRecord(variableForumulaId, variableFormula, variableNumber, TruthTable.GetTruthTable(variableFormula).ToString());
                        ruleDb.FormulaRecords.Add(record);
                        ruleDb.SaveChanges();
                        ruleDb.ChangeTracker.Clear();
                        Trace.WriteLine($"New variable added: {variableFormula}");
                    }
                }
            }

            Trace.WriteLine($"Now, generate all possible formulas with exactly {variableNumber} variables...");
            {
                bool gotoNextTotalLength = true;
                for (int iTotalLength = 3; gotoNextTotalLength; iTotalLength += 2)
                {
                    GC.Collect();
                    GCSettings.LatencyMode = GCLatencyMode.Batch;

                    gotoNextTotalLength = false;

                    Trace.WriteLine($"Generate all possible formulas of length {iTotalLength}...");

                    // start one async task that fills up the todo list with chunks of formulas to process.
                    // Then process all the chunks on multiple threads.
                    var todo = new Queue<Nand[]>();
                    var todoCompleted = false;
                    TaskCompletionSource todoNotifier = null;
                    var generateTask = Task.Run(async () =>
                    {
                        using (var ruleDb = new RuleDatabaseContext(ruleOptions))
                        {
                            for (int iInnerLength = 1; iInnerLength < iTotalLength; iInnerLength += 2)
                            {

                                // select canonical formulas derived from the current variable, in formula order
                                var innerFormulas = await ruleDb.FormulaRecords.AsNoTracking()
                                    .Select(_ => _.TruthValue)
                                    .Distinct()
                                    .Select(t =>
                                        ruleDb.FormulaRecords.AsNoTracking()
                                        .OrderBy(_ => _.VarCount).ThenBy(_ => _.Length).ThenBy(_ => _.Text)
                                        .Where(_ => _.TruthValue == t)
                                        .First())
                                    .Where(_ => _.Length == iInnerLength && _.VarCount == variableNumber)
                                    .ToFormulas()
                                    .ToListAsync();

                                var iOuterLength = iTotalLength - iInnerLength - 1;

                                // select all canonical formulas in the database, in formula order
                                var outerFormulas = await ruleDb.FormulaRecords.AsNoTracking()
                                    .Select(_ => _.TruthValue)
                                    .Distinct()
                                    .Select(t =>
                                        ruleDb.FormulaRecords.AsNoTracking()
                                        .OrderBy(_ => _.VarCount).ThenBy(_ => _.Length).ThenBy(_ => _.Text)
                                        .Where(_ => _.TruthValue == t)
                                        .First())
                                    .Where(_ => _.Length == iOuterLength)
                                    .ToFormulas()
                                    .ToListAsync();


                                var chunks = SystemExtensions.CartesianProduct(innerFormulas, outerFormulas)
                                    .Select(_ => Nand.NewNand(_.Item1, _.Item2))
                                    .Chunk(CHUNK_SIZE);

                                lock (todo)
                                {
                                    foreach (var chunk in chunks)
                                    {
                                        todo.Enqueue(chunk);
                                        if (todoNotifier != null)
                                        {
                                            todoNotifier.SetResult();
                                        }
                                    }
                                }
                            }
                        }
                        lock (todo)
                        {
                            todoCompleted = true;
                            if (todoNotifier != null)
                            {
                                todoNotifier.SetResult();
                            }
                        }
                    });

                    var chunkTasks = new List<Task>();
                    while (true)
                    {
                        while (CONCURRENCY_LIMIT < chunkTasks.Count)
                        {
                            Trace.Write($".");
                            await chunkTasks[0];
                            chunkTasks.RemoveAt(0);
                        }


                        Nand[] nextChunk = null;
                        lock (todo)
                        {
                            if (!todo.Any())
                            {
                                if (todoCompleted)
                                {
                                    break;
                                }
                                todoNotifier = new();
                            }
                            else
                            {
                                nextChunk = todo.Dequeue();
                            }
                        }
                        if (todoNotifier != null)
                        {
                            await todoNotifier.Task;
                            todoNotifier = null;
                        }
                        if (nextChunk != null)
                        {
                            if ((++gcCounter % GC_COUNT) == 0)
                            {
                                GC.Collect();
                            }


                            Nand[] _nextChunk = nextChunk;
                            var chunkTask = Task.Run(async () =>
                            {
                                try
                                {
                                    using (var _ruleDb = new RuleDatabaseContext(ruleOptions))
                                    using (var transaction = _ruleDb.Database.BeginTransaction())
                                    {
                                        foreach (var derivedFormula in _nextChunk)
                                        {
                                            // No need to complete formulas that can already be reduced.
                                            var isReducible = await _ruleDb.FormulaCanBeReducedAsync(derivedFormula);

                                            if (isReducible)
                                            {
                                                Trace.WriteLine($"Skipped, can be reduced : {derivedFormula}");
                                            }
                                            else
                                            {
                                                var nextFormulaId = Interlocked.Increment(ref lastFormulaId);
                                                var truthValue = TruthTable.GetTruthTable(derivedFormula).ToString();
                                                var nextRecord = new FormulaRecord(nextFormulaId, derivedFormula, variableNumber, truthValue);
                                                Trace.WriteLine($"Adding formula          : {derivedFormula}");
                                                await _ruleDb.FormulaRecords.AddAsync(nextRecord);
                                                await _ruleDb.SaveChangesAsync();

                                                gotoNextTotalLength = true;
                                            }
                                        }
                                        Trace.WriteLine($"Saving chunk...");
                                        await transaction.CommitAsync();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Trace.TraceError($"Fatal error while processing chunk of formulas: {ex.Message}\n{ex.StackTrace}");
                                    Environment.Exit(-1);
                                }
                            });
                            chunkTasks.Add(chunkTask);
                        }
                    }

                    await Task.WhenAll(chunkTasks);

                    // At this point all formulas of length iTotalLength have been generated,
                    // reducible formulas have been tossed, and the remainder have been evaluated.  
                    // Now we can determine which are canonical and which are not and add the non-canonical
                    // formulas to the Lookup index.
                    {
                        Trace.WriteLine($"Index all formulas of length {iTotalLength}...");
                        var indexChunks = Enumerable.Empty<FormulaRecord[]>();
                        var indexTasks = new List<Task>();
                        using (var ruleDb = new RuleDatabaseContext(ruleOptions))
                        {
                            indexChunks = (await ruleDb.FormulaRecords.AsNoTracking()
                                .Where(_ => _.Length == iTotalLength)
                                .OrderBy(_ => _.VarCount).ThenBy(_ => _.Length).ThenBy(_ => _.Text)
                                .Select(_ => _.TruthValue)
                                .Distinct()
                                .SelectMany(t =>
                                    ruleDb.FormulaRecords.AsNoTracking()
                                    .OrderBy(_ => _.VarCount).ThenBy(_ => _.Length).ThenBy(_ => _.Text)
                                    .Where(_ => _.TruthValue == t && _.Length == iTotalLength)
                                    .Skip(1))
                                .ToListAsync())
                                .Chunk(CHUNK_SIZE);
                        }

                        foreach (var indexChunk in indexChunks)
                        {
                            while (CONCURRENCY_LIMIT < indexTasks.Count)
                            {
                                Trace.Write($".");
                                await indexTasks[0];
                                indexTasks.RemoveAt(0);
                            }

                            if ((++gcCounter % GC_COUNT) == 0)
                            {
                                GC.Collect();
                            }

                            var _chunk = indexChunk;
                            var task = Task.Run(async () =>
                            {
                                using (var _ruleDb = new RuleDatabaseContext(ruleOptions))
                                using (var taction = _ruleDb.Database.BeginTransaction())
                                {
                                    foreach (var record in _chunk)
                                    {
                                        await _ruleDb.AddGeneralizationAsync(record);
                                        await _ruleDb.SaveChangesAsync();
                                    }
                                    await taction.CommitAsync();
                                }
                            });
                            indexTasks.Add(task);
                        }
                        Trace.WriteLine($"Wait for {indexTasks.Count} indexing chunks to finish processing...");
                        await Task.WhenAll(indexTasks);
                    }
                }
            }
        }
    }
}
