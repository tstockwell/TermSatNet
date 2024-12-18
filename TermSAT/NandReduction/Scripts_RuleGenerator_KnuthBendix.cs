using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction
{
    internal class Scripts_RuleGenerator_KnuthBendix
    {

        // do this to create memory-based db
        //readonly string DATABASE_PATH = ":memory:"; 

        // Note: I rewrote the rule sieve algorithm from scratch in 2024.
        // The new algorithm is magnitudes of order more efficient.
        //
        //[TestMethod]
        //public void RunNandRuleGenerator()
        //{
        //    var database = new FormulaDatabase(DATABASE_PATH);
        //    database.Clear();
        //    new RuleGenerator(database, new NandFormulaGenerator(database, VARIABLE_COUNT)).Run();
        //}



        /**
         * This method generates a database of rewrite 'rules' for reducing TermSAT formulas.  
         * Each formula has a 'truth value' that identifies equivalent formulas.
         * Formulas are also marked as either canonical of non-canonical.  
         * The formulas are in 'TermSAT order'...
         *  - formulas where the highest numbered variable is X come before formulas where the highest numbered variable is Y, and X < Y
         *  - shorter formulas before longer formulas
         *  - formulas of the same length are sorted lexically.  
         * The first formula in the formula order with a particular truth value is canonical, others are non-canonical.  
         * Thus, the database is also a table of rewrite rules, 
         * where each non-canonical formula is the left side of a rewrite rule, 
         * and the right-side of the rule is the corresponding canonical formula.
         * 
         * The database is constructed by adding new formulas, one by one, and each formula that is added is also 'closed'.  
         * Closing a formula means constructing all possible formulas in the 'closure' of the new formula and all the canonical 
         * formulas previously added to the database.
         * New formulas may only be added to the database if they're not reducible using the rules currently in the database.  
         * 
         * First, all formulas with just one variable are constructed.  
         *      First, the formula .1 is added to the database.
         *      Then .1 is closed, resulting |.1.1 being added to the database. 
         *      Then |.1.1 is closed, resulting in |.1|.1.1, ||.1.1.1, and ||.1.1|.1.1 being added to the database.
         *      And so on...
         * Then two variables, then three, and so on, until adding a new variable results in no new formulas/rules being added to the database.  
         * 
         * When completing a new variable, new formulas are calculated in formula order.  
         * First, all rules for all formulas of Length == 1 are added to the database and marked as completed, canonical formulas.
         * Then, all all formulas of Length == 2 are calculated from the previously completed, canonical formulas are generated.
         * And so on, until no more formulas are added to the database.
         * 
         * Conceptually, I consider this method to be a homegrown implementation of the [Knuth-Bendix completion method](https://en.wikipedia.org/wiki/Knuth%E2%80%93Bendix_completion_algorithm#:~:text=The%20Knuth%E2%80%93Bendix%20completion%20algorithm,problem%20for%20the%20specified%20algebra.).  
         * The idea behind both is to calculate the *deductive closure* of a set of rules.  
         * The difference being that this method is specifically designed for TermSAT formulas 
         * and calculates the closure much more efficiently than unoptimized Knuth-Bendix.
         * The database produced by this method contains all the formulas included in the deductive closure. 
         * 
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
        public static void RunNandRuleGenerator(FormulaDatabase formulaDatabase)
        {
            formulaDatabase.Clear();

            using (var ctx = formulaDatabase.GetDatabaseContext())
            {
                InstanceRecognizer instanceRecognizer = new InstanceRecognizer();
                // prime the prefix tree used to recognize substitution instances of reducible formulas.
                {
                    var allNonCanonical = ctx.FormulaRecords.Where(_ => _.IsCanonical == false);
                    foreach (var nonCanonical in allNonCanonical)
                    {
                        instanceRecognizer.Add(Formula.Parse(nonCanonical.Text));
                    }
                }

                int nextFormulaId = ctx.FormulaRecords.Any() ? ctx.FormulaRecords.OrderByDescending(_ => _.Id).First().Id + 1 : 1;

                // In order to be able to use induction to show that there are no rules with more than 6 variables, 
                // rules are generated formulas starting with formulas of just one variable,
                // then two variables, and so on.
                for (int variableNumber = 1; variableNumber <= 7; variableNumber++)
                {
                    int saveStartId = nextFormulaId;

                    Trace.WriteLine($"Start generating all rules with exactly {variableNumber} variables.");

                    // start by adding variable formula to database
                    Trace.WriteLine($"Start by adding a new variable, {variableNumber}.");
                    var variableFormula = Variable.NewVariable(variableNumber);
                    {
                        var varRecord = ctx.FormulaRecords.Where(_ => _.Text == variableFormula.ToString()).FirstOrDefault();
                        if (varRecord != null)
                        {
                            Trace.WriteLine($"Skipping, variable already exists: {variableFormula}");
                            nextFormulaId = varRecord.Id++;
                        }
                        else
                        {
                            var variableForumulaId = nextFormulaId++;
                            ctx.FormulaRecords.Add(new FormulaRecord(variableForumulaId, variableFormula, isCanonical: true));
                            ctx.SaveChanges();
                            ctx.Clear();
                            Trace.WriteLine($"New variable added: {variableFormula}");
                        }
                    }

                    Trace.WriteLine($"Now, generate all possible formulas with exactly {variableNumber} variables...");

                    /* 
                     * 'Complete' the set of formulas by completing each formula added to the db, starting with the new variable.  
                     * Complete each formula by combining it all completed canonical formulas, ie create the deductive closure of the formula.  
                     * In order to create the minimal closure, formulas need to be completed in formula order.
                     */
                    while (ctx.FormulaRecords.Where(_ => !_.IsCompleted).Any()) // repeat while there are uncompleted formulas
                    {

                        // select the 'smallest' formula, ie the first formula from the formulas in formula order.
                        var todoRecord = ctx.FormulaRecords.Where(f => !f.IsCompleted).OrderBy(_ => _.Length).ThenBy(_ => _.Text).First();
                        var todoFormula = Formula.Parse(todoRecord.Text);
                        var todoId = todoRecord.Id;
                        todoRecord.IsCompleted = true;
                        ctx.SaveChanges();

                        var todoCount = ctx.FormulaRecords.Where(_ => !_.IsCompleted).Count();

                        // we only need to complete canonical formulas
                        if (!todoRecord.IsCanonical)
                        {
                            // if this formula is reducible using the rules generated so far then it can just be removed from the db
                            if (Scripts.FormulaCanBeReduced(ctx, instanceRecognizer, todoFormula))
                            {
                                Debug.Assert(!todoRecord.IsCanonical);
                                ctx.FormulaRecords.Remove(todoRecord);

                                Trace.Write($"Removed from db, formula can be reduced [{todoId}]: {todoRecord.Text}.  ");
                                Trace.WriteLine($"{todoCount} formulas remaining.");
                            }
                            else
                            {
                                // going forward, we'll prevent substitution instances of this formula from becoming new rules.
                                instanceRecognizer.Add(todoFormula);

                                Trace.Write($"Formula is new reduction rule [{todoId}]: {todoRecord.Text}.  ");
                                Trace.WriteLine($"{todoCount} formulas remaining.");

                                //// Used to check for existing rules that are subsumed by the new rule and can be removed.
                                //// This is super expensive, super slow, and can be done afterwards, so removed.
                            }

                            ctx.SaveChanges();
                            ctx.Clear();
                            continue;
                        }

                        Trace.Write($"Complete formula [{todoId}]: {todoRecord.Text}.  ");
                        Trace.WriteLine($"{todoCount} formulas remaining.");

                        var previousCanonicalRecords = ctx.FormulaRecords.AsNoTracking()
                            .Where(_ => _.IsCanonical == true && _.IsCompleted)
                            .OrderBy(_ => _.Length)
                            .ThenBy(_ => _.Text);

                        List<Formula> derivedFormulas = new();
                        foreach (var previousCanonicalRecord in previousCanonicalRecords)
                        {
                            var previousCanonical = Formula.Parse(previousCanonicalRecord.Text);
                            foreach (var derivedFormula in new[] {
                            Nand.NewNand(previousCanonical, todoFormula),
                            Nand.NewNand(todoFormula, previousCanonical) })
                            {
                                // No need to complete formulas with less than {variableNumber} variables, 
                                // completing them won't generate any new rules that werent generated when completing previous variables.  
                                if (derivedFormula.AllVariables.Count < variableNumber)
                                {
                                    Trace.WriteLine($"Not added to db, less than {variableNumber} variables : {derivedFormula}");
                                    continue;
                                }

                                // if already in database then skip
                                if (ctx.FormulaRecords.Where(_ => _.Text == derivedFormula.ToString()).Any())
                                {
                                    Trace.WriteLine($"Not added to db, already in database : {derivedFormula}");
                                    continue;
                                }

                                // No need to complete formulas that can already be reduced.
                                if (Scripts.FormulaCanBeReduced(ctx, instanceRecognizer, derivedFormula))
                                {
                                    Trace.WriteLine(false, $"Not added to db, can already be reduced : {derivedFormula}");
                                    continue;
                                }

                                if (!derivedFormulas.Contains(derivedFormula))
                                {
                                    derivedFormulas.Add(derivedFormula);
                                }
                            }
                        }
                        derivedFormulas.Sort((a, s) => a.CompareTo(s));

                        if (derivedFormulas.Any())
                        {
                            foreach (var derivedFormula in derivedFormulas)
                            {
                                var tt = TruthTable.GetTruthTable(derivedFormula);
                                Formula canonicalFormula = null;
                                var canonicalRecord = ctx.FindCanonicalRecord(tt);
                                if (canonicalRecord != null)
                                {
                                    canonicalFormula = Formula.Parse(canonicalRecord.Text);
                                }

                                // Formulas are not generated in formula order, so we might generate a formula shorter than the current canonical form.
                                // That's OK though, we can just patch them up as long as both formulas have not yet been completed
                                if (canonicalFormula != null && derivedFormula.CompareTo(canonicalFormula) < 0)
                                {
                                    if (canonicalRecord.IsCompleted)
                                    {
                                        throw new TermSatException("Non-canonical formulas are expected to come after their canonical form in the formula order." +
                                            $"\n    canonical: {canonicalRecord.Id}:{canonicalFormula}" +
                                            $"\n    derived: {nextFormulaId}:{derivedFormula}" +
                                            $"\n    todo: {todoId}:{todoFormula}");
                                    }
                                    canonicalRecord.IsCanonical = false;
                                    ctx.SaveChanges();
                                    canonicalRecord = null;
                                    canonicalFormula = null;
                                }

                                var nextRecord = new FormulaRecord(nextFormulaId, derivedFormula, canonicalFormula == null);
                                ctx.FormulaRecords.Add(nextRecord);
                                ctx.SaveChanges();
                                ctx.Clear();

                                if (nextRecord.IsCanonical)
                                {
                                    Trace.WriteLine($"Added uncompleted canonical formula: {derivedFormula}");
                                }
                                else
                                {
                                    Trace.WriteLine($"Added uncompleted non-canonical formula: {derivedFormula} => {canonicalFormula}");
                                }

                                nextFormulaId++;
                            }
                        }
                    }
                }
            }
        }

    }
}
