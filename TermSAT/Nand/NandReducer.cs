using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;
using TermSAT.SchemeReducer;

namespace TermSAT.Nand
{
    public static class NandReducer
    {
        /// <summary>
        /// Reduces formulas composed of constants, variables, and nand operators.  
        /// Repeatedly discovers and reduces reducible sub-formulas using 'schemes'.
        /// Schemes are algorithms that can discover and reduces non-canonical formulas.
        /// </summary>
        public static Formula NandReduction(this Formula startingFormula)
        {
            var reducedFormula = startingFormula;
            var Validate = () =>
            {
                var reducedTT = reducedFormula.GetTruthTable().ToString();
                var startingTT = startingFormula.GetTruthTable().ToString();
                if (!reducedTT.Equals(startingTT))
                {
                    Debug.Assert(false, $"{reducedFormula} is not a valid reduction for {startingFormula}");
                }
            };

            // if given formula is not a nand then it must be a variable or constant and is not reducible.
            if (!(startingFormula is Formulas.Nand))
            {
                goto Completed;
            }

            // reduce the two subformulas.
            var startingNand = startingFormula as Formulas.Nand;
            var reducedAntecent = startingNand.Antecedent.NandReduction();
            var reducedSubsequent = startingNand.Subsequent.NandReduction();

            //
            // basic rules.
            // These are production rules that were discovered by the 'nand-rule-generation-3' script that 
            // are not reducible by the 'nand resolution' algorithm, see below.
            // These rules are considered 'atomic'.  
            // Other than constant elimination, these rules are about ordering rules of equal length.
            //

            // constant elimination
            if (reducedAntecent == Constant.TRUE)
            {
                if (reducedSubsequent is Constant constantConsequent)
                {
                    // |TT => F, and |TF => T
                    reducedFormula = constantConsequent.Equals(Constant.TRUE) ? Constant.FALSE : Constant.TRUE;
                    Validate();
                    goto Completed;
                }
                if (reducedSubsequent is Formulas.Nand nandConsequent)
                {
                    if (nandConsequent.Antecedent.Equals(Constant.TRUE))
                    {
                        // |T|T.1 => .1
                        reducedFormula= nandConsequent.Subsequent;
                        Validate();
                        goto Completed;
                    }
                }
                // |T.1 is canonical
                return Formulas.Nand.NewNand(Constant.TRUE, reducedSubsequent);
            }
            if (reducedAntecent == Constant.FALSE)
            {
                // |F.1 => T
                reducedFormula= Constant.TRUE;
                Validate();
                goto Completed;
            }
            if (reducedSubsequent.Equals(Constant.TRUE))
            {
                // |.1T => |T.1
                reducedFormula= Formulas.Nand.NewNand(Constant.TRUE, reducedAntecent).NandReduction();
                Validate();
                goto Completed;
            }
            if (reducedSubsequent == Constant.FALSE)
            {
                // |.1F => T
                reducedFormula= Constant.TRUE;
                Validate();
                goto Completed;
            }


            // nand is commutative, the order of operatives doesnt matter.
            //  but, when reducing, the 'smaller' formula goes first.
            // Since the 'nand reduction system' (NRA) is symmetrical...
            //      That is, since the NRA replaces in .2, subterms in .1 that cause .1 to reduce to F and
            //      replaces in .1, subterms in .2 that cause .2 to reduce to F.
            // |.2.1 => |.1.2 
            {
                if (reducedSubsequent.CompareTo(reducedAntecent) < 0)
                {
                    reducedFormula= Formulas.Nand.NewNand(reducedSubsequent, reducedAntecent).NandReduction();
                    Validate();
                    goto Completed;
                }
            }

            // |.2|T|.1.3 => |.1|T|.2.3 
            // I think this must be a distributive reduction since its not commutative and its not associative.
            {
                if (reducedSubsequent is Formulas.Nand nand
                    && nand.Antecedent == Constant.TRUE
                    && nand.Subsequent is Formulas.Nand nandSubNand
                    && nandSubNand.Antecedent.CompareTo(reducedAntecent) < 0)
                {
                    reducedFormula= Formulas.Nand.NewNand(
                        nandSubNand.Antecedent,
                        Formulas.Nand.NewNand(
                            Constant.TRUE,
                            Formulas.Nand.NewNand(
                                reducedAntecent,
                                nandSubNand.Subsequent)))
                        .NandReduction();
                    Validate();
                    goto Completed;
                }
            }


            // |.1||T.2|T.3 => |T||.1.2|.1.3
            // I think this is a form of distributive reduction
            // reducible by rewriting using rule |a||Tb|Tc -> |T||ab|ac 
            {
                if (reducedSubsequent is Formulas.Nand nandSub
                    && nandSub.Antecedent is Formulas.Nand nandSubAnt
                    && nandSub.Subsequent is Formulas.Nand nandSubSub
                    && nandSubAnt.Antecedent.Equals(Constant.TRUE)
                    && nandSubSub.Antecedent.Equals(Constant.TRUE))
                {
                    var reduction = Formulas.Nand.NewNand(
                        Constant.TRUE,
                        Formulas.Nand.NewNand(
                            Formulas.Nand.NewNand(
                                reducedAntecent,
                                nandSubAnt.Subsequent),
                            Formulas.Nand.NewNand(
                                reducedAntecent,
                                nandSubSub.Subsequent)))
                        .NandReduction();

                    // maybe this is just lazy coding, but it's the easiest way to deal with all conditions
                    if (reduction.CompareTo(startingFormula) < 0)
                    {
                        reducedFormula= reduction;
                        Validate();
                        goto Completed;
                    }
                }
            }


            // ||.1.2|.1|T.3 => |T|.1|.3|T.2
            // distributive replacement
            // reducible by rewriting using rule ||ab|ac -> |T|a||Tb|Tc
            // => |T|.1||T.2|T|T.3 
            // => |T|.1||T.2.3 
            // => |T|.1|.3|T.2
            {
                if (reducedAntecent is Formulas.Nand nandAnt // inherited
                    && reducedSubsequent is Formulas.Nand nandSub // inherited
                    && nandSub.Subsequent is Formulas.Nand nandSubSub // inherited
                    && nandAnt.Antecedent.Equals(nandSub.Antecedent)
                    && nandSubSub.Antecedent.Equals(Constant.TRUE)) // inherited
                {
                    reducedFormula= Formulas.Nand.NewNand(
                        Constant.TRUE,
                        Formulas.Nand.NewNand(
                            nandAnt.Antecedent,
                            Formulas.Nand.NewNand(
                                nandSubSub.Subsequent,
                                Formulas.Nand.NewNand(
                                    Constant.TRUE,
                                    nandAnt.Subsequent))))
                        .NandReduction();
                    Validate();
                    goto Completed;
                }
            }

            // ||.1.2|.2|T.3 => |T|.2|.3|T.1
            // distributive replacement
            // reducible by rewriting using rule ||ab|ac -> |T|a||Tb|Tc
            //  => |T|.2||.1T|T|T.3
            //  => |T|.2||.1T.3
            //  => |T|.2||.3|T.1
            // I'm pretty sure that this rule may be removed without consequence
            {
                if (reducedAntecent is Formulas.Nand nandAnt // inherited
                    && reducedSubsequent is Formulas.Nand nandSub // inherited
                    && nandSub.Subsequent is Formulas.Nand nandSubSub // inherited
                    && nandAnt.Subsequent.Equals(nandSub.Antecedent)
                    && nandSubSub.Antecedent.Equals(Constant.TRUE)) // inherited
                {
                    reducedFormula= Formulas.Nand.NewNand(
                        Constant.TRUE,
                        Formulas.Nand.NewNand(
                            nandAnt.Subsequent,
                            Formulas.Nand.NewNand(
                                nandSubSub.Subsequent,
                                Formulas.Nand.NewNand(
                                    Constant.TRUE,
                                    nandAnt.Antecedent))))
                        .NandReduction();
                    Validate();
                    goto Completed;
                }
            }


            // consolidate common subformula in nand arguments, using negation: .1. in this case
            // ||.1.2|.1|.3|T.2 => |T|.1|.3|T.2
            // reducible by rewriting using rule ||ab|ac -> |T|a||Tb|Tc
            //  => |T|.1||T.2|T|.3|T.2
            //  => |T|.1||T.2|T|.3|T.F
            //  => |T|.1||T.2|T|.3T
            //  => |T|.1||T.2.3
            //  => |T|.1|.3|T.2
            {
                if (reducedAntecent is Formulas.Nand nandAnt // inherited
                    && reducedSubsequent is Formulas.Nand nandSub // inherited
                    && nandSub.Subsequent is Formulas.Nand nandSubSub // inherited
                    && nandSubSub.Subsequent is Formulas.Nand nandSubSubSub // inherited
                    && nandAnt.Antecedent.Equals(nandSub.Antecedent)
                    && nandAnt.Subsequent.Equals(nandSubSubSub.Subsequent) // inherited
                    && nandSubSubSub.Antecedent.Equals(Constant.TRUE))
                {
                    reducedFormula= Formulas.Nand.NewNand(
                        Constant.TRUE,
                        Formulas.Nand.NewNand(
                            nandAnt.Antecedent,
                            Formulas.Nand.NewNand(
                                nandSubSub.Antecedent,
                                Formulas.Nand.NewNand(
                                    Constant.TRUE,
                                    nandAnt.Subsequent))))
                        .NandReduction();
                    Validate();
                    goto Completed; 
                }
            }

            // ||.1|.2.3|.2|T.1 => ||.1|T.2|.2|.1.3
            // reducible by rewriting using rule ||ab||Tac -> |T|a||Tb|Tc
            // @see |.2|T|.1.3 => |.1|T|.2.3 
            {
                if (reducedAntecent is Formulas.Nand nandAnt 
                    && nandAnt.Subsequent is Formulas.Nand nandAntSub
                    && reducedSubsequent is Formulas.Nand nandSub 
                    && nandSub.Subsequent is Formulas.Nand nandSubSub
                    && nandAntSub.Antecedent.Equals(nandSub.Antecedent)
                    && nandAnt.Antecedent.Equals(nandSubSub.Subsequent)
                    && nandSubSub.Antecedent.Equals(Constant.TRUE)
                    && nandAnt.Antecedent.CompareTo(nandAntSub.Antecedent) < 0
                    && nandAntSub.Antecedent.CompareTo(nandAntSub.Subsequent) < 0)
                {
                    var reduction = Formulas.Nand.NewNand(
                        Formulas.Nand.NewNand(
                            nandAnt.Antecedent,
                            Formulas.Nand.NewNand(
                                Constant.TRUE,
                                nandAntSub.Antecedent)),
                        Formulas.Nand.NewNand(
                            nandAntSub.Antecedent,
                            Formulas.Nand.NewNand(
                                nandAnt.Antecedent,
                                nandAntSub.Subsequent)))
                        .NandReduction();

                    // maybe this is just lazy coding, but it's the easiest way to deal with all other conditions
                    if (reduction.CompareTo(startingFormula) < 0)
                    {
                        reducedFormula= reduction;
                        Validate();
                        goto Completed;
                    }
                }
            }

            // ||.1|.2.3|.3|T.1 => ||.1|T.3|.3|.1.2
            {
                if (reducedAntecent is Formulas.Nand nandAnt
                    && nandAnt.Subsequent is Formulas.Nand nandAntSub
                    && reducedSubsequent is Formulas.Nand nandSub
                    && nandSub.Subsequent is Formulas.Nand nandSubSub
                    && nandAntSub.Subsequent.Equals(nandSub.Antecedent)
                    && nandAnt.Antecedent.Equals(nandSubSub.Subsequent)
                    && nandSubSub.Antecedent.Equals(Constant.TRUE)
                    && nandAnt.Antecedent.CompareTo(nandAntSub.Antecedent) < 0
                    && nandAntSub.Antecedent.CompareTo(nandAntSub.Subsequent) < 0)
                {
                    var reduction = Formulas.Nand.NewNand(
                        Formulas.Nand.NewNand(
                            nandAnt.Antecedent,
                            Formulas.Nand.NewNand(
                                Constant.TRUE,
                                nandAntSub.Subsequent)),
                        Formulas.Nand.NewNand(
                            nandAntSub.Subsequent,
                            Formulas.Nand.NewNand(
                                nandAnt.Antecedent,
                                nandAntSub.Antecedent)))
                        .NandReduction();

                    // maybe this is just lazy coding, but it's the easiest way to deal with all other conditions
                    if (reduction.CompareTo(startingFormula) < 0)
                    {
                        reducedFormula= reduction;
                        Validate();
                        goto Completed;
                    }
                }
            }

            // |||.1.2|.1.3|.3|T||T.1|T.2, Id = 5478
            // why is this formula not marked as subsumed in the database?
            //  cuz it should...
            //  => |||.1.2|.1.3|.3|T||T.1|T.2, cuz setting .1 to F in the antecedent reduces to an independent formula...
            //  => |||.1.2|.1.3|.3|T||T.T|T.2
            //  => |||.1.2|.1.3|.3|T|F|T.2
            //  => |||.1.2|.1.3|.3|TT
            //  => |||.1.2|.1.3|.3F
            //  => |||.1.2|.1.3T
            //  => |T||.1.2|.1.3
            //  here's the proof that setting .1 to F in the antecedent reduces to an independent formula...
            //  => |||F.2|F.3|.3|T||T.1|T.2
            //  => ||TT|.3|T||T.1|T.2
            //  => |F|.3|T||T.1|T.2
            //  => T
            //
            // This formula illustrates the need to extend the NRA 


            //
            // This is the 'nand resolution' algorithm.
            // The nand resolution algorithm discovers subformulas that may be replaced
            // with a constant based on the following theorem...
            // ```
            // Notation:
            //  Let $f be a formula
            //  Let $f.A refers to f's antecedent
            //  Let $f.S refers to f's subsequent
            //  Let $f.A* be the set of all sub-formulas of A
            // Theorem:
            //  Assume f.A and f.S are canonical, ie not reducible.
            //  For any $a in f.A* except T and F...
            //      For all $c in [T,F]
            //          If
            //              replacing all instances of $a in f.A with $c causes f to reduce to
            //              an 'independent' formula that does not contain any instances of $a
            //          then
            //              any instances of $a in f.S may be replaced by $c ? F:T, the opposite of C.
            //  Similarly for all $s in S* except T and F...
            //      For all $c in [T,F]
            //          If
            //              replacing all instances of $s in f.S with $c causes f to reduce to 
            //              an 'independent' formula that does not contain any instances of $s
            //          then
            //              any instances of $s in f.A may be replaced by $c ? F:T, the opposite of C.
            // ```
            //
            // The above theorem used to be simpler, see below.
            // I originally came up with simpler idea by thinking about formulas in a logical way.
            // That is, this simpler idea can be justified using an argument based on propositional calculus.  
            // I came up with the more refined idea above by thinking about formulas as strings and
            // propositional calculus as a production system.
            // ```
            //  Let f be a formula
            //  Let A be f's antecedent
            //  Let S be f's subsequent
            //  Let A* be the set of all subformula of A
            //  Let S* be the set of all subformula of S
            //  Let C be a constant, that is, T or F.
            //  For any $a in A*...
            //      For all $c in [T,F]
            //          If replacing all instances of $a in A with $c causes A to reduce to F 
            //          then any instances of $a in S may be replaced by $c ? F:T, the opposite of C.
            //  For any $s in S*...
            //      For all $c in [T,F]
            //          If replacing all instances of $s in S with C causes S to reduce to F 
            //          then any instances of $s in A may be replaced by $c ? F:T, the opposite of C.
            // ```
            var commonTerms = reducedAntecent.AllSubterms
                .Intersect(reducedSubsequent.AllSubterms)
                .Where(s => !(s is Constant));

            foreach (var subterm in commonTerms)
            {
                {
                    var replaced =
                        Formulas.Nand.NewNand(
                            reducedAntecent.ReplaceAll(subterm, Constant.TRUE),
                            reducedSubsequent);
                    var reduction = replaced.NandReduction();
                    var reductionTerms = reduction.AllSubterms
                        .Except(replaced.AllSubterms)
                        .Where(s => !(s is Constant));

                    // if (subAntecedent == Constant.FALSE)
                    //if (!reductionTerms.Any() && !reduction.AllSubterms.Contains(subterm))
                    if (!reduction.AllSubterms.Contains(subterm))
                    {
                        var subReducedConsequent = reducedSubsequent
                            .ReplaceAll(subterm, Constant.FALSE)
                            .NandReduction();

                        if (subReducedConsequent != reducedSubsequent)
                        {
                            reducedFormula=  Formulas.Nand.NewNand(reducedAntecent, subReducedConsequent).NandReduction();
                            Validate();
                            goto Completed;
                        }
                    }
                }

                {
                    var replaced =
                        Formulas.Nand.NewNand(
                            reducedAntecent.ReplaceAll(subterm, Constant.FALSE),
                            reducedSubsequent);
                    var reduction = replaced.NandReduction();
                    var reductionTerms = reduction.AllSubterms
                        .Except(replaced.AllSubterms)
                        .Where(s => !(s is Constant));

                    //if (subAntecedent == Constant.FALSE)
                    //if (!reductionTerms.Any() && !reduction.AllSubterms.Contains(subterm))
                    if (!reduction.AllSubterms.Contains(subterm))
                    {
                        var subReducedConsequent = reducedSubsequent
                            .ReplaceAll(subterm, Constant.TRUE)
                            .NandReduction();

                        if (subReducedConsequent != reducedSubsequent)
                        {
                            reducedFormula=  Formulas.Nand.NewNand(reducedAntecent, subReducedConsequent).NandReduction();
                            Validate();
                            goto Completed;
                        }
                    }
                }
                
                {
                    var replaced =
                        Formulas.Nand.NewNand(
                            reducedAntecent,
                            reducedSubsequent.ReplaceAll(subterm, Constant.FALSE));
                    var reduction = replaced.NandReduction();
                    var reductionTerms = reduction.AllSubterms
                        .Except(replaced.AllSubterms)
                        .Where(s => !(s is Constant));


                    //if (subConsequent == Constant.FALSE)
                    //if (!reductionTerms.Any() && !reduction.AllSubterms.Contains(subterm))
                    if (!reduction.AllSubterms.Contains(subterm))
                    {
                        var subReducedAntecent = reducedAntecent
                            .ReplaceAll(subterm, Constant.TRUE)
                            .NandReduction();

                        if (subReducedAntecent != reducedAntecent)
                        {
                            reducedFormula=  Formulas.Nand.NewNand(subReducedAntecent, reducedSubsequent).NandReduction();
                            Validate();
                            goto Completed;
                        }
                    }
                }

                {
                    var replaced =
                        Formulas.Nand.NewNand(
                            reducedAntecent,
                            reducedSubsequent.ReplaceAll(subterm, Constant.TRUE));
                    var reduction = replaced.NandReduction();
                    var reductionTerms = reduction.AllSubterms
                        .Except(replaced.AllSubterms)
                        .Where(s => !(s is Constant));


                    //if (subConsequent == Constant.FALSE)
                    //if (!reductionTerms.Any() && !reduction.AllSubterms.Contains(subterm))
                    if (!reduction.AllSubterms.Contains(subterm))
                    {
                        var subReducedAntecent = reducedAntecent
                            .ReplaceAll(subterm, Constant.FALSE)
                            .NandReduction();

                        if (subReducedAntecent != reducedAntecent)
                        {
                            reducedFormula=  Formulas.Nand.NewNand(subReducedAntecent, reducedSubsequent).NandReduction();
                            Validate();
                            goto Completed;
                        }
                    }
                }
            }

            // the formula cannot be further reduced
            reducedFormula= Formulas.Nand.NewNand(reducedAntecent, reducedSubsequent);
            Validate();
            goto Completed;

            Completed:
            {
                return reducedFormula;
            }
        }
    }
}
