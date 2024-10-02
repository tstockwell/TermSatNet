using Microsoft.VisualStudio.TestTools.UnitTesting;
using TermSAT.Formulas;
using TermSAT.Nand;
using TermSAT.RuleDatabase;

namespace TermSAT.Tests
{
    [TestClass]
    public class NandSchemeReductionTests
    {
        [TestMethod]
        public void CurrentNandReductionTests()
        {
            // 315	||.1.2|.3|.1|T.2	11	1B1B	0		167	||.1.2|.3|T.1
            // setting .2 => T in antecedent => ||.1.T|.3|.1|T.2
            //                             => ||.1.T|.3|F|T.2
            //                             => ||.1.T|.3T
            // yields result that is independent of .2, therefore set .2 => F in subsequent...
            // ||.1.2|.3|.1|T.2 => ||.1.2|.3|.1|TF
            //                  => ||.1.2|.3|.1T
            //                  => ||.1.2|.3|T.1
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("||.1.2|.3|.1|T.2");
                var canonicalFormula = Formula.Parse("||.1.2|.3|T.1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }


            // |T||.3|.1.2||T.1|T.2 is not a valid reduction for ||.1|T.2||.3|.1.2||T.1|T.2
            // => ||.1|T.2||.3|.1.2||T.1|T.2
            // setting .1 => T in antecedent => ||T|T.2||.3|.1.2||T.1|T.2
            //                             => |.2||.3|.1.2||T.1|T.2
            //                             => |.2||.3|.1T||T.1|TT
            //                             => |.2||.3|.1T||T.1F
            //                             => |.2||.3|.1TT
            //                             => |.2|T|.3|.1T
            // yields result that is independent of .2, therefore set .2 => F in subsequent...
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("||.1|T.2||.3|.1.2||T.1|T.2");
                var canonicalFormula = Formula.Parse("|T||.1.2|.1.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

        }
        [TestMethod]
        public void BasicNandReductionTests()
        {
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.1T");
                var canonicalFormula = Formula.Parse("|T.1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.2|.3|.1T");
                var canonicalFormula = Formula.Parse("|.2|.3|T.1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|TT");
                var canonicalFormula = Formula.Parse("F");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.2.1");
                var canonicalFormula = Formula.Parse("|.1.2");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.1|.1.2");
                var canonicalFormula = Formula.Parse("|.1|T.2");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|T|T.1");
                var canonicalFormula = Formula.Parse(".1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.2|.3|.1.2");
                var canonicalFormula = Formula.Parse("|.2|.3|T.1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("||.1|T.2|.2|.1T");
                var canonicalFormula = Formula.Parse("||.1|T.2|.2|T.1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.1||.2.3||T.3|.1.2");
                var canonicalFormula = Formula.Parse("|.1||.2.3||T.2|T.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("||.1|.2.3|.3|T.1");
                var canonicalFormula = Formula.Parse("||.1|T.3|.3|.1.2");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("||.1|.2.3|.2|T.1");
                // => 
                var canonicalFormula = Formula.Parse("||.1|T.2|.2|.1.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("||.2.3||T.3|.1T");
                var canonicalFormula = Formula.Parse("||.2.3||T.1|T.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.1||T.2|T.3");
                var canonicalFormula = Formula.Parse("|T||.1.2|.1.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.3||.1.2||T.1|.2.3");
                var canonicalFormula = Formula.Parse("|.3||.1.2||T.1|T.2");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.1T");
                var canonicalFormula = Formula.Parse("|T.1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.2|T|.1.3");
                var canonicalFormula = Formula.Parse("|.1|T|.2.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                // SELECT f.*, c.Text FROM FormulaRecords f
                // join(SELECT * FROM FormulaRecords WHERE IsCanonical = 1) c on c.TruthValue = f.TruthValue
                // Where f.Id = 5478 yields...
                // 5478    |||.1.2|.1.3|.3|T||T.1|T.2  19  EAEA    0    94  |T||.1.2|.1.3
                // Note: cant tell, but IsSubsumedBySchema == '', when IsSubsumedBySchema should be anything else
                // Note: weirdly, |T||T.1|T.2 is canonical
                // setting .1 => F in antecedent => |||F.2|F.3|.3|T||T.1|T.2
                //                             => ||TT|.3|T||T.1|T.2
                //                             => |F|.3|T||T.1|T.2
                //                             => T
                // yields result that is independent of .1, therefore set .1 => T in subsequent...
                // |||.1.2|.1.3|.3|T||T.1|T.2 => |||.1.2|.1.3|.3|T||TT|T.2
                //                            => |||.1.2|.1.3|.3|T|F|T.2
                //                            => |||.1.2|.1.3|.3|TT
                //                            => |||.1.2|.1.3|.3F
                //                            => |||.1.2|.1.3T
                //                            => |T||.1.2|.1.3
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|||.1.2|.1.3|.3|T||T.1|T.2");
                var canonicalFormula = Formula.Parse("|T||.1.2|.1.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }


        }
    }
}
