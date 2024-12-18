using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Linq;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.SchemeReducer;

public static class Scripts
{
    private static object f;

    public static bool OrderingTest(FormulaDatabase formulaDatabase)
    {
        Formula previousFormula = null;
        FormulaRecord previousRecord = null;
        // note: formulaDatabase.GetAllFormulas() returns formulas ordered by Id.
        foreach (var record in formulaDatabase.GetAllFormulaRecords())
        {
            var formula = Formula.Parse(record.Text);
            if (previousRecord != null)
            {
                Assert.IsTrue(previousRecord.Id < record.Id);
                Assert.IsTrue(previousFormula.CompareTo(formula) < 0, "id order should be the same as formula order");
            }
            Trace.WriteLine($"{record.Id}:{formula} is in order");
            previousRecord = record;
            previousFormula = formula;
        }

        Trace.WriteLine("generated formulas are in correct formula order...");


        return true;
    }

    /// <summary>
    /// Demonstrates that the basic reduction scheme is subsumes all reduction rules of 3 variables or less.  
    /// This is done by using the basic reduction scheme to reduce all non-canonical formulas of 3 variables 
    /// or less to their canonical formula.
    /// </summary>
    public static bool BasicSchemeEquivalence(FormulaDatabase formulaDatabase)
    {
        bool isEquivalent = true;
        foreach (var formula in formulaDatabase.GetAllNonCanonicalFormulas())
        {
            TruthTable truthTable = TruthTable.GetTruthTable(formula);
            var canonicalFormulas = formulaDatabase.GetCanonicalFormulas(truthTable);
            Debug.Assert(canonicalFormulas.Count == 1, "there is never more than 1 canonical formula");
            var canonicalFormula = canonicalFormulas[0];

            Debug.Assert(canonicalFormula.Length <= formula.Length, "non-canonical formulas are never shorter than canonical formulas");
            if (canonicalFormula.Length == formula.Length)
            {
                Trace.WriteLine($"Skipping {formula}, canonical form: {canonicalFormula}");
                continue;
            }

            Formula reducedFormula = formula.ReduceUsingBasicScheme();

            if (!reducedFormula.Equals(canonicalFormula))
            {
                isEquivalent = false;
                Trace.WriteLine("The basic reduction scheme does not reduce this formula to its canonical form...");
                Trace.WriteLine("=== non-canonical form ===");
                Trace.WriteLine(formula.ToString());
                Trace.WriteLine("=== canonical form ===");
                Trace.WriteLine(canonicalFormula.ToString());
                break;
            }

            Trace.WriteLine($"The basic reduction scheme reduces {formula} to {canonicalFormula}");
        }

        return isEquivalent;
    }
}
