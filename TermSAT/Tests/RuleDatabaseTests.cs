using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.Tests
{
    [TestClass]
    public class RuleDatabaseTests
    {

        [TestMethod]
        public void TruthTableTests()
        {
            // truth table for a formula
            var tt= TruthTable.NewTruthTable("T".ToFormula());
            Assert.AreEqual("FF", tt.ToString());
            tt= TruthTable.NewTruthTable("F".ToFormula());
            Assert.AreEqual("00", tt.ToString());

            // truth table from hex string 
            tt= TruthTable.NewTruthTable("FF");
            Assert.AreEqual("FF", tt.ToString());
            tt= TruthTable.NewTruthTable("00");
            Assert.AreEqual("00", tt.ToString());

        }

        FormulaDatabase database;

        [TestInitialize]
        public void Initialize()
        {
            database = new FormulaDatabase();
            database.Clear();
        }

        [TestMethod]
        public void FormulaGeneratorTests()
        {
            Formula formula= null;

            /// the following sequence of operations is essentially what the RuleGenerator does
            /// when it first starts up
            var generator= new FormulaGenerator(database);

            var startingFormulaSequence= new List<Formula> { "T", "F", ".1", ".2", ".3" };

            foreach( var f in startingFormulaSequence)
            {
                formula= (formula == null) ? generator.GetStartingFormula() : generator.GetNextWellFormedFormula();
                Assert.AreEqual(f, formula);
                database.AddFormula(formula, isCanonical:true);
            }
            Assert.AreEqual(startingFormulaSequence.Count, database.CountCanonicalFormulas());

            foreach( var f in startingFormulaSequence)
            {
                formula= generator.GetNextWellFormedFormula();
                var n= Negation.NewNegation(f);
                Assert.AreEqual(n, formula);
                var tt= database.GetCanonicalFormulas(TruthTable.NewTruthTable(n));
                if (tt.Count < 0)
                    database.AddFormula(n, isCanonical:true);
            }

            formula= generator.GetNextWellFormedFormula();
            Assert.AreEqual("*TT", formula);

            formula= generator.GetNextWellFormedFormula();
            Assert.AreEqual("*TF", formula);
            formula= generator.GetNextWellFormedFormula();
            Assert.AreEqual("*T.1", formula);
            formula= generator.GetNextWellFormedFormula();
            Assert.AreEqual("*T.2", formula);
            formula= generator.GetNextWellFormedFormula();
            Assert.AreEqual("*T.3", formula);
            formula= generator.GetNextWellFormedFormula();
            Assert.AreEqual("*FT", formula);
            formula= generator.GetNextWellFormedFormula();
            Assert.AreEqual("*FF", formula);
            formula= generator.GetNextWellFormedFormula();
            Assert.AreEqual("*F.1", formula);
            formula= generator.GetNextWellFormedFormula();
            Assert.AreEqual("*F.2", formula);
            formula= generator.GetNextWellFormedFormula();
            Assert.AreEqual("*F.3", formula);
            formula= generator.GetNextWellFormedFormula();
            Assert.AreEqual("*.1T", formula);
            formula= generator.GetNextWellFormedFormula();
            Assert.AreEqual("*.1F", formula);
        }
    }
}
