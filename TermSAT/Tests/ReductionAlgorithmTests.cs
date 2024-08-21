using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.Tests
{
    [TestClass]
    public class ReductionAlgorithmTests
    {

        [TestMethod]
        public void SingleReplacementTests()
        {
            ReplacementReduction reduction;

            Assert.IsTrue(ReductionAlgorithms.TryReduceUsingSingleReplacement("*.1.1", out reduction));
            Assert.AreEqual(1, reduction.Replacements.Count);
            Assert.AreEqual("T", reduction.Replacements[2]);
            Assert.AreEqual("*.1T", reduction.ReducedFormula);

            Assert.IsTrue(ReductionAlgorithms.TryReduceUsingSingleReplacement("*.1-.1", out reduction));
            Assert.AreEqual(1, reduction.Replacements.Count);
            Assert.AreEqual("T", reduction.Replacements[3]);
            Assert.AreEqual("*.1-T", reduction.ReducedFormula);

            Assert.IsTrue(ReductionAlgorithms.TryReduceUsingSingleReplacement("*.1*.1.2", out reduction));
            Assert.AreEqual(1, reduction.Replacements.Count);
            Assert.AreEqual("T", reduction.Replacements[3]);
            Assert.AreEqual("*.1*T.2", reduction.ReducedFormula);

            Assert.IsTrue(ReductionAlgorithms.TryReduceUsingSingleReplacement("*.1*.2.1", out reduction));
            Assert.AreEqual(1, reduction.Replacements.Count);
            Assert.AreEqual("T", reduction.Replacements[4]);
            Assert.AreEqual("*.1*.2T", reduction.ReducedFormula);

            Assert.IsTrue(ReductionAlgorithms.TryReduceUsingSingleReplacement("*.1-*.1.2", out reduction));
            Assert.AreEqual(1, reduction.Replacements.Count);
            Assert.AreEqual("T", reduction.Replacements[4]);
            Assert.AreEqual("*.1-*T.2", reduction.ReducedFormula);

            Assert.IsTrue(ReductionAlgorithms.TryReduceUsingSingleReplacement("*.1-*.2.1", out reduction));
            Assert.AreEqual(1, reduction.Replacements.Count);
            Assert.AreEqual("T", reduction.Replacements[5]);
            Assert.AreEqual("*.1-*.2T", reduction.ReducedFormula);


            Assert.IsTrue(ReductionAlgorithms.TryReduceUsingSingleReplacement("**.1.3*.1.2", out reduction));
            Assert.AreEqual(1, reduction.Replacements.Count);
            Assert.AreEqual("T", reduction.Replacements[2]);
            Assert.AreEqual("**T.3*.1.2", reduction.ReducedFormula);

            Assert.IsTrue(ReductionAlgorithms.TryReduceUsingSingleReplacement("*.1*.1.2", out reduction));
            Assert.AreEqual(1, reduction.Replacements.Count);
            Assert.AreEqual("T", reduction.Replacements[3]);
            Assert.AreEqual("*.1*T.2", reduction.ReducedFormula);

            Assert.IsTrue(ReductionAlgorithms.TryReduceUsingSingleReplacement("*.1*-.1.2", out reduction));
            Assert.AreEqual(1, reduction.Replacements.Count);
            Assert.AreEqual("T", reduction.Replacements[4]);
            Assert.AreEqual("*.1*-T.2", reduction.ReducedFormula);

            Assert.IsTrue(ReductionAlgorithms.TryReduceUsingSingleReplacement("**.1.3*-.1.2", out reduction));
            Assert.AreEqual(1, reduction.Replacements.Count);
            Assert.AreEqual("F", reduction.Replacements[2]);
            Assert.AreEqual("**F.3*-.1.2", reduction.ReducedFormula);

            //* (*.1*.4.3) -*(*.4.1)(-**.2-.4.3) ==> **.3.4-*.1-.4
            Assert.IsTrue(ReductionAlgorithms.TryReduceUsingSingleReplacement("**.1*.4.3-**.4.1-**.2-.4.3", out reduction));
            Assert.AreEqual(1, reduction.Replacements.Count);
            Assert.AreEqual("F", reduction.Replacements[2]);
            Assert.AreEqual("**F.3*-.1.2", reduction.ReducedFormula);

        }
    }
}
