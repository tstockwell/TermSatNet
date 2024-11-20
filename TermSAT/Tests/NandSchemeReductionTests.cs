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
            // Answer: replacing just one term instance during wildcard substitution is not logically correct.
            // It works when going the other direction because there's only one matching term instance.  
            // Wow, I sure did waste a gd lot of time implementing and testing reduction mapping for nothing.
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
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            // |T||.1|T.2|.3|T.1 => ||.1.2||T.1|T.3
            {
                var nonCanonicalformula = (Nand)Formula.Parse("|T||.1|T.2|.3|T.1");
                var canonicalFormula = Formula.Parse("||.1.2||T.1|T.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

        }
        [TestMethod]
        public void BasicNandReductionTests()
        {

            {
                var nonCanonicalformula = (Nand)Formula.Parse("|.1T");
                var canonicalFormula = Formula.Parse("|T.1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Nand)Formula.Parse("|TT");
                var canonicalFormula = Formula.Parse("F");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Nand)Formula.Parse("|.2.1");
                var canonicalFormula = Formula.Parse("|.1.2");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Nand)Formula.Parse("|.1|.1.2");
                var canonicalFormula = Formula.Parse("|.1|T.2");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Nand)Formula.Parse("|T|T.1");
                var canonicalFormula = Formula.Parse(".1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Nand)Formula.Parse("|.1T");
                var canonicalFormula = Formula.Parse("|T.1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Nand)Formula.Parse("|.2|.1T");
                var canonicalFormula = Formula.Parse("|.2|T.1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Nand)Formula.Parse("|.2|.3|.1T");
                var canonicalFormula = Formula.Parse("|.2|.3|T.1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            ReduceFormula58();

            ReduceFormula104();

            {
                var nonCanonicalformula = (Nand)Formula.Parse("||.1|T.2|.2|.1T");
                var canonicalFormula = Formula.Parse("||.1|T.2|.2|T.1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Nand)Formula.Parse("||T.2||T.3|.1F");
                var canonicalFormula = Formula.Parse("|.3|T.2");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
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
                var nonCanonicalformula = (Nand)Formula.Parse("||.2.3||T.3|.1.2");
                var canonicalFormula = Formula.Parse("||.2.3||T.3|.1.2");
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }



            {
                var nonCanonicalformula = (Nand)Formula.Parse("|.1||.2.3||T.3|.1.2");
                var canonicalFormula = Formula.Parse("|.1||.2.3||T.2|T.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Nand)Formula.Parse("||.1|.2.3|.3|T.1");
                var canonicalFormula = Formula.Parse("||.1|T.3|.3|.1.2");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            {
                var nonCanonicalformula = (Nand)Formula.Parse("||.1|.2.3|.2|T.1");
                // => 
                var canonicalFormula = Formula.Parse("||.1|T.2|.2|.1.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Nand)Formula.Parse("||.2.3||T.3|.1T");
                var canonicalFormula = Formula.Parse("||.2.3||T.1|T.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Nand)Formula.Parse("|.3||.1.2||T.1|.2.3");
                var canonicalFormula = Formula.Parse("|.3||.1.2||T.1|T.2");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                var nonCanonicalformula = (Nand)Formula.Parse("|.2|T|.1.3");
                var canonicalFormula = Formula.Parse("|.1|T|.2.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
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
                var nonCanonicalformula = (Nand)Formula.Parse("|||T.1|T.3||.1.3|.2.3");
                var canonicalFormula = Formula.Parse("|T||.1.3|.2.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            // |||T.2|T.3|.3|T||T.1|T.2
            {
                //var nonCanonicalFormula = (Formulas.Nand)Formula.Parse("|||T.2|T.3|.3|T||T.1|T.2");
                //var nonCanonicalFormula = (Formulas.Nand)Formula.Parse("|||T.2|T.3|T|T||T.1|T.2");
                //var nonCanonicalFormula = (Formulas.Nand)Formula.Parse("|||T.2|T.3||T.1|T.2");
                //var nonCanonicalFormula = (Formulas.Nand)Formula.Parse("|||T.2|T.3||T.1|T.2");
                //var nonCanonicalFormula = (Formulas.Nand)Formula.Parse("|T||T.2||T.3|T.1");
                var nonCanonicalFormula = Formula.Parse("|T||.1.3|T.2");
                var nonCanonicalTT = TruthTable.NewTruthTable(nonCanonicalFormula).ToString();
                var canonicalFormula = Formula.Parse("|T||T.2|.1.3");
                var canonicalTT = TruthTable.NewTruthTable(canonicalFormula).ToString();
                var reducedFormula = NandReducer.Reduce(nonCanonicalFormula);
                Assert.AreEqual(nonCanonicalTT, canonicalTT);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            {
                // SELECT f.*, c.Text FROM FormulaRecords f
                // join(SELECT * FROM FormulaRecords WHERE CreateCompletionMarker = 1) c on c.TruthValue = f.TruthValue
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

                var nonCanonicalformula = (Nand)Formula.Parse("|||.1.2|.1.3|.3|T||T.1|T.2");
                var canonicalFormula = Formula.Parse("|T||.1.2|.1.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
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
                var nonCanonicalformula = (Nand)Formula.Parse("||.1.2|.3|.1|T.2");
                var canonicalFormula = Formula.Parse("||.1.2|.3|T.1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            // ||.1|.2.3|.2.3
            // ||.2.3|.1|.2.3
            //  => ||.2.3|T.1, since  |T||.1.2|.2 => |T||T.1|.2
            //  => ||T.1|.2.3
            {
                var nonCanonicalformula = (Nand)Formula.Parse("||.2.3|.1|.2.3");
                var canonicalFormula = Formula.Parse("||T.1|.2.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            // |T||.1|.2.3|.2.3
            //  => |T||T.1|.2.3, since  |T||.1.2|.2 => |T||T.1|.2
            {
                var nonCanonicalformula = (Nand)Formula.Parse("|T||.1|.2.3|.2.3");
                var canonicalFormula = Formula.Parse("|T||T.1|.2.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            // ||.1.2|.3|T.2 is not a valid reduction for ||.1.2|.3|.1.2
            {
                var nonCanonicalformula = (Nand)Formula.Parse("||.1.2|.3|.1.2");
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreNotEqual(reducedFormula, Formula.Parse("||.1.2|.3|T.2"));
            }
            // error: ||.1.2|.2.3 is not a valid reduction for ||.1.2|T||.1|.2.3|.2.3
            {
                var nonCanonicalformula = (Nand)Formula.Parse("||.1.2|T||.1|.2.3|.2.3");
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
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
                var nonCanonicalformula = (Nand)Formula.Parse("|||T.1|T.3||.1.3|.2.3");
                var canonicalFormula = Formula.Parse("|T||.1.3|.2.3");
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
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
                var nonCanonicalformula = (Nand)Formula.Parse("||.1.3||.2.3||.1.2|.1.3");
                var canonicalFormula = Formula.Parse("||.1.2|.1.3");
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
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
                var nonCanonicalformula = (Nand)Formula.Parse("|T||T.1||T.2|T.3");
                var canonicalFormula = Formula.Parse("||.2|T.1|.3|T.1");
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }


            // error... ||.1|T.2|.3|T.2 is not a valid reduction for |.1||T.2||T.1|T.3
            //  => |.1||T.2||TT|T.3
            //  => |.1||T.2|F|T.3
            //  => |.1||T.2T
            //  => |.1.2
            {
                var nonCanonicalformula = (Nand)Formula.Parse("|.1||T.2||T.1|T.3");
                var canonicalFormula = Formula.Parse("|.1.2");
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
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
                var nonCanonicalformula = (Nand)Formula.Parse("||.2|.1.3|.3|T.2");
                var canonicalFormula = Formula.Parse("||.2|T.3|.3|.1.2");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
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
                var nonCanonicalformula = (Nand)Formula.Parse("||.1.2||.1.3|.2|T.1");
                var canonicalFormula = Formula.Parse("||.1.2||T.2|.1.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
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
                var nonCanonicalformula = (Nand)Formula.Parse("||.2|T.3||.1.2|.1.3");
                var canonicalFormula = Formula.Parse("|.1.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
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
                var nonCanonicalformula = (Nand)Formula.Parse("|||.1.2|.1.3|.1|T|.2|T.3");
                var canonicalFormula = Formula.Parse("|.1.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            // |.1||.2|T.3|.3|.1.2 should be reducible via wildcard analysis
            // proof...
            // test .1 -> F in f.A...
            //  => |F||.2|T.3|.3|.1.2 wildcard
            // therefore
            // => |.1||.2|T.3|.3|T.2 
            {
                var nonCanonicalformula = (Nand)Formula.Parse("|.1||.2|T.3|.3|.1.2"); // id=484
                var canonicalFormula = Formula.Parse("||.2.3||.1.2|.1.3"); //id=483
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            // |T||.1.2||T.1|T.2 => ||.1|T.2|.2|T.1
            // Should be reducible by rewriting using rule |T|a||Tb|Tc -> ||ab||ac 
            // proof...
            //  => ||.1|.1.2||.2|.1.2
            //  => ||.1|T.2||.2|.1T
            //  => ||.1|T.2||.2|T.1
            {
                var nonCanonicalformula = (Nand)Formula.Parse("|T||.1.2||T.1|T.2");
                var canonicalFormula = Formula.Parse("||.1|T.2|.2|T.1");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

            //  ||.1|.2.3||F.2|.3|T.1 is not a valid reduction for ||.1|.2.3||.1.2|.3|T.1 (1480)
            //  Here's what Reduce does...
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
                var nonCanonicalformula = (Nand)Formula.Parse("||.1|.2.3||.1.2|.3|T.1");
                var canonicalFormula = Formula.Parse("|.3|.1|T.2");
                var testFormula = Formula.Parse("||.1|.2.3||F.2|.3|TF");
                var testFormula2 = Formula.Parse("||.1|.2.3||.1.2|.3|TF");
                Assert.AreEqual(TruthTable.NewTruthTable(testFormula2).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                Assert.AreEqual(TruthTable.NewTruthTable(testFormula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
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
                var nonCanonicalformula = (Nand)Formula.Parse("|||T.2|T.3||.2.3|.1|T.2");
                var canonicalFormula = Formula.Parse("|T||.1.3|.2.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }


            // tossed an error
            {
                var nonCanonicalformula = (Nand)Formula.Parse("||.2.3||.1.2|.3|T.1");
                var canonicalFormula = Formula.Parse("||.1.3||.1.2|.3|T.2");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }


            {
                var nonCanonicalformula = (Nand)Formula.Parse("|T||.1|T.2|.1|T.3");
                var canonicalFormula = Formula.Parse("|.1|.2.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }
            // |.3||.1|T.2|.2|.1.T => ||.1.2||.1.3|.2.3
            // => |T||.3|T|.1|T.2|.3|T|.2|.1T |a|bc -> |T||a|Tb|a|Tc  a= .3 b= |.1|T.2 c= |.2|.1T
            // => |T||.3|T|.1|T.2|.2|T|.3|.1T 
            // => |T| |.1|T|.3|T.2 |.2|T|.3|.1T 
            {
                var nonCanonicalformula = (Nand)Formula.Parse("|.3||.1|T.2|.2|T.1");
                var canonicalFormula = Formula.Parse("||.1.2||.1.3|.2.3"); // verified canonical
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
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
            //      I guess what I would want is for the call to Reduce to fail gracefully, by returning an un-reduced formula.
            // 2)   Extend the NRA proof trace to track all reductions in a 'context', a context that is maintained
            //      across sub-reductions.
            //      And disallow reductions that modify a subterm that is the subject of a parent context.
            //      This would cause Reduce to fail gracefully.
            //      But it's a lot of work, and its complicated.
            //
            // Instead, I took a less labor-intensive fix as a shortcut.
            // This shortcut is NOT THE SAME as extending the NRA to track 'context's but its far less labor intensive.
            // The shortcut is to skip common terms that contain any other common terms as a subterm.
            // Using this scheme, |T.2 would be skipped as a common term because it contains another comment term, .2, within it.
            {
                var nonCanonicalformula = (Nand)Formula.Parse("|||.1.2|.3|T.2||.2.3|.1|T.2");
                var canonicalFormula = Formula.Parse("|.1.3");
                Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
                var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
                Assert.AreEqual(canonicalFormula, reducedFormula);
            }

        }

        /// <summary>
        /// |.1||T.2|T.3 => |T||.1.2|.1.3
        /// The new way...
        /// Reducible directly by wildcard swapping .1 <-> T
        /// => |T||.1.2|.1.3
        /// 
        /// The old way...
        /// Reducible by rewriting using rule |a|bc -> |T||a|Tb|a|Tc.
        /// => |T||.1|T|T.2|.1|T|T.3 
        /// => |T||.1|T|T.2|.1.3
        /// => |T||.1.2|.1.3
        /// </summary>
        [TestMethod]
        public void ReduceFormula104()
        {
            var nonCanonicalformula = (Nand)Formula.Parse("|.1||T.2|T.3");
            var canonicalFormula = Formula.Parse("|T||.1.2|.1.3");
            Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
            var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
            Assert.AreEqual(canonicalFormula, reducedFormula);
        }

        [TestMethod]
        public void ReduceFormula58()
        {
            var nonCanonicalformula = (Nand)Formula.Parse("|.2|.3|.1.2");
            var canonicalFormula = Formula.Parse("|.2|.3|T.1");
            Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
            var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
            Assert.AreEqual(canonicalFormula, reducedFormula);
        }


        /// <summary>
        /// |T||.1|T.2|.2|T.1 => ||.1.2||T.1|T.2
        /// This formula cannot be reduced using just the current wildcard analysis algorithm and constant elimination rules.
        /// Also the two terms in this formula are a [critical pair](https://en.wikipedia.org/wiki/Critical_pair_(term_rewriting)).
        ///     cuz...
        ///         |T||.1|.1.2|.2|.1.2 ; can be reduced to...
        ///             => ||.1.2||T.1|T.2 ; using wildcard swapping |.1.2 <-> T
        ///         or => |T||.1|T.2|.2|T.1 ; by reducing descendants first
        ///         Therefore (||.1.2||T.1|T.2, |T||.1|T.2|.2|T.1) is a critical pair.
        /// Therefore, these two terms form a new rule.
        /// The [Knuth-Bendix](https://en.wikipedia.org/wiki/Knuth%E2%80%93Bendix_completion_algorithm) way of 
        /// extending the current system would be to add this production rule to the set of rules in our system.  
        /// However, don't be thinking that the Knuth-Bendix procedure will ever terminate when applied to this system, it won't.
        /// That's not proven but I have good reasons for thinking that.  
        /// So, in order to create a complete reduction system (in the Knuth-Bendix sense) we'll need some reduction 
        /// method that's more powerful than simple production rules.  
        /// Instead, the 'NandSAT Way' of extending the system has been to extend the wildcard analysis algorithm 
        /// in a way that will implement the same functionality as the new production rule. 
        /// I have never yet failed to be able to extend wildcard analysis to cover new formulas.  
        /// It's hoped that the 'NandSat Way' will result in a system that can be proven to be complete 
        /// by showing that extending the number of variables in the system creates no more rules.
        /// I believe that it will turn out that all production rules can be classified into a fixed set of 
        /// extensions to the wildcard analysis algorithm, that's why I'm doing this.
        /// The 'NandSAT Way' lead to the development of wildcard substitution, then wildcard swapping, 
        /// then term substitution and term swapping, and showed me that I needed to to abandon the 'distributive' 
        /// rules that used to be in the system.  
        /// 
        /// In order to be able to implement the rule |T||.1|T.2|.2|T.1 => ||.1.2||T.1|T.2, 
        /// the system was simplified by eliminating constants.
        /// Removing constants also removes the need for wildcard substitution and wildcard swapping.
        /// One argument against such a change to the system is that it requires formulas to be longer.  
        /// For instance, you cant write |T.1, you have to write |.1.1.
        /// And this means formulas require more storage and are harder to understand, **for humans***.
        /// But that's not really true.
        /// When storing formulas as strings** they are longer, , that's true, and in that sense they require more storage.  
        /// BUT... it doesn't require more storage **on a computer** and it isn't harder **for a computer** to understand them.  
        ///     > hint, in NandSAT, all terms are singletons, so repeating them in a formula takes less storage than 
        ///         using constants to represent some of those instances.
        /// The advantage to getting rid of constants is that it removes the need for this particular production rule 
        /// while leaving everything else working the same.  That is...
        ///         |T||.1|.1.2|.2|.1.2 ; will be written as...
        ///         |||.1|.1.2|.2|.1.2||.1|.1.2|.2|.1.2 ; and will be reduced to...
        ///             => ||.1.2||.1|.1.2|.2|.1.2 ; using wildcard substitution |.1.2 <-> T 
        ///             => ||.1.2||.1.1|.2.2 ; using wildcard substitution |.1.2 <-> T
        /// Everything else will work the same.
        /// Problem solved.
        /// 
        /// Note...
        /// Elsewhere in the documentation there is a computer-based proof that the wildcard analysis algorithm can reduce 
        /// all formulas of 3 or less variables.
        /// There is also a computer-based proof that extending the system to four or more variables requires no new rules, 
        /// and therefore the wildcard analysis algorithm is complete.
        /// 
        /// the old way, using a 'distributive' rule....
        /// Prove |T||.1|T.2|.2|T.1 => ||.1.2||T.1|T.2
        ///  => |||.1|T.2|T.2||.1|T.2|T|T.1   |T||a|bc -> ||a|Tb|a|Tc, where a= |.1|T.2, b = .2, c = |T.1
        ///  => |||.1|T.2|T.2||.1|T.2.1 
        ///  => |||.1|T.2|T.2|.1|.1|T.2
        ///  => |||.1|T.2|T.2|.1|T|T.2
        ///  => |||.1|T.2|T.2|.1.2
        ///  => ||.1.2||.1|T.2|T.2
        ///  => ||.1.2||.1T|T.2
        ///  => ||.1.2||T.1|T.2
        /// </summary>
        [TestMethod]
        public void ReduceFormula456()
        {
            var nonCanonicalformula = (Nand)Formula.Parse("|T||.1|T.2|.2|T.1"); // id=456
            var canonicalFormula = Formula.Parse("||.1.2||T.1|T.2");
            Assert.AreEqual(TruthTable.NewTruthTable(nonCanonicalformula).ToString(), TruthTable.NewTruthTable(canonicalFormula).ToString());
            var reducedFormula = NandReducer.Reduce(nonCanonicalformula);
            Assert.AreEqual(canonicalFormula, reducedFormula);
        }
    }
}
