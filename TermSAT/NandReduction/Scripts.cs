using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    // postgresql default max connections is 100
    // my home machine has 8 cores and 16 'logical processors'
    // todo: The CPU is maxed out during generation, so I suspect that calculating
    // truth values and/or generating substitution instances more efficiently would improve generation performance 
    const int CONCURRENCY_LIMIT = 64; 
    const int CHUNK_SIZE = 500; // # of records in a single transaction
    const int GC_COUNT = 100; // do a full garbage collect every GC_COUNT chunks




    /// <summary>
    /// Demonstrates that the basic reduction scheme is subsumes all reduction rules of 3 variables or less.  
    /// This is done by using the basic reduction scheme to reduce all non-canonical formulas of 3 variables 
    /// or less to their canonical record.
    /// </summary>
    public static async Task<bool> DiscoverRulesSubsumedBySchemeAsync(string dataSource)
    {
        bool isEquivalent = true;
        try
        {
            using (var generatedRulesDb = ReRiteDbContext.GetDatabaseContext("ruledb"))
            {
                await foreach (var record in generatedRulesDb.Formulas.GetAllNonCanonicalRecords().AsAsyncEnumerable())
                {
                    var truthTable = TruthTable.GetTruthTable(record.Formula).ToString();
                    var canonicalFormulas = await generatedRulesDb.Formulas.GetAllCanonicalRecords().Where(_ => _.TruthValue ==  truthTable).ToListAsync();
                    Debug.Assert(canonicalFormulas.Count == 1, "there is never more than 1 canonical formula");
                    var canonicalFormula = canonicalFormulas[0];

                    Debug.Assert(canonicalFormula.Length <= record.Length, "non-canonical formulas are never shorter than canonical formulas");
                    //if (canonicalFormula.Length == record.Length)
                    //{
                    //    Trace.WriteLine($"Skipping {record}, canonical form: {canonicalFormula}");
                    //    continue;
                    //}

                    try
                    {

                        var reducedRecord = await generatedRulesDb.GetCanonicalRecordAsync(record);
                        Formula reducedFormula = reducedRecord.Formula;

                        if (reducedFormula.Equals(canonicalFormula))
                        {
                            await generatedRulesDb.IsSubsumedBySchemeAsync(record, "yes");
                            Trace.WriteLine($"nand reduction reduces {record} to canonical form: {canonicalFormula}");
                        }
                        else
                        {
                            isEquivalent = false;
                            await generatedRulesDb.IsSubsumedBySchemeAsync(record, "");
                            Trace.WriteLine("nand reduction does not reduce this formula to its canonical form...");
                            Trace.WriteLine("=== non-canonical form ===");
                            Trace.WriteLine(record.ToString());
                            Trace.WriteLine("=== canonical form ===");
                            Trace.WriteLine(canonicalFormula.ToString());

                            //break;
                        }

                    }
                    catch (Exception ex)
                    {
                        await generatedRulesDb.IsSubsumedBySchemeAsync(record, "error");
                        Trace.WriteLine($"error: {ex.Message}, {record}");
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
     * A record can be reduced if it is a substitution instance of 
     * a previously generated non-canonical record.
     * Since new formulas are assembled from previously generated canonical 
     * formulas all subformulas of the given record are guaranteed
     * to be non-reducible.   
     */
    public static async Task<bool> FormulaCanBeReducedAsync(this ReRiteDbContext ctx, Formula formula)
    {
        var searchResults = await ctx.Lookup.FindGeneralizationsAsync(formula, maxMatchCount: 1);
        foreach (var searchResult in searchResults)
        {
            // This check is required because applying reduction rules without checking that formulas
            // respect the record order doesn't necessarily produce shorter formulas.  
            // That is, a rule like |.2|.1.2 => |.2|.1.1 doesn't produce a shorter record if you use formulas where .1 > .2.
            // Instead of only applying the rule when the formulas respect the record order,
            // Its easier (I think) to just apply the rule and then confirm that the result is reduced.  
            var substitutions = new Dictionary<Variable, Formula>();
            foreach (var substitution in searchResult.Substitutions)
            {
                substitutions.Add(Variable.NewVariable(substitution.Key), substitution.Value);
            }
#if DEBUG
            {
                if (searchResult.Node.Value <= 0)
                {
                    throw new TermSatException($"not a valid formula id:{searchResult.Node.Value}");
                }
                if (await ctx.Formulas.FindAsync(searchResult.Node.Value) == null)
                {
                    throw new TermSatException($"not a valid formula id:{searchResult.Node.Value}");
                }
            }
#endif
            Debug.Assert(0 < searchResult.Node.Value, "not a valid formula id");
            var nonCanonicalRecord = await ctx.Formulas.AsNoTracking()
                .Where(_ => _.Id == searchResult.Node.Value)
                .FirstAsync();
            var canonicalRecord = await ctx.Formulas.AsNoTracking()
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
     * The first record in the record order with a particular truth value is canonical, others are non-canonical.  
     * Each record has a 'truth value' that identifies equivalent formulas.
     * Thus, the database is also a table of rewrite rules, 
     * where each non-canonical record is the left side of a rewrite rule, 
     * and the right-side of the rule is the corresponding canonical record.
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
     *      First, the record .1 is added to the database.
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
     * Since all reductions are guaranteed to converge to the same singular canonical record, these rules are locally confluent.  
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
        long lastFormulaId = 0;
        int gcCounter= 0;
#if DEBUG
        var databaseName = "testruledb";
#else
        var databaseName = "ruledb";
#endif
        var ruleOptions =
            new DbContextOptionsBuilder()
            .UseNpgsql($"Server=localhost;Database={databaseName};Port=5432;User Id=postgres;Password=password;Pooling=true;")
            .EnableSensitiveDataLogging()
            //.LogTo(msg => Trace.WriteLine(msg), new[] { DbLoggerCategory.Database.Command.Name })
            //.UseSqlite("Data Source=file:rules?mode=memory&cache=shared;Pooling=False;")
            //.UseSqlite("Data Source=file:rules?mode=memory&cache=shared")
            .Options;

        // if the db is populated then try to pickup where it left off
        int startingVariable = 1;
        int startingLength = 3;

        {   // initialization
            using (var ruleDb = new ReRiteDbContext(ruleOptions))
            {
                {
#if DEBUG
                    try
                    {
                        await ruleDb.Database.EnsureDeletedAsync();
                    }
                    catch { }
#endif
                }

                int varCount = -1;
                try
                {
                    varCount = await ruleDb.Formulas
                        .OrderByDescending(_ => _.VarCount)
                        .Select(_ => _.VarCount)
                        .FirstOrDefaultAsync();
                }
                catch { }

                if (startingVariable < varCount)
                {
                    startingVariable = varCount;

                    var length = await ruleDb.Formulas
                        .Where(_ => _.VarCount == varCount)
                        .OrderByDescending(_ => _.Length)
                        .Select(_ => _.Length)
                        .FirstOrDefaultAsync();
                    if (startingLength <= length)
                    {
                        startingLength = length + 2; // assuming that last total length was completed
                    }
                }
                else
                {
                    if (!(await ruleDb.Database.EnsureCreatedAsync()))
                    {
                        throw new TermSatException("!ruleDb.Database.EnsureCreatedAsync()");
                    }

                    {
                        await ruleDb.Lookup.AddRootAsync();

                        //await foreach (var nonCanonical in ruleDb.FormulaRecords.AsNoTracking().GetAllNonCanonicalRecords().AsAsyncEnumerable())
                        //{
                        //    await ruleDb.AddGeneralizationAsync(nonCanonical);
                        //}
                        await ruleDb.SaveChangesAsync();
                    }
                }

                if (await ruleDb.Formulas.AsNoTracking().AnyAsync())
                {
                    lastFormulaId = ruleDb.Formulas.AsNoTracking().OrderByDescending(_ => _.Id).First().Id;
                }
            }
        }


        // Enumerate formulas starting with formulas of just one variable, then two variables, and so on.
        // It's hoped that completing formulas with 7 variables will not produce any new rules,
        // and thus the rules will be complete, in the Knuth-Bendix sense.
        for (int variableNumber = startingVariable; variableNumber <= 7; variableNumber++)
        {
            Trace.WriteLine($"Start generating all rules with exactly {variableNumber} variables.");

            {   // start by adding variable record to database
                using (var ruleDb = new ReRiteDbContext(ruleOptions))
                {
                    Trace.WriteLine($"Start by adding a new variable, {variableNumber}.");

                    var variableFormula = Variable.NewVariable(variableNumber);
                    var varRecord = ruleDb.Formulas.AsNoTracking().Where(_ => _.Text == variableFormula.ToString()).FirstOrDefault();
                    if (varRecord != null)
                    {
                        Trace.WriteLine($"Skipping, variable already exists: {variableFormula}");
                    }
                    else
                    {
                        var record = new ReductionRecord(variableFormula, isCanonical:true)
                        {
                            TruthValue = TruthTable.GetTruthTable(variableFormula).ToString(),
                        };
                        ruleDb.Formulas.Add(record);
                        ruleDb.SaveChanges();
                        ruleDb.ChangeTracker.Clear();
                        Trace.WriteLine($"New variable added: {variableFormula}");
                    }
                }
            }

            Trace.WriteLine($"Now, generate all possible formulas with exactly {variableNumber} variables...");
            {
                bool gotoNextTotalLength = true;
                for (int iTotalLength = startingLength; gotoNextTotalLength; iTotalLength += 2)
                {
                    startingLength = 3; // next time, definitely start at the beginning
                    gotoNextTotalLength = false;

                    GC.Collect();
                    GCSettings.LatencyMode = GCLatencyMode.Batch;

                    Trace.WriteLine($"Generate all possible formulas of length {iTotalLength}...");

                    // start one async task that fills up the todo list with chunks of formulas to process.
                    // Then process all the chunks on multiple threads.
                    var todo = new Queue<Nand[]>();
                    var todoCompleted = false;
                    TaskCompletionSource todoNotifier = null;
                    var generateTask = Task.Run(async () =>
                    {
                        try
                        {
                            using (var ruleDb = new ReRiteDbContext(ruleOptions))
                            {
                                for (int iInnerLength = 1; iInnerLength < iTotalLength; iInnerLength += 2)
                                {
                                    var iOuterLength = iTotalLength - iInnerLength - 1;
                                    Trace.WriteLine($"Generate formulas of length {iTotalLength} with left length of {iInnerLength} and right length of {iOuterLength}");

                                    // select canonical formulas derived from the current variable, in record order
                                    var innerSql = $"SELECT * FROM (" +
                                                        $"SELECT DISTINCT ON (f.\"TruthValue\") f.\"Id\", f.\"VarCount\", f.\"Length\", f.\"TruthValue\", f.\"Text\" " +
                                                        $"FROM \"Reductions\" AS f " +
                                                        $"ORDER BY f.\"TruthValue\", f.\"VarCount\", f.\"Length\", f.\"Text\"" +
                                                   $") AS t " +
                                                   $"WHERE t.\"VarCount\" = {variableNumber} AND t.\"Length\" = {iInnerLength} " + 
                                                   $"ORDER BY t.\"VarCount\", t.\"Length\", t.\"Text\"";
                                    var innerFormulas = await ruleDb.Formulas
                                        .FromSqlRaw(innerSql)
                                        .ToFormulas()
                                        .ToListAsync();

                                    // select all canonical formulas in the database, in record order
                                    var outerSql = $"SELECT * FROM (" +
                                                        $"SELECT DISTINCT ON (f.\"TruthValue\") f.\"Id\", f.\"VarCount\", f.\"Length\", f.\"TruthValue\", f.\"Text\" " +
                                                        $"FROM \"Reductions\" AS f " +
                                                        $"ORDER BY f.\"TruthValue\", f.\"VarCount\", f.\"Length\", f.\"Text\"" +
                                                   $") AS t " +
                                                   $"WHERE t.\"Length\" = {iOuterLength} " + 
                                                   $"ORDER BY t.\"VarCount\", t.\"Length\", t.\"Text\" ";
                                    var outerFormulas = await ruleDb.Formulas
                                        .FromSqlRaw(outerSql)
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
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError($"Fatal error while generating formulas: {ex.Message}\n{ex.StackTrace}");
                            Trace.Flush();
                            Environment.Exit(-1);
                        }
                    });

                    // start processing chunks
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
                                    using (var _ruleDb = new ReRiteDbContext(ruleOptions))
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
                                                var truthValue = TruthTable.GetTruthTable(derivedFormula).ToString();
                                                var nextFormulaId = Interlocked.Increment(ref lastFormulaId);
                                                var nextRecord = new ReductionRecord(derivedFormula, variableNumber, truthValue);

                                                // before adding the record, check to see if a shorter record with the same truth value is already present.
                                                // if so, then this record can be indexed immediately
                                                var proofNotCanonical = await _ruleDb.Formulas
                                                    .Where(_ => _.TruthValue == truthValue && _.Length < iTotalLength)
                                                    .FirstOrDefaultAsync();
                                                var msg = $"Adding formula          : {derivedFormula}";
                                                if (proofNotCanonical != null)
                                                {
                                                    nextRecord.IsIndexed = 1;
                                                    msg = $"Adding non-canonical    : {derivedFormula}";
                                                }
                                                Trace.WriteLine(msg);

                                                await _ruleDb.Formulas.AddAsync(nextRecord);

                                                if (proofNotCanonical != null)
                                                {
                                                    await _ruleDb.AddGeneralizationAsync(nextRecord);
                                                }
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
                                    Trace.Flush();
                                    Environment.Exit(-1);
                                }
                            });
                            chunkTasks.Add(chunkTask);
                        }
                    }

                    await Task.WhenAll(chunkTasks);

                    // At this point all formulas of length iTotalLength have been generated,
                    // reducible formulas have been tossed, and the remainder have been evaluated.  
                    // Now we can determine which are canonical and which are not.
                    // First, we'll select the canonical formulas and mark them as canonical.  
                    // This is a convenience that makes things simpler going forward.
                    // Then we'll index the non-canonical formulas, 
                    // BUT the formulas must be indexed sequentially.  
                    // This is because there will be formulas that are subsumed by formulas of the same length.  
                    Trace.WriteLine($"Mark canonical formulas of length {iTotalLength}...");
                    using (var ruleDb = new ReRiteDbContext(ruleOptions))
                    {
                        var updateCanonicalSql = 
                            $"UPDATE public.\"FormulaRecords\" AS u " +
                            $"SET \"IsCanonical\"=1 " +
                            $"FROM ( " +
                                    $"SELECT * FROM (" +
                                        $"SELECT DISTINCT ON (f.\"TruthValue\") f.\"Id\", f.\"IsCanonical\", f.\"VarCount\", f.\"Length\", f.\"TruthValue\", f.\"Text\" " +
                                        $"FROM \"Reductions\" AS f " +
                                        $"ORDER BY f.\"TruthValue\", f.\"VarCount\", f.\"Length\", f.\"Text\"" +
                                    $") AS t " +
                                    $"WHERE t.\"VarCount\" = {variableNumber} AND t.\"Length\" = {iTotalLength} " +
                            $") AS subquery " +
                            $"WHERE u.\"Id\"=subquery.\"Id\" ";
                        var count = await ruleDb.Database.ExecuteSqlRawAsync(updateCanonicalSql);
                        Trace.WriteLine($"...updated {count} records");
                    }

                    using (var indexingCtx = new ReRiteDbContext(ruleOptions))
                    using (var transaction = indexingCtx.Database.BeginTransaction())
                    {

                        Trace.WriteLine($"Index all remaining un-indexed formulas of length {iTotalLength}...");
                        var unindexedRecords = await indexingCtx.Formulas
                            .WhereCanonical()
                            .Where(_ => _.IsIndexed != 1)
                            .OrderBy(_ => _.VarCount).ThenBy(_ => _.Length).ThenBy(_ => _.Text)
                            .ToListAsync();

                        var indexTasks = new List<Task>();
                        foreach (var record in unindexedRecords)
                        {
                            // No need to index formulas that can already be reduced.
                            var isReducible = await indexingCtx.FormulaCanBeReducedAsync(record.Formula);
                            record.IsIndexed = 0;

                            if (!isReducible)
                            {
                                record.IsIndexed = 1;
                                await indexingCtx.AddGeneralizationAsync(record);
                                await indexingCtx.SaveChangesAsync();
                            }
                        }
                        transaction.Commit();
                    }

                    Trace.WriteLine($"Finally, delete any un-indexed, non-canonical formulas still remaining...");
                    using (var ruleDb = new ReRiteDbContext(ruleOptions))
                    {
                        var deleteSql =
                            $"DELETE FROM public.\"FormulaRecords\" AS d " +
                            $"WHERE d.\"IsCanonical\" != 1 AND d.\"IsIndexed\" == -1 ";
                        var deleteCount = await ruleDb.Database.ExecuteSqlRawAsync(deleteSql);
                        Trace.WriteLine($"...deleted {deleteCount} records");
                    }


                }
            }
        }
    }
}
