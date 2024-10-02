using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;
using TermSAT.SchemeReducer;
using static System.Net.Mime.MediaTypeNames;

namespace TermSAT.Tests
{
    [TestClass]
    public class BasicReductionAlogorthmTests
    {
        /// <summary>
        /// Formulas are ordered.
        /// When generating rules, it's important that the generated formulas were generated in formula order.
        /// Therefore the formula id column in the database 
        /// </summary>
        [TestMethod]
        public void FormulaOrderingTest()
        {
        }

        [TestMethod]
        public void BasicReductionTests()
        {
            {
                var nonCanonicalformula = Formula.Parse("*-.2*-.1.3");
                var canonicalFormula = Formula.Parse("*-.1*-.2.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = nonCanonicalformula.ReduceUsingBasicScheme();
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = Formula.Parse("*-.2**.1.2.3");
                var canonicalFormula = Formula.Parse("*-.1*-.2.3");
                var reducedFormula = nonCanonicalformula.ReduceUsingBasicScheme();
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = Formula.Parse("**.1.2-*.1.3");
                var canonicalFormula = Formula.Parse("-*.1-*.2-.3");
                var reducedFormula = nonCanonicalformula.ReduceUsingBasicScheme();
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = Formula.Parse("**.1.2-*.3.2");
                var canonicalFormula = Formula.Parse("-**-.1.3.2");
                var reducedFormula = nonCanonicalformula.ReduceUsingBasicScheme();
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = Formula.Parse("*.2*.1.3");
                var canonicalFormula = Formula.Parse("*.1*.2.3");
                var reducedFormula = nonCanonicalformula.ReduceUsingBasicScheme();
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = Formula.Parse("*-.2-*.1.2");
                var canonicalFormula = Formula.Parse("*-.1.2");
                var reducedFormula = nonCanonicalformula.ReduceUsingBasicScheme();
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = Formula.Parse("*-.1-*.2.3");
                var canonicalFormula = Formula.Parse("**.2.3.1");
                var reducedFormula = nonCanonicalformula.ReduceUsingBasicScheme();
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = Formula.Parse("*-.1.1");
                var canonicalFormula = Formula.Parse(".1");
                var reducedFormula = nonCanonicalformula.ReduceUsingBasicScheme();
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = Formula.Parse("*.1T");
                var canonicalFormula = Formula.Parse("T");
                var reducedFormula = nonCanonicalformula.ReduceUsingBasicScheme();
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = Formula.Parse("*.1F");
                var canonicalFormula = Formula.Parse("-.1");
                var reducedFormula = nonCanonicalformula.ReduceUsingBasicScheme();
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = Formula.Parse("*.1.1");
                var canonicalFormula = Formula.Parse("T");
                var reducedFormula = nonCanonicalformula.ReduceUsingBasicScheme();
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
        }
    }
}
