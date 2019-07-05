using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using TermSAT.RuleDatabase;

namespace TermSAT.Tests
{
    [TestClass]
    public class ReductionAlgorithmTests
    {

        [TestMethod]
        public void SingleReplacementTests()
        {
            ReductionAlgorithms.SingleReplacementReduction reduction;

            Assert.IsTrue(ReductionAlgorithms.TryReduceUsingSingleReplacement("*.1*.1.2", out reduction));
            Assert.AreEqual(1, reduction.Replacements.Count);
            Assert.AreEqual("T", reduction.Replacements[1]);
            Assert.AreEqual("*T*.1.2", reduction.ReducedFormula);

            Assert.IsTrue(ReductionAlgorithms.TryReduceUsingSingleReplacement("*.1*-.1.2", out reduction));
            Assert.AreEqual(1, reduction.Replacements.Count);
            Assert.AreEqual("F", reduction.Replacements[1]);
            Assert.AreEqual("*F*.1.2", reduction.ReducedFormula);

        }
    }
}
