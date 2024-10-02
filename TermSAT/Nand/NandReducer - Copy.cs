using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TermSAT.Formulas;
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

            // if given formula is not a nand then it must be a variable or constant and is not reducible.
            if (!(startingFormula is Formulas.Nand))
            {
                return startingFormula;
            }

            // reduce the two subformulas.
            var startingNand = startingFormula as Formulas.Nand;
            var reducedAntecent = startingNand.Antecedent.NandReduction();
            var reducedSubsequent = startingNand.Subsequent.NandReduction();

            //
            // Now, repeatedly apply schemes to the top-level formula....
            //

            // The most common reduction is replacing variable instances with a constant...
            // ```
            // Notation:
            //  Let $f be a formula
            //  Let $f.A refers to f's antecedent
            //  Let $f.S refers to f's subsequent
            //  Let $f.A* be the set of all sub-formulas of A
            //
            // Algorithm:
            //  Assume f.A and f.S are canonical, ie not reducible.
            //  For any $a in f.A* except T and F...
            //      For all $c in [T,F]
            //          If
            //              replacing all instances of $a in f.A with $c causes f to reduce to
            //              an 'independent' formula that does not contain any instances of $a
            //          then
            //              any instances of $a in f.S may be replaced by $c ? F:T, the opposite of C.
            //  For all $s in S* except T and F...
            //      For all $c in [T,F]
            //          If
            //              replacing all instances of $s in f.S with $c causes f to reduce to 
            //              an 'independent' formula that does not contain any instances of $s
            //          then
            //              any instances of $s in f.A may be replaced by $c ? F:T, the opposite of C.
            // ```
            //
            // The above formula used to be simpler...
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

                var subAntecedent = reducedAntecent.ReplaceAll(subterm, Constant.TRUE);
                subAntecedent = subAntecedent.NandReduction();

                // if (subAntecedent == Constant.FALSE)
                if (!subAntecedent.AllSubterms.Contains(subterm))
                {
                    var subReducedConsequent = reducedSubsequent.ReplaceAll(subterm, Constant.FALSE);
                    subReducedConsequent = subReducedConsequent.NandReduction();

                    if (subReducedConsequent != reducedSubsequent)
                    {
                        return Formulas.Nand.NewNand(reducedAntecent, subReducedConsequent).NandReduction();
                    }
                }

                subAntecedent = reducedAntecent.ReplaceAll(subterm, Constant.FALSE);
                subAntecedent = subAntecedent.NandReduction();

                //if (subAntecedent == Constant.FALSE)
                if (!subAntecedent.AllSubterms.Contains(subterm))
                {
                    var subReducedConsequent = reducedSubsequent.ReplaceAll(subterm, Constant.TRUE);
                    subReducedConsequent = subReducedConsequent.NandReduction();

                    if (subReducedConsequent != reducedSubsequent)
                    {
                        return Formulas.Nand.NewNand(reducedAntecent, subReducedConsequent).NandReduction();
                    }
                }


                var subConsequent = reducedSubsequent.ReplaceAll(subterm, Constant.FALSE);
                subConsequent = subConsequent.NandReduction();
                //if (subConsequent == Constant.FALSE)
                if (!subConsequent.AllSubterms.Contains(subterm))
                {
                    var subReducedAntecent = reducedAntecent.ReplaceAll(subterm, Constant.TRUE);
                    subReducedAntecent = subReducedAntecent.NandReduction();

                    if (subReducedAntecent != reducedAntecent)
                    {
                        return Formulas.Nand.NewNand(subReducedAntecent, reducedSubsequent).NandReduction();
                    }
                }

                subConsequent = reducedSubsequent.ReplaceAll(subterm, Constant.TRUE);
                subConsequent = subConsequent.NandReduction();
                //if (subConsequent == Constant.FALSE)
                if (!subConsequent.AllSubterms.Contains(subterm))
                {
                    var subReducedAntecent = reducedAntecent.ReplaceAll(subterm, Constant.FALSE);
                    subReducedAntecent = subReducedAntecent.NandReduction();

                    if (subReducedAntecent != reducedAntecent)
                    {
                        return Formulas.Nand.NewNand(subReducedAntecent, reducedSubsequent).NandReduction();
                    }
                }

            }

            //
            // other rules...
            //


            if (reducedAntecent == Constant.TRUE)
            {
                if (reducedSubsequent is Constant constantConsequent)
                {
                    // |TT => F, and |TF => T
                    return constantConsequent.Equals(Constant.TRUE) ? Constant.FALSE : Constant.TRUE;
                }
                if (reducedSubsequent is Formulas.Nand nandConsequent)
                {
                    if (nandConsequent.Antecedent.Equals(Constant.TRUE))
                    {
                        // |T|T.1 => .1
                        return nandConsequent.Subsequent;
                    }
                }
                // |T.1 is canonical
                return Formulas.Nand.NewNand(Constant.TRUE, reducedSubsequent);
            }
            if (reducedAntecent == Constant.FALSE)
            {
                // |F.1 => T
                return Constant.TRUE;
            }

            if (reducedSubsequent.Equals(Constant.TRUE))
            {
                // |.1T => |T.1
                return Formulas.Nand.NewNand(Constant.TRUE, reducedAntecent).NandReduction();
            }
            if (reducedSubsequent == Constant.FALSE)
            {
                // |.1F => T
                return Constant.TRUE;
            }


            // |.2.1 => |.1.2 
            {
                if (reducedSubsequent.CompareTo(reducedAntecent) < 0)
                {
                    return Formulas.Nand.NewNand(reducedSubsequent, reducedAntecent).NandReduction();
                }
            }

            // |.2|T|.1.3 => |.1|T|.2.3 
            {
                if (reducedSubsequent is Formulas.Nand nand 
                    && nand.Antecedent == Constant.TRUE 
                    && nand.Subsequent is Formulas.Nand nandSubNand
                    && nandSubNand.Antecedent.CompareTo(reducedAntecent) < 0)
                {
                    return Formulas.Nand.NewNand(
                        nandSubNand.Antecedent,
                        Formulas.Nand.NewNand(
                            Constant.TRUE,
                            Formulas.Nand.NewNand(
                                reducedAntecent,
                                nandSubNand.Subsequent)))
                        .NandReduction();
                }
            }

            // |.1||T.2|T.3 => |T||.1.2|.1.3
            {
                if (reducedSubsequent is Formulas.Nand nand
                    && nand.Antecedent is Formulas.Nand nandAnt
                    && nand.Subsequent is Formulas.Nand nandSub
                    && nandAnt.Antecedent == Constant.TRUE
                    && nandSub.Antecedent == Constant.TRUE)
                {
                    return Formulas.Nand.NewNand(
                        Constant.TRUE,
                        Formulas.Nand.NewNand(
                            Formulas.Nand.NewNand(
                                reducedAntecent,
                                nandAnt.Subsequent),
                            Formulas.Nand.NewNand(
                                reducedAntecent,
                                nandSub.Subsequent)))
                        .NandReduction();
                }
            }

            // ||.1.2|.1|.2.3 => .1
            // This formula can be reduced automatically by extending the token replacement algorithm.  
            // Assume that .1, .2, and .3 represent any canonical, ie non-reducible, formulas.
            // Consider setting .2 to T in the antecedent.  
            // Setting the leftmost instance of .2 to T produces...
            //   => ||.1T|.1|.2.3 
            //   => ||.1T|F|.2.3, ...setting .1 in |.1T cause formula to reduce to T
            //   => ||.1TT, ...|F.1 => T
            //   => |TT.1, ...|.1T => |T.1
            //   => .1, ...|TT.1 => .1
            // Note that the result, .1, is not a function of the other instances of .2.
            // Therefore we can replace those other instances with F, resulting in...
            // => ||.1.2|.1|F.3 
            // => ||.1.2|.1T 
            // => ||F.2|.1T 
            // => |T|.1T 
            // => |.1
            {
                if (reducedAntecent is Formulas.Nand nandAnt
                    && reducedSubsequent is Formulas.Nand nandSub
                    && nandSub.Subsequent is Formulas.Nand nandSubSub
                    && nandAnt.Antecedent.Equals(nandSub.Antecedent)
                    && nandAnt.Subsequent.Equals(nandSubSub.Antecedent))
                {
                    return nandAnt.Antecedent;
                }
            }

            // ||.1.2|.1|T.2 => .1
            // ||.1.2|.1|.2.3 => .1
            {
                if (reducedAntecent is Formulas.Nand nandAnt // inherited
                    && reducedSubsequent is Formulas.Nand nandSub // inherited
                    && nandSub.Subsequent is Formulas.Nand nandSubSub // inherited
                    && nandAnt.Antecedent.Equals(nandSub.Antecedent) // inherited
                    && nandAnt.Subsequent.Equals(nandSubSub.Subsequent) 
                    && nandSubSub.Antecedent.Equals(Constant.TRUE))
                {
                    return nandAnt.Antecedent;
                }
            }

            // ||.1.2|.1|T.3 => |T|.1|.3|T.2
            {
                if (reducedAntecent is Formulas.Nand nandAnt // inherited
                    && reducedSubsequent is Formulas.Nand nandSub // inherited
                    && nandSub.Subsequent is Formulas.Nand nandSubSub // inherited
                    && nandAnt.Antecedent.Equals(nandSub.Antecedent) 
                    && nandSubSub.Antecedent.Equals(Constant.TRUE)) // inherited
                {
                    return Formulas.Nand.NewNand(
                        Constant.TRUE,
                        Formulas.Nand.NewNand(
                            nandAnt.Antecedent,
                            Formulas.Nand.NewNand(
                                nandSubSub.Subsequent,
                                Formulas.Nand.NewNand(
                                    Constant.TRUE,
                                    nandAnt.Subsequent))))
                        .NandReduction();
                }
            }

            // ||.1.2|.2|T.1 => .2
            // ||.1.2|.1|T.2 => .1
            // ||.1.2|.1|.2.3 => .1
            {
                if (reducedAntecent is Formulas.Nand nandAnt 
                    && reducedSubsequent is Formulas.Nand nandSub 
                    && nandSub.Subsequent is Formulas.Nand nandSubSub 
                    && nandAnt.Antecedent.Equals(nandSubSub.Subsequent) 
                    && nandAnt.Subsequent.Equals(nandSub.Antecedent)
                    && nandSubSub.Antecedent.Equals(Constant.TRUE)) 
                {
                    return nandAnt.Subsequent;
                }
            }

            // ||.1.2|.2|T.3 => |T|.2|.3|T.1
            // ||.1.2|.1|T.3 => |T|.1|.3|T.2
            {
                if (reducedAntecent is Formulas.Nand nandAnt // inherited
                    && reducedSubsequent is Formulas.Nand nandSub // inherited
                    && nandSub.Subsequent is Formulas.Nand nandSubSub // inherited
                    && nandAnt.Subsequent.Equals(nandSub.Antecedent)
                    && nandSubSub.Antecedent.Equals(Constant.TRUE)) // inherited
                {
                    return Formulas.Nand.NewNand(
                        Constant.TRUE,
                        Formulas.Nand.NewNand(
                            nandAnt.Subsequent,
                            Formulas.Nand.NewNand(
                                nandSubSub.Subsequent,
                                Formulas.Nand.NewNand(
                                    Constant.TRUE,
                                    nandAnt.Antecedent))))
                        .NandReduction();
                }
            }

            // ||.1.2|.2|.1.3 => .2
            // ||.1.2|.2|T.1 => .2
            // ||.1.2|.1|T.2 => .1
            // ||.1.2|.1|.2.3 => .1
            {
                if (reducedAntecent is Formulas.Nand nandAnt
                    && reducedSubsequent is Formulas.Nand nandSub
                    && nandSub.Subsequent is Formulas.Nand nandSubSub
                    && nandAnt.Antecedent.Equals(nandSubSub.Antecedent)
                    && nandAnt.Subsequent.Equals(nandSub.Antecedent))
                {
                    return nandAnt.Subsequent;
                }
            }

            // ||.1.3|.1|.2.3 => .1
            // @see...
            // ||.1.2|.2| T.1 => .2
            // ||.1.2|.1| T.2 => .1
            // ||.1.2|.1|.2.3 => .1
            {
                if (reducedAntecent is Formulas.Nand nandAnt
                    && reducedSubsequent is Formulas.Nand nandSub
                    && nandSub.Subsequent is Formulas.Nand nandSubSub
                    && nandAnt.Antecedent.Equals(nandSub.Antecedent)
                    && nandAnt.Subsequent.Equals(nandSubSub.Subsequent))
                {
                    return nandAnt.Antecedent;
                }
            }

            // ||.2.3|.3|.1.2 => .3
            // @see...
            // ||.1.3|.1|.2.3 => .1
            // ||.1.2|.2| T.1 => .2
            // ||.1.2|.1| T.2 => .1
            // ||.1.2|.1|.2.3 => .1
            {
                if (reducedAntecent is Formulas.Nand nandAnt
                    && reducedSubsequent is Formulas.Nand nandSub
                    && nandSub.Subsequent is Formulas.Nand nandSubSub
                    && nandAnt.Antecedent.Equals(nandSubSub.Subsequent)
                    && nandAnt.Subsequent.Equals(nandSub.Antecedent))
                {
                    return nandAnt.Subsequent;
                }
            }

            // ||.1.2|.1|.3|T.2 => |T|.1|.3|T.2
            // ||.1.2|.2|T.3 => |T|.2|.3|T.1
            // ||.1.2|.1|T.3 => |T|.1|.3|T.2
            {
                if (reducedAntecent is Formulas.Nand nandAnt // inherited
                    && reducedSubsequent is Formulas.Nand nandSub // inherited
                    && nandSub.Subsequent is Formulas.Nand nandSubSub // inherited
                    && nandSubSub.Subsequent is Formulas.Nand nandSubSubSub // inherited
                    && nandAnt.Antecedent.Equals(nandSub.Antecedent)
                    && nandAnt.Subsequent.Equals(nandSubSubSub.Subsequent) // inherited
                    && nandSubSubSub.Antecedent.Equals(Constant.TRUE))
                {
                    return Formulas.Nand.NewNand(
                        Constant.TRUE,
                        Formulas.Nand.NewNand(
                            nandAnt.Antecedent,
                            Formulas.Nand.NewNand(
                                nandSubSub.Antecedent,
                                Formulas.Nand.NewNand(
                                    Constant.TRUE,
                                    nandAnt.Subsequent))))
                        .NandReduction();
                }
            }

            // ||.1.2|.2|.3|T.1 => |T|.2|.3|T.1
            // @see...
            // ||.1.2|.1|.3|T.2 => |T|.1|.3|T.2
            // ||.1.2|.2|T.3 => |T|.2|.3|T.1
            // ||.1.2|.1|T.3 => |T|.1|.3|T.2
            {
                if (reducedAntecent is Formulas.Nand nandAnt // inherited
                    && reducedSubsequent is Formulas.Nand nandSub // inherited
                    && nandSub.Subsequent is Formulas.Nand nandSubSub // inherited
                    && nandSubSub.Subsequent is Formulas.Nand nandSubSubSub // inherited
                    && nandAnt.Antecedent.Equals(nandSubSubSub.Subsequent)
                    && nandAnt.Subsequent.Equals(nandSub.Antecedent) // inherited
                    && nandSubSubSub.Antecedent.Equals(Constant.TRUE))
                {
                    return Formulas.Nand.NewNand(
                        Constant.TRUE,
                        Formulas.Nand.NewNand(
                            nandAnt.Subsequent,
                            Formulas.Nand.NewNand(
                                nandSubSub.Antecedent,
                                Formulas.Nand.NewNand(
                                    Constant.TRUE,
                                    nandAnt.Antecedent))))
                        .NandReduction();
                }
            }

            // ||.1.2|.3|.1|T.2 => ||.1.2|.3|T.1
            // @see...
            // ||.1.2|.2|.3|T.1 => |T|.2|.3|T.1
            // ||.1.2|.1|.3|T.2 => |T|.1|.3|T.2
            // ||.1.2|.2|T.3 => |T|.2|.3|T.1
            // ||.1.2|.1|T.3 => |T|.1|.3|T.2
            {
                if (reducedAntecent is Formulas.Nand nandAnt // inherited
                    && reducedSubsequent is Formulas.Nand nandSub // inherited
                    && nandSub.Subsequent is Formulas.Nand nandSubSub // inherited
                    && nandSubSub.Subsequent is Formulas.Nand nandSubSubSub // inherited
                    && nandAnt.Antecedent.Equals(nandSubSub.Antecedent)
                    && nandAnt.Subsequent.Equals(nandSubSubSub.Subsequent) 
                    && nandSubSubSub.Antecedent.Equals(Constant.TRUE))
                {
                    return Formulas.Nand.NewNand(
                        reducedAntecent,
                        Formulas.Nand.NewNand(
                            nandSub.Antecedent,
                            Formulas.Nand.NewNand(
                                Constant.TRUE,
                                nandAnt.Antecedent)))
                        .NandReduction();
                }
            }

            // ||.1.2|.3|.2|T.1 => ||.1.2|.3|T.2
            // @see...
            // ||.1.2|.3|.1|T.2 => ||.1.2|.3|T.1
            // ||.1.2|.2|.3|T.1 => |T|.2|.3|T.1
            // ||.1.2|.1|.3|T.2 => |T|.1|.3|T.2
            // ||.1.2|.2|T.3 => |T|.2|.3|T.1
            // ||.1.2|.1|T.3 => |T|.1|.3|T.2
            {
                if (reducedAntecent is Formulas.Nand nandAnt // inherited
                    && reducedSubsequent is Formulas.Nand nandSub // inherited
                    && nandSub.Subsequent is Formulas.Nand nandSubSub // inherited
                    && nandSubSub.Subsequent is Formulas.Nand nandSubSubSub // inherited
                    && nandAnt.Antecedent.Equals(nandSubSubSub.Subsequent)
                    && nandAnt.Subsequent.Equals(nandSubSub.Antecedent)
                    && nandSubSubSub.Antecedent.Equals(Constant.TRUE))
                {
                    return Formulas.Nand.NewNand(
                        reducedAntecent,
                        Formulas.Nand.NewNand(
                            nandSub.Antecedent,
                            Formulas.Nand.NewNand(
                                Constant.TRUE,
                                nandAnt.Subsequent)))
                        .NandReduction();
                }
            }

            // ||.1|T.2|.1|.2.3 => |T|.1|.2.3
            // @see...
            // |.1||T.2|T.3 => |T||.1.2|.1.3
            {
                if (reducedAntecent is Formulas.Nand nandAnt 
                    && reducedSubsequent is Formulas.Nand nandSub
                    && nandSub.Subsequent is Formulas.Nand nandSubSub
                    && nandAnt.Subsequent is Formulas.Nand nandAntSub 
                    && nandAnt.Antecedent.Equals(nandSub.Antecedent)
                    && nandAntSub.Subsequent.Equals(nandSubSub.Antecedent)
                    && nandAntSub.Antecedent == Constant.TRUE)
                {
                    return Formulas.Nand.NewNand(
                        Constant.TRUE,
                        Formulas.Nand.NewNand(
                            nandAnt.Antecedent,
                            Formulas.Nand.NewNand(
                                nandAntSub.Subsequent,
                                nandSubSub.Subsequent)))
                        .NandReduction();
                }
            }


            // the formula cannot be further reduced
            return Formulas.Nand.NewNand(reducedAntecent, reducedSubsequent);
        }
    }
}
