using Microsoft.VisualStudio.TestTools.UnitTesting;
using TermSAT.RuleDatabase;

namespace TermSAT.Tests
{

    [TestClass]
    public class RuleRepositoryTests
    {
        // This test is commented out because the original java version obviously did nothing useful
        //
        //class FormulaInfo
        //{
        //    string text;
        //    int[] counts = new int[] { 0, 0, 0 };
        //}

        ///*
        // * We must check that our ordering of formulas by length is 'stable'.
        // * An ordering is stable if, for all equal terms, if t' > t'' then 
        // * t'[t/x] > t''[t/x].
        // * Our ordering is stable if for all formulas with the same truth value 
        // * there is no formula t that contains a variable x and a formula t' 
        // * such that t' < t and t' contains more instances of x than t. 
        // * 
        // * It is easy to see that our reduction ordering is also 'monotonic'.
        // * Also, all our reduction rules have the form t -> t' where t' < t.
        // * Therefore, if our reduction ordering is stable then our set of 
        // * reduction rules is Noetherian and thus any reduction process driven by 
        // * our reduction rules is guaranteed to terminate. 
        // */
        //public void testOrderingStability()
        //{
        //    FormulaDatabase ruleDatabase = new FormulaDatabase();

        //    for (int t = 0; t < TruthTable.MAX_TRUTH_TABLES; t++)
        //    {
        //        TruthTable tt = TruthTable.create(t);
        //        ResultIterator<Formula> i = ruleDatabase.getAllFormulas(tt);
        //        ArrayList<FormulaInfo> list = new ArrayList<FormulaInfo>();
        //        while (i.hasNext())
        //        {
        //            FormulaInfo fi = new FormulaInfo();
        //            fi.text = i.next().tostring();
        //            string[] vars = fi.text.split("[^1234567890]");
        //            for (string var:vars)
        //            {
        //                if (0 < var.length())
        //                    fi.counts[Integer.parseInt(var) - 1]++;
        //            }
        //        }
        //        for (FormulaInfo a: list)
        //        {
        //            for (FormulaInfo b: list)
        //            {
        //                if (a.text.length() <= b.text.length())
        //                    continue;
        //                for (int v = 0; v < 3; v++)
        //                    if (a.counts[v] < b.counts[v])
        //                        throw new RuntimeException("Reduction ordering is not stable for these two formulas:\n" + a.text + "\n" + b.text);
        //            }
        //        }
        //    }
        //}

        // basic consistency checks
        public void testSoundness()
        {
            RuleRepository rules = new RuleRepository();
            FormulaDatabase ruleDatabase = new FormulaDatabase();
            ResultIterator<Formula> nonCanonicalFormulas = ruleDatabase.getAllNonCanonicalFormulas();

            // for all non-canonical formulas...
            while (nonCanonicalFormulas.hasNext())
            {
                Formula rule = nonCanonicalFormulas.next();
                assertTrue(rules.containsKey(rule.tostring()));

                // ...make sure that reduced formula is actually shorter...
                Formula reduced = rules.findReducedFormula(rule);
                assertTrue(reduced.length() < rule.length());

                //..and has the same truth table as the original formula...
                assertEquals(TruthTable.getTruthTable(rule), TruthTable.getTruthTable(reduced));

                //..and has the same truth table as the associated canonical formula
                Formula canonical = ruleDatabase.findCanonicalFormula(rule);
                assertEquals(TruthTable.getTruthTable(rule), TruthTable.getTruthTable(canonical));
            }
        }

    }

}
