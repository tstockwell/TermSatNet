using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction;

public class Scripts
{
    static readonly int VARIABLE_COUNT = 3;
    static readonly string DATABASE_PATH = $"nand-rules-{VARIABLE_COUNT}.db";

    // do this to create memory-based db
    //readonly string DATABASE_PATH = ":memory:"; 

    [TestMethod]
    public void RunNandRuleGenerator()
    {
        var database = new FormulaDatabase(DATABASE_PATH);
        database.Clear();
        new RuleGenerator(database, new NandFormulaGenerator(database, VARIABLE_COUNT)).Run();
    }

    [TestMethod]
    public void RunNandRuleReport()
    {
        var database = new FormulaDatabase(DATABASE_PATH);
        var options = new DatabaseReport.DatabaseReportOptions();
        if (VARIABLE_COUNT <= 2)
            options.ShowNonCanonicalFormulas = true;
        new DatabaseReport(database, options).Run();
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
            TruthTable truthTable = TruthTable.NewTruthTable(formula);
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
                Formula reducedFormula = formula.NandReduction();

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
                Trace.WriteLine($"error: {formula}");
            }
        }

        return isEquivalent;
    }

}
