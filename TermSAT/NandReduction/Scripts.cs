using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TermSAT.Common;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction;

public static class Scripts
{
    const int CONCURRENCY_LIMIT = 32;



    /*
     * A formula can be reduced if it is a substitution instance of 
     * a previously generated non-canonical formula.
     * Since new formulas are assembled from previously generated canonical 
     * formulas all subformulas of the given formula are guaranteed
     * to be non-reducible.   
     */
    public static async Task<bool> FormulaCanBeReducedAsync(RuleDatabaseContext ctx, FormulaIndex.NodeContext instanceRecognizer, Formula formula) 
    {
        var searchResults = instanceRecognizer.FindGeneralizationsAsync(formula, maxMatchCount: 1);
        await foreach(var searchResult in searchResults)
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
            var canonicalRecord = await ctx.FindCanonicalByTruthValueAsync(nonCanonicalRecord.TruthValue);
            Formula canonicalFormula = Formula.Parse(canonicalRecord.Text);
            var reducedFormula = canonicalFormula.CreateSubstitutionInstance(substitutions);
            if (formula.CompareTo(reducedFormula) <= 0)
                return false;

            return true;
        }
        return false;
    }



    /// <summary>
    /// Demonstrates that the basic reduction scheme is subsumes all reduction rules of 3 variables or less.  
    /// This is done by using the basic reduction scheme to reduce all non-canonical formulas of 3 variables 
    /// or less to their canonical formula.
    /// </summary>
    public static async Task<bool> DiscoverRulesSubsumedBySchemeAsync(FormulaDatabase formulaDatabase)
    {
        bool isEquivalent = true;
        foreach (var formula in formulaDatabase.GetAllNonCanonicalFormulas())
        {
            TruthTable truthTable = TruthTable.GetTruthTable(formula);
            var canonicalFormulas = formulaDatabase.GetCanonicalFormulas(truthTable);
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
                    await formulaDatabase.IsSubsumedBySchemeAsync(formula, "yes");
                    Trace.WriteLine($"nand reduction reduces {formula} to canonical form: {canonicalFormula}");
                }
                else
                {
                    isEquivalent = false;
                    await formulaDatabase.IsSubsumedBySchemeAsync(formula, "");
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
                await formulaDatabase.IsSubsumedBySchemeAsync(formula, "error");
                Trace.WriteLine($"error: {ex.Message}, {formula}");
            }
        }

        return isEquivalent;
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

        var ruleOptions = 
            new DbContextOptionsBuilder()
            //.UseSqlite("Data Source=file:rules?mode=memory&cache=shared;Pooling=False;")
            .UseSqlite("Data Source=file:rules?mode=memory&cache=shared")
            .Options;

        DbContextOptions idxOptions = 
            new DbContextOptionsBuilder()
            .UseSqlite("Data Source=file:index?mode=memory&cache=shared")
            .Options;

        using (var ruleDb = new RuleDatabaseContext(ruleOptions))
        using (var instanceRecognizer = new FormulaIndex.NodeContext(idxOptions))
        {
            {   // initialization
                //ruleDb.Database.ExecuteSqlRaw($"PRAGMA cache_size = 1000000;"); // 2000 is the default, ~8mb.  1000000 is ~4gb
                //ruleDb.Database.ExecuteSqlRaw($"PRAGMA synchronous = OFF;"); // https://stackoverflow.com/questions/1711631/improve-insert-per-second-performance-of-sqlite

                //// https://news.ycombinator.com/item?id=35547819
                ruleDb.Database.ExecuteSqlRaw($"PRAGMA journal_mode = WAL;");
                //ruleDb.Database.ExecuteSqlRaw($"PRAGMA synchronous = normal;");
                //ruleDb.Database.ExecuteSqlRaw($"PRAGMA temp_store = memory;");
                //ruleDb.Database.ExecuteSqlRaw($"PRAGMA mmap_size = 30000000000;");
                //ruleDb.Database.ExecuteSqlRaw($"PRAGMA cache_size = 1000000;"); // 2000 is the default, ~8mb.  1000000 is ~4gb

                if (!ruleDb.Database.EnsureCreated())
                {
                    throw new TermSatException("!ruleDb.Database.EnsureCreated()");
                }
                ruleDb.DeleteAll();

                if (ruleDb.FormulaRecords.Any())
                {
                    lastFormulaId = ruleDb.FormulaRecords.OrderByDescending(_ => _.Id).First().Id;
                }


                instanceRecognizer.Database.ExecuteSqlRaw($"PRAGMA journal_mode = WAL;");

                //instanceRecognizer.Database.ExecuteSqlRaw($"PRAGMA cache_size = 1000000"); // 2000 is the default, ~8mb.  1000000 is ~4gb
                //instanceRecognizer.Database.ExecuteSqlRaw($"PRAGMA synchronous = OFF"); // https://stackoverflow.com/questions/1711631/improve-insert-per-second-performance-of-sqlite
                if (!instanceRecognizer.Database.EnsureCreated())
                {
                    throw new TermSatException("!instanceRecognizer.Database.EnsureCreated()");
                }
                instanceRecognizer.DeleteAll();


                // prime the prefix tree used to recognize substitution instances of reducible formulas.
                {
                    var allNonCanonical = ruleDb.FormulaRecords.Where(_ => _.IsCanonical == false);
                    foreach (var nonCanonical in allNonCanonical)
                    {
                        await instanceRecognizer.AddGeneralizationAsync(nonCanonical);
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
                    Trace.WriteLine($"Start by adding a new variable, {variableNumber}.");

                    var variableFormula = Variable.NewVariable(variableNumber);
                    var varRecord = ruleDb.FormulaRecords.Where(_ => _.Text == variableFormula.ToString()).FirstOrDefault();
                    if (varRecord != null)
                    {
                        Trace.WriteLine($"Skipping, variable already exists: {variableFormula}");
                    }
                    else
                    {
                        var variableForumulaId = Interlocked.Increment(ref lastFormulaId);
                        ruleDb.FormulaRecords.Add(new FormulaRecord(variableForumulaId, variableFormula, isCanonical: true)
                        {
                            // todo rename VarCount to VarNum
                            VarCount = variableNumber // its more convenient to just make all formulas with the variable # instead of the count
                        });
                        ruleDb.SaveChanges();
                        ruleDb.Clear();
                        Trace.WriteLine($"New variable added: {variableFormula}");
                    }
                }

                Trace.WriteLine($"Now, generate all possible formulas with exactly {variableNumber} variables...");
                {
                    bool gotoNextTotalLength = true;
                    for (int iTotalLength = 3; gotoNextTotalLength; iTotalLength += 2)
                    {
                        gotoNextTotalLength = false;

                        Trace.WriteLine($"Generate all possible formulas of length {iTotalLength}...");

                        for (int iInnerLength = 1; iInnerLength < iTotalLength; iInnerLength += 2)
                        {

                            // select canonical formulas derived from the current variable, in formula order
                            var innerFormulas = ruleDb.FormulaRecords
                                .Where(_ => _.Length == iInnerLength && _.IsCanonical == true && _.VarCount == variableNumber)
                                .OrderBy(_ => _.Text)
                                .Select(_ => Formula.Parse(_.Text));

                            var iOuterLength = iTotalLength - iInnerLength - 1;

                            // select all canonical formulas in the database, in formula order
                            var outerFormulas = ruleDb.FormulaRecords
                                .Where(_ => _.Length == iOuterLength && _.IsCanonical == true)
                                .OrderBy(_ => _.Text)
                                .Select(_ => Formula.Parse(_.Text));


                            //allFormulas.Sort((a, b) => a.CompareTo(b));

                            // This block is asynchronous.
                            // For every non-reducible formula in the list, this method adds the formula to the rule database.
                            // We can do this asynchronously because all the formulas are the same length, and therefore 
                            // we know that none of them will be reduced by any of the other formulas in the list.
                            // Therefore they don't need to be processed sequentially.
                            //
                            // These formulas used to be processed sequentially, in order.
                            // One side-effect of the change is that Ids do not match formula order.
                            // That's OK though, formula order does not depend on the Id.  
                            // Now, the Id just represents the order in which the formula was added to the database.  
                            // It should be possible to write a script that verifies that every formulas' Id is greater than 
                            // the Id of any formula that can reduce the formula.
                            //
                            // For performance reasons, 
                            var tasks = new List<Task>();
                            foreach (var pair in SystemExtensions.CartesianProduct(innerFormulas, outerFormulas))
                            {
                                //while (CONCURRENCY_LIMIT < tasks.Count)
                                //{
                                //    await tasks[0];
                                //    tasks.RemoveAt(0);
                                //}
                                var derivedFormula = Nand.NewNand(pair.Item1, pair.Item2);

                                var action = async () =>
                                {
                                    using (var _ruleDb = new RuleDatabaseContext(ruleOptions))
                                    using (var _indexDb = new FormulaIndex.NodeContext(idxOptions))
                                    {
                                        try
                                        {
                                            // No need to complete formulas that can already be reduced.
                                            var isReducible = await Scripts.FormulaCanBeReducedAsync(_ruleDb, _indexDb, derivedFormula);

                                            if (isReducible)
                                            {
                                                Trace.WriteLine($"Not added to db, can be reduced : {derivedFormula}");
                                            }
                                            else
                                            {
                                                Formula canonicalFormula = null;
                                                {
                                                    var tt = TruthTable.GetTruthTable(derivedFormula);

                                                    var canonicalRecord = await _ruleDb.FindCanonicalByTruthValueAsync(tt.ToString());

                                                    if (canonicalRecord != null)
                                                    {
                                                        canonicalFormula = Formula.Parse(canonicalRecord.Text);
                                                    }
                                                }

                                                var nextFormulaId = Interlocked.Increment(ref lastFormulaId);
                                                var nextRecord = new FormulaRecord(nextFormulaId, derivedFormula, isCanonical: canonicalFormula == null);

                                                await _ruleDb.FormulaRecords.AddAsync(nextRecord);
                                                await _ruleDb.SaveChangesAsync();


                                                if (nextRecord.IsCanonical)
                                                {
                                                    Trace.WriteLine($"Added canonical formula         : {derivedFormula}");
                                                }
                                                else
                                                {
                                                    // going forward, we'll prevent substitution instances of this formula from becoming new rules.
                                                    await _indexDb.AddGeneralizationAsync(nextRecord);
                                                    await _indexDb.SaveChangesAsync();

                                                    Trace.WriteLine($"Added non-canonical formula     : {derivedFormula} => {canonicalFormula}");
                                                }

                                                gotoNextTotalLength = true;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Trace.TraceError($"FATAL ERROR: {ex.Message}\n{ex.StackTrace}");
                                        }
                                    }
                                };
                                var task = Task.Run(action);
                                tasks.Add(task);
                            }
                            await Task.WhenAll(tasks);
                        }
                    }
                }
            }
        }
    }

    
}
