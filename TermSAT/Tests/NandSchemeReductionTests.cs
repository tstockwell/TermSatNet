using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using TermSAT.Formulas;
using TermSAT.NandReduction;
using TermSAT.RuleDatabase;

namespace TermSAT.Tests
{
    [TestClass]
    public class NandSchemeReductionTests
    {
        [TestMethod]
        public void CurrentNandReductionTests()
        {
            // |||T.1|.2.3||.1.2|.1.3 => |T||.1.2|.1.3
            //      test .1->F in antecedent
            //      => |||TF|.2.3||.1.2|.1.3
            //      => ||T|.2.3||.1.2|.1.3
            //          test .2->T in antecedent
            //          => ||T|T.3||.1.2|.1.3
            //          => |.3||.1.2|.1.3
            //          => |.3||.1.2|.1T .3->T in subsequent
            //          => |.3||F.2|.1T .1->F in antecedent 
            //          => |.3|T|.1T .2 is wildcard
            //          => |.3.1 
            //          => |.1.3
            //      => ||T|.2.3||.1F|.1.3   .2 ->F in subsequent
            //      => ||T|.2.3|T|.1.3      .1 is wildcard
            //      => ||T|.1.3|T|.2.3      
            //      => |.1|T|.2.3
            // => |||T.1|.2.3||T.2|.1.3     !!!!  .1->T in subsequent is not a valid reduction WTF !!!!!!
            // BUT... start by testing the subsequent first and it works, WTF!!!
            // |||T.1|.2.3||.1.2|.1.3 => |T||.1.2|.1.3
            //      test .1->F in subsequent
            //      => |||T.1|.2.3||F.2|F.3 
            //      => |||T.1|.2.3||F.2T
            //      => |||T.1|.2.3|TT
            //      => |||T.1|.2.3F
            //      => T .1 is wildcard
            // => |||TT|.2.3||.1.2|.1.3 .1->T in antecedent
            // => ||F|.2.3||.1.2|.1.3 
            // => |T||.1.2|.1.3 
            {
                {

                }
                {
                    var reductionSteps = new[] 
                    {
                        "|||TF|.2.3||.1.2|.1.3",
                        "||T|.2.3||.1.2|.1.3",
                        "||T|.2.3||.1F|.1.3",
                        "||T|.2.3|T|.1.3",
                        "|.1|T|.2.3"
                    };
                    var reductionFormulas = reductionSteps.Select(s => Formula.Parse(s)).ToArray();
                    var truthTables = reductionFormulas.Select(f => TruthTable.NewTruthTable(f).ToString()).Distinct().ToArray();
                    Assert.IsTrue(truthTables.Length == 1);
                }
                {
                    var reductionSteps = new[]
                    {
                        "|||T.1|.2.3||.1.2|.1.3",
                        "|||TT|.2.3||.1.2|.1.3",
                    };
                    var reductionFormulas = reductionSteps.Select(s => Formula.Parse(s)).ToArray();
                    var truthTables = reductionFormulas.Select(f => TruthTable.NewTruthTable(f).ToString()).Distinct().ToArray();
                    Assert.IsTrue(truthTables.Length == 1);
                }

                var nonCanonicalformula = (Nand)Formula.Parse("|||T.1|.2.3||.1.2|.1.3");
                var canonicalFormula = Formula.Parse("|T||.1.2|.1.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                Proof proof = new();
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula, proof);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            // |T||.1|T.2|.3|T.1 => ||.1.2||T.1|T.3
            {
                var nonCanonicalformula = (Nand)Formula.Parse("|T||.1|T.2|.3|T.1");
                var canonicalFormula = Formula.Parse("||.1.2||T.1|T.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                Proof proof = new();
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula, proof);
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
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.1T");
                var canonicalFormula = Formula.Parse("|T.1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.2|.1T");
                var canonicalFormula = Formula.Parse("|.2|T.1");
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
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.2|.3|.1.2");
                var canonicalFormula = Formula.Parse("|.2|.3|T.1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            {
                //// |.1||T.2|T.3 => |T||.1.2|.1.3
                //// Reducible by rewriting using rule |a|bc -> |T||a|Tb|a|Tc.
                //// => |T||.1|T|T.2|.1|T|T.3 
                //// => |T||.1|T|T.2|.1.3
                //// => |T||.1.2|.1.3
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.1||T.2|T.3");
                var canonicalFormula = Formula.Parse("|T||.1.2|.1.3");
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
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("||T.2||T.3|.1F");
                var canonicalFormula = Formula.Parse("|.3|T.2");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            // ||.2.3||T.3|.1.2, is canonical
            // proof is that, for the common subterms .2 and .3, no wildcards exist.
            // test .2ant -> T
            //  => ||T.3||T.3|.1.2
            //  => ||T.3||TF|.1.2
            //  => ||T.3|T|.1.2 canonical
            // test .2ant -> F
            //  => ||F.3||T.3|.1.2
            //  => |T||T.3|.1.2 canonical
            // test .2seq -> T
            //  => ||.2.3||T.3|.1T
            //  => ||.2.3||T.1|T.3 canonical
            // test .2seq -> F
            //  => ||.2.3||T.3|.1F
            //  => ||.2.3||T.3T
            //  => ||.2.3.3
            //  => |.3|.2.3
            //  => |.3|T.2 canonical
            // test .3ant -> T
            //  => ||.2T||T.3|.1.2
            //  => ||.2T||T.3|.1F
            //  => ||.2T||T.3T
            //  => ||.2T.3
            //  => |.3|.2T canonical
            // test .3ant -> F
            //  => ||.2F||T.3|.1.2
            //  => |T||T.3|.1.2 canonical
            // test .3seq -> T
            //  => ||.2.3||TT|.1.2
            //  => ||.2.3|F|.1.2
            //  => ||.2.3T
            //  => |T|.2.3 canonical
            // test .3seq -> F
            //  => ||.2.3||F.3|.1.2
            //  => ||.2.3|T|.1.2
            //  => |.1|T|.2|T.3 canonical
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("||.2.3||T.3|.1.2");
                var canonicalFormula = Formula.Parse("||.2.3||T.3|.1.2");
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
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.3||.1.2||T.1|.2.3");
                var canonicalFormula = Formula.Parse("|.3||.1.2||T.1|T.2");
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
                // |||T.1|T.3||.1.3|.2.3
                // DebugAssertException: 'an instance of the subterm should have been found
                // Caused by the wildcard tracer returning wildcard position of 5 instead of 4
                //  => test .1seq -> T
                //      => |||T.1|T.3||T.3|.2.3
                //      => |||T.1|T.3||T.3|.2F
                //      => |||T.1|T.3||T.3T
                //      => |||T.1|T.3.3
                //      => |.3||T.1|T.3
                //      => |.3||T.1|TT
                //      => |.3||T.1F wildcard @ 5
                //      => |.3T
                //      => |T.3
                //  => replace wildcard @ 4 -> F
                //  => |||TF|T.3||.1.3|.2.3
                //  => ||T|T.3||.1.3|.2.3
                //  => |.3||.1.3|.2.3
                //  => |.3||.1T|.2T
                //  => |.3||T.1|T.2
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|||T.1|T.3||.1.3|.2.3");
                var canonicalFormula = Formula.Parse("|T||.1.3|.2.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            // |||T.2|T.3|.3|T||T.1|T.2
            {
                var nonCanonicalFormula = (Formulas.Nand)Formula.Parse("|||T.2|T.3|.3|T||T.1|T.2");
                var nonCanonicalTT = TruthTable.NewTruthTable(nonCanonicalFormula).ToString();
                var canonicalFormula = Formula.Parse("|T||T.2|.1.3");
                var canonicalTT = TruthTable.NewTruthTable(canonicalFormula).ToString();
                var reducedFormula = NandReducer.NandReduction(nonCanonicalFormula);
                Assert.AreEqual(nonCanonicalTT, canonicalTT);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                // SELECT f.*, c.Text FROM FormulaRecords f
                // join(SELECT * FROM FormulaRecords WHERE IsCanonical = 1) c on c.TruthValue = f.TruthValue
                // Where f.Id = 5478 yields...
                // 5478    |||.1.2|.1.3|.3|T||T.1|T.2  19  EAEA    0    94  |T||.1.2|.1.3
                // Note: you cant tell from the above, but IsSubsumedBySchema == '', and it shouldn't be blank
                // Note: weirdly, |T||T.1|T.2 is canonical
                // |||.1.2|.1.3|.3|T||T.1|T.2
                //      test .1 => F in antecedent
                //      => |||F.2|F.3|.3|T||T.1|T.2
                //      => ||TT|.3|T||T.1|T.2
                //      => |F|.3|T||T.1|T.2 ;.1 is a wildcard
                //      => T
                // yields result that is independent of .1, therefore set .1 => T in subsequent...
                // => |||.1.2|.1.3|.3|T||TT|T.2
                // => |||.1.2|.1.3|.3|T|F|T.2
                // => |||.1.2|.1.3|.3|TT
                //                            => |||.1.2|.1.3|.3F
                //                            => |||.1.2|.1.3T
                //                            => |T||.1.2|.1.3
                //
                // error: |||.1.2|T.3|.3|T||T.1|T.2 is not a valid reduction for |||.1.2|.1.3|.3|T||T.1|T.2
                // test .1->F in subsequent
                // => |||.1.2|.1.3|.3|T||TF|T.2
                // => |||.1.2|.1.3|.3|T|T|T.2
                // => |||.1.2|.1.3|.3|T.2
                //      test => |||.1.2|.1.3|.3|T.2 ;wildcard .2->F 

                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|||.1.2|.1.3|.3|T||T.1|T.2");
                var canonicalFormula = Formula.Parse("|T||.1.2|.1.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

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

            // ||.1|.2.3|.2.3
            // ||.2.3|.1|.2.3
            //  => ||.2.3|T.1, since  |T||.1.2|.2 => |T||T.1|.2
            //  => ||T.1|.2.3
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("||.2.3|.1|.2.3");
                var canonicalFormula = Formula.Parse("||T.1|.2.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            // |T||.1|.2.3|.2.3
            //  => |T||T.1|.2.3, since  |T||.1.2|.2 => |T||T.1|.2
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|T||.1|.2.3|.2.3");
                var canonicalFormula = Formula.Parse("|T||T.1|.2.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            // ||.1.2|.3|T.2 is not a valid reduction for ||.1.2|.3|.1.2
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("||.1.2|.3|.1.2");
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreNotEqual(reducedFormula, Formula.Parse("||.1.2|.3|T.2"));
            }
            // error: ||.1.2|.2.3 is not a valid reduction for ||.1.2|T||.1|.2.3|.2.3
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("||.1.2|T||.1|.2.3|.2.3");
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreNotEqual(reducedFormula, Formula.Parse("||.1.2|.2.3"));
            }

            // error...  |||T.1|T.3||T.2|.1.3 is not a valid reduction for |||T.1|T.3||.1.3|.2.3
            // The issue is that the proof listener erroneously identifies the last instance of .3 as a wildcard.
            //  |||T.1|T.3||.1.3|.2.3
            //      => |||T.1|TF||.1.3|.2.3, .3 -> F in antecedent
            //      => |||T.1T||.1.3|.2.3
            //      => |.1||.1.3|.2.3
            //      => |.1||T.3|.2.3
            //      => |.1||T.3|.2F
            //          <<< the following step identifies .2 as a wildcard,
            //          <<< the proof listener erroneously identifies this step as a reduction target for .3
            //      => |.1||T.3T
            //      => |.1.3
            // Another issue is that |||T.1|T.3||.1.3|.2.3 should be reducible via 'normal' nand reduction....
            //  test .1ant -> T
            //  => |||TT|T.3||.1.3|.2.3
            //  => ||F|T.3||.1.3|.2.3 wildcard @ position 8
            //  therefore
            //  => |||T.1|T.3||F.3|.2.3 
            //  => |||T.1|T.3|T|.2.3 
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|||T.1|T.3||.1.3|.2.3");
                var canonicalFormula = Formula.Parse("|T||.1.3|.2.3");
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreNotEqual(reducedFormula, Formula.Parse("|||T.1|T.3||T.2|.1.3"));
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            // error... |T|.1.3 is not a valid reduction for ||.1.3||.2.3||.1.2|.1.3
            // ||.1.3||.2.3||.1.2|.1.3 should be reducible via 'normal' nand reduction....
            //  .1 -> T in antecedent yields...
            //      => ||T.3||.2.3||.1.2|.1.3
            //      => ||T.3||.2F||.1.2|.1F
            //      => ||T.3||.2F||.1.2T, and therefore .1 is a wildcard, targetPosition == 12 (the .1 in |.13)
            //      if we dont stop the proof at this point...
            //      => ||T.3|T||.1.2T
            //  therefore .1 -> F in subsequent
            //      => ||.1.3||.2.3||.1.2|F.3
            //      => ||.1.3||.2.3|T|.1.2
            //      => ||.1.3|.1|T||.2.3.2, since |.2|T|.1.3 => |.1|T|.2.3 
            //      => ||.1.3|.1|T|.2|.2.3
            //      => ||.1.3|.1|T|.2|T.3
            //      => |T|.1||.2|T.3|T.3 , since ||.1.2|.1|T.3 => |T|.1|.3|T.2
            //      => |T|.1||.2T|T.3 
            //      => |T|.1||T.2|T.3 
            //      => |T|T||.1.2|.1.3, since |a||Tb|Tc -> |T||ab|ac 
            //      => ||.1.2|.1.3
            //  ..or..
            //  .3 -> T in antecedent yields...
            //      => ||.1T||.2.3||.1.2|.1.3
            //      => ||.1T||.2.3||F.2|F.3
            //      => ||.1T||.2.3|TT
            //      => ||.1T||.2.3F
            //      => ||.1TT
            //      => .1
            //  therefore .3 -> F in subsequent
            //      => ||.1.3||.2F||.1.2|.1F
            //      => ||.1.3|T||.1.2T
            //      => ||.1.3|T|T|.1.2
            //      => ||.1.3|.1.2
            //      => ||.1.2|.1.3
            // also error... Debug.Fail failed with '||.1.3||.2.3||F.2|.1.3 is not a valid reduction for ||.1.3||.2.3||.1.2|.1.3
            //  This happens because the 'wildcard finder' decides that the .1 in |.1.2 is a wildcard,
            //  when in fact it should be the .1 in the last instance of |.1.3

            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("||.1.3||.2.3||.1.2|.1.3");
                var canonicalFormula = Formula.Parse("||.1.2|.1.3");
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreNotEqual(reducedFormula, Formula.Parse("|T|.1.3"));
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            // |T||T.1||T.2|T.3 => ||.2|T.1|.3|T.1
            //  test |a|bc => |a|bc -> |T||a|Tb|a|Tc -> * on subsequent
            //      => |T|||T.1|T|T.2||T.1|T|T.3 where a= |T.1 b= |T.2 c= |T.3
            //      => |T|||T.1|T|T.2||T.1.3 
            //      => |T|||T.1|T|T.2|.3|T.1
            //      => |T|||T.1.2|.3|T.1
            //      => |T||.2|T.1|.3|T.1
            //      => |T||T.1||T.2|T.3,  ||ba|ca -> |T|a||Tb|Tc -> *
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|T||T.1||T.2|T.3");
                var canonicalFormula = Formula.Parse("||.2|T.1|.3|T.1");
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }


            // error... ||.1|T.2|.3|T.2 is not a valid reduction for |.1||T.2||T.1|T.3
            //  => |.1||T.2||TT|T.3
            //  => |.1||T.2|F|T.3
            //  => |.1||T.2T
            //  => |.1.2
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.1||T.2||T.1|T.3");
                var canonicalFormula = Formula.Parse("|.1.2");
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreNotEqual(reducedFormula, Formula.Parse("||.1|T.2|.3|T.2"));
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            // ||.2|.1.3|.3|T.2 => ||.2|T.3|.3|.1.2
            // Is not reducible by NRA...
            //  test .2ant => F 
            //  => ||F|.1.3|.3|T.2
            //  => |T|.3|T.2, canonical
            //  test .2ant => T
            //  => ||T|.1.3|.3|T.2
            //  => ||T|.1.3|T|T.2
            //  => ||T|.1.3.2, canonical
            //  test .2seq => F 
            //  => ||.2|.1.3|.3|TF
            //  => ||.2|.1.3|T.3
            //  => ||.2|.1F|T.3
            //  => ||T.2|T.3, canonical
            //  test .2seq => T 
            //  => ||.2|.1.3|.3|T.T
            //  => ||.2|.1.3|.3F
            //  => |T|.2|.1.3, canonical
            //  test .3ant => F 
            //  => ||.2|.1F|.3|T.2
            //  => ||T.2|.3|T.2
            //  => ||T.2|.3|TF
            //  => ||T.2|T.3, canonical
            //  test .3ant => T
            //  => ||.2|T.1|.3|T.2, canonical
            //  test .3seq => F 
            //  => ||.2|.1.3|F|T.2
            //  => ||.2|.1.3T, canonical
            //  test .3seq => T
            //  => ||.2|.1.3|T|T.2
            //  => ||.2|.1.3.2
            //  => |.2|.2|.1.3
            //  => |.2|T|.1.3, canonical
            // This is the first rule that I ever took the time to actually prove
            // cannot be implemented via wildcard analysis.
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("||.2|.1.3|.3|T.2");
                var canonicalFormula = Formula.Parse("||.2|T.3|.3|.1.2");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            // ||.1.2||.1.3|.2|T.1 => ||.1.2||T.2|.1.3
            // This formula should be reducible using wildcard analysis
            //  test .1 -> T in f.A
            //      => ||T.2||.1.3|.2|T.1
            //      => ||T.2||.1.3|F|T.1, wildcard @ 12
            //      => ||T.2||.1.3T
            //      => ||T.2|T|.1.3
            //  replace wildcard @ 12 -> F
            //  => ||.1.2||.1.3|.2|TF
            //  => ||.1.2||.1.3|T.2
            //  => ||.1.2||T.2|.1.3
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("||.1.2||.1.3|.2|T.1");
                var canonicalFormula = Formula.Parse("||.1.2||T.2|.1.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            // bug: ||.2|T.3||.1.2|.1.3 - is not reducible via wildcard analysis but should be.
            // Proof...
            //  test .2->T in f.A
            //      => ||T|T.3||.1.2|.1.3
            //      => |.3||.1.2|.1.3
            //      => |.3||.1.2|.1T
            //      => |.3||F.2|.1T, wildcard found
            //      => |.3|T|.1T
            //      => |.3.1
            //      => |.1.3
            //  therefore rewrite as....
            //  => ||.2|T.3||.1F|.1.3
            //  => ||.2|T.3|T|.1.3
            //  test .3 -> F in f.S
            //      => ||.2|T.3|T|.1F
            //      => ||.2|T.3|TT
            //      => ||.2|T.3F, wildcard found
            //      => T
            // therefore rewrite as....
            // => ||.2|TT|T|.1.3
            // => ||.2F|T|.1.3
            // => |T|T|.1.3
            // => |.1.3
            // 
            //  test .2->F in f.A, ||.2|T.3||.1.2|.1.3
            //      => ||F|T.3||.1.2|.1.3
            //      => |T||.1.2|.1.3, verified canonical
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("||.2|T.3||.1.2|.1.3");
                var canonicalFormula = Formula.Parse("|.1.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            // bug: |||.1.2|.1.3|.1|T|.2|T.3 - is not reducible via wildcard analysis but should be.
            // Proof...
            //  test .2->T in f.S
            //      => |||.1.2|.1.3|.1|T|T|T.3
            //      => |||.1.2|.1.3|.1|T.3
            //      test .3->F in f.S
            //          => |||.1.2|.1.3|.1|TF
            //          => |||.1.2|.1.3|.1T
            //          => |||F.2|F.3|.1T wildcard
            //          => ||TT|.1T 
            //          => |F|.1T 
            //          => T
            //      therefore .3->T in f.A...
            //      => |||.1.2|.1T|.1|T.3
            //      => |||F.2|.1T|.1|T.3 wildcard
            //      => ||T|.1T|.1|T.3
            //      => |.1|.1|T.3
            //      => |.1|T|T.3
            //      => |.1.3
            //  therefore rewrite as....
            //  => |||.1F|.1.3|.1|T|.2|T.3
            //  => ||T|.1.3|.1|T|.2|T.3
            //  test .3->F in f.A
            //      => ||T|.1F|.1|T|.2|T.3
            //      => ||T|.1F|.1|T|.2|T.3
            //      => ||TT|.1|T|.2|T.3
            //      => |F|.1|T|.2|T.3 wildcard
            //      => T
            //  therefore rewrite as....
            //  => ||T|.1.3|.1|T|.2|TT
            //  => ||T|.1.3|.1|T|.2F
            //  => ||T|.1.3|.1|TT
            //  => ||T|.1.3|.1F
            //  => ||T|.1.3T
            //  => |T|T|.1.3
            //  => |.1.3
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|||.1.2|.1.3|.1|T|.2|T.3");
                var canonicalFormula = Formula.Parse("|.1.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            // |.1||.2|T.3|.3|.1.2 should be reducible via wildcard analysis
            // proof...
            // test .1 -> F in f.A...
            //  => |F||.2|T.3|.3|.1.2 wildcard
            // therefore
            // => |.1||.2|T.3|.3|T.2 
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.1||.2|T.3|.3|.1.2"); // id=484
                var canonicalFormula = Formula.Parse("||.2.3||.1.2|.1.3"); //id=483
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            // |T||.1.2||T.1|T.2 => ||.1|T.2|.2|T.1
            // Should be reducible by rewriting using rule |T|a||Tb|Tc -> ||ab||ac 
            // proof...
            //  => ||.1|.1.2||.2|.1.2
            //  => ||.1|T.2||.2|.1T
            //  => ||.1|T.2||.2|T.1
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|T||.1.2||T.1|T.2");
                var canonicalFormula = Formula.Parse("||.1|T.2|.2|T.1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            //  ||.1|.2.3||F.2|.3|T.1 is not a valid reduction for ||.1|.2.3||.1.2|.3|T.1 (1480)
            //  Here's what NandReduction does...
            //  test .1->T in antecedent
            //      => ||T|.2.3||.1.2|.3|T.1
            //          test .2->F in antecedent 
            //          => ||T|F.3||.1.2|.3|T.1
            //          => ||TT||.1.2|.3|T.1
            //          => |F||.1.2|.3|T.1 wildcard
            //          => |T
            //      => ||T|.2.3||.1T|.3|T.1 .2->T in subsequent
            //  $   => ||T|.2.3||.1T|.3|TF !!!!!replacing .1 with F is also a wildcard!!!!!! (note: F is the opposite of the test value) 
            //      => ||T|.2.3||.1T|.3T
            //      => ||T|.2.3||.1T|T.3
            //      => ||T|.2.3||.1T|TT
            //      => ||T|.2.3||.1TF wildcard
            //      => ||T|.2.3T wildcard
            //  => ||.1|.2.3||F.2|.3|TF
            // The proof tracer should return the position of the first discovered wildcard.
            // However, the proof tracer did not previously recognize the wildcard at position 12 so it returned position 8 instead.
            // NOTE...
            // The proof tracer needs to be extended to recognize 'wildcard reductions' 
            // that replace a formula that contains an instance of the target test term with a constant.
            // Such reductions also identify wildcards.
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("||.1|.2.3||.1.2|.3|T.1");
                var canonicalFormula = Formula.Parse("|.3|.1|T.2");
                var testFormula = Formula.Parse("||.1|.2.3||F.2|.3|TF");
                var testFormula2 = Formula.Parse("||.1|.2.3||.1.2|.3|TF");
                Assert.AreEqual(TruthTable.NewTruthTable(testFormula2).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                Assert.AreEqual(TruthTable.NewTruthTable(testFormula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }


            // |||T.2|T.3||.2.3|.1|TT is not a valid reduction for |||T.2|T.3||.2.3|.1|T.2
            //  test .2->F in antecedent
            //  => |||TF|T.3||.2.3|.1|T.2
            //  => ||T|T.3||.2.3|.1|T.2
            //  => |.3||.2.3|.1|T.2
            //  => |.3||.2T|.1|T.2
            //  => |.3||T.2|.1|T.2
            //  => |.3||T.2|.1|TF !!!this is NOT a wildcard because the target term should be replaced with the OPPOSITE of the test value 
            //  => |.3||T.2|.1T
            // In order to to be able to check the value of the replacement against the value of the test value the test value 
            // had to be added to the ReductionTargetFinder proof tracer class.
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|||T.2|T.3||.2.3|.1|T.2");
                var canonicalFormula = Formula.Parse("|T||.1.3|.2.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }


            // tossed an error
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("||.2.3||.1.2|.3|T.1");
                var canonicalFormula = Formula.Parse("||.1.3||.1.2|.3|T.2");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var proof = new Proof();
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula, proof);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }


            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|T||.1|T.2|.1|T.3");
                var canonicalFormula = Formula.Parse("|.1|.2.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var proof = new Proof();
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula, proof);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            // |.3||.1|T.2|.2|.1.T => ||.1.2||.1.3|.2.3
            // => |T||.3|T|.1|T.2|.3|T|.2|.1T |a|bc -> |T||a|Tb|a|Tc  a= .3 b= |.1|T.2 c= |.2|.1T
            // => |T||.3|T|.1|T.2|.2|T|.3|.1T 
            // => |T| |.1|T|.3|T.2 |.2|T|.3|.1T 
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|.3||.1|T.2|.2|T.1");
                var canonicalFormula = Formula.Parse("||.1.2||.1.3|.2.3"); // verified canonical
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var proof = new Proof();
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula, proof);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }


            // |.1||.2|T.3|.3|T.2 should be reducible via wildcard analysis
            //  test .1->F in antecedent
            //      => |F||.2|T.3|.3|.1.2 wildcard
            //      => T
            //  wildcard in subsequent: .1->T 
            //  => |.1||.2|T.3|.3|T.2 
            // error... |||T.1|.2.3||.1.2|.3|T.2 is not a valid reduction for |||.1.2|.3|T.2||.2.3|.1|T.2 (3407)
            // |||T.1|.2.3||.1.2|.3|T.2 (3407) ->* |T||.1.2|.1.3) 
            // |||.1.2|.3|T.2||.2.3|.1|T.2 (6071) ->* |T||.1.3) 
            // Here's basically what the NRA is (currently) doing... 
            //  test |T.2 -> F in antecedent
            //      => |||.1.2|.3F||.2.3|.1|T.2
            //      => |||.1.2T||.2.3|.1|T.2
            //      => ||T|.1.2||.2.3|.1|T.2
            //      test .1 -> T in antecedent
            //          => ||T|T.2||.2.3|.1|T.2
            //          => |.2||.2.3|.1|T.2, cuz |T|T.1 => .1
            //          => |.2||T.3|.1|TT, cuz .2 is wildcard in seq when .2 -> F
            //          => |.2||T.3|.1F,  <- wildcard
            //      => ||T|.1.2||.2.3|F|T.2, <- wildcard
            // => |||.1.2|.3|T.2||.2.3|.1T, cuz |T.2 is sub wildcard 
            // => |||.1.2|.3|T.2||.2.3|T.1
            // => |||.1.2|.3|T.2||T.1|.2.3
            // The problem is that the NRA finds that |T.2 is a wildcard when its not.  
            // This happens because NRA fails to consider that |T.2 was modified while testing .1 -> T.
            // How to fix?...
            // 1)   This formula is not reducible via wildcard analysis, it requires a hard-coded ordering rule.
            //      The ordering rule is not yet implemented.
            //      If this rule were already implemented then this problem would go away because the NRA wouldn't even get
            //      as far as attempting wildcard analysis on this formula.
            //      But that's kinda cheating, it doesnt fix the problem but just avoids it.
            //      I guess what I would want is for the call to NandReduction to fail gracefully, by returning an un-reduced formula.
            // 2)   Extend the NRA proof trace to track all reductions in a 'context', a context that is maintained
            //      across sub-reductions.
            //      And disallow reductions that modify a subterm that is the subject of a parent context.
            //      This would cause NandReduction to fail gracefully.
            //      But it's a lot of work, and its complicated.
            //
            // Instead, I took a less labor-intensive fix as a shortcut.
            // This shortcut is NOT THE SAME as extending the NRA to track 'context's but its far less labor intensive.
            // The shortcut is to skip common terms that contain any other common terms as a subterm.
            // Using this scheme, |T.2 would be skipped as a common term because it contains another comment term, .2, within it.
            {
                var nonCanonicalformula = (Formulas.Nand)Formula.Parse("|||.1.2|.3|T.2||.2.3|.1|T.2");
                var canonicalFormula = Formula.Parse("|.1.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                Proof proof = new();
                var reducedFormula = NandReducer.NandReduction(nonCanonicalformula, proof);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

        }


        // |T||.1|T.2|.2|T.1 => ||.1.2||T.1|T.2
        //  => |||.1|T.2|T.2||.1|T.2|T|T.1   |T||a|bc -> ||a|Tb|a|Tc, where a= |.1|T.2, b = .2, c = |T.1
        //  => |||.1|T.2|T.2||.1|T.2|T|T.1 
        //  => |||.1|T.2|T.2||.1|T.2.1 
        //  => |||.1|T.2|T.2|.1|.1|T.2
        //  => |||.1|T.2|T.2|.1|T|T.2
        //  => |||.1|T.2|T.2|.1.2
        //  => |||.1T|T.2|.1.2
        //  => |||T.1|T.2|.1.2
        //  => ||.1.2||T.1|T.2
        [TestMethod]
        public void ReduceFormula456()
        {
            var nonCanonicalformula = (Nand)Formula.Parse("|T||.1|T.2|.2|T.1"); // id=456
            var canonicalFormula = Formula.Parse("||.1.2||T.1|T.2");
            Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
            var reducedFormula = NandReducer.NandReduction(nonCanonicalformula);
            Assert.AreEqual(canonicalFormula, reducedFormula);
        }
    }
}
