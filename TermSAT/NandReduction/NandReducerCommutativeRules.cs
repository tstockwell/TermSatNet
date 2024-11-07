using System.Collections.Immutable;
using System.Linq;
using TermSAT.Common;
using TermSAT.Formulas;

namespace TermSAT.NandReduction;

public static class NandReducerCommutativeRules
{

    public static Reduction ReduceCommutativeFormulas(this Nand startingNand, Proof proof)
    {

        // |.2.1 => |.1.2 
        // A pure ordering rule, because the length doesn't change, just the symbols are rearranged.
        // NandSAT formulas are not commutative, nor associative, the 'smaller' formula must go first.
        {
            if (startingNand.Subsequent.CompareTo(startingNand.Antecedent) < 0)
            {
                var reducedFormula = Formulas.Nand.NewNand(startingNand.Subsequent, startingNand.Antecedent);
                var mapping = Enumerable.Repeat(-1, 1)
                    .Concat(Enumerable.Range(startingNand.Antecedent.Length + 1, startingNand.Subsequent.Length))
                    .Concat(Enumerable.Range(1, startingNand.Antecedent.Length))
                    .ToImmutableList();
                var result = new Reduction(startingNand, reducedFormula, "|.2.1 => |.1.2", mapping);
                return result;
            }
        }

        // |.2|T|.1.3 => |.1|T|.2.3.  
        // A pure ordering rule, because the length doesn't change, just the symbols are rearranged.
        {
            if (startingNand.Subsequent is Formulas.Nand nand
                && nand.Antecedent == Constant.TRUE
                && nand.Subsequent is Formulas.Nand nandSubNand
                && nandSubNand.Antecedent.CompareTo(startingNand.Antecedent) < 0)
            {
                var reducedFormula = Formulas.Nand.NewNand(
                    nandSubNand.Antecedent,
                    Formulas.Nand.NewNand(
                        Constant.TRUE,
                        Formulas.Nand.NewNand(
                            startingNand.Antecedent,
                            nandSubNand.Subsequent)));
                // maybe this is just lazy coding, but it's the easiest way to deal with all order conditions
                if (reducedFormula.CompareTo(startingNand) < 0)
                {
                    var mapping = Enumerable.Repeat(0, 1) // |
                        .Concat(Enumerable.Range(startingNand.Antecedent.Length + 4, nandSubNand.Antecedent.Length)) // .1
                        .Concat(Enumerable.Range(startingNand.Antecedent.Length + 1, 3)) // |T|
                        .Concat(Enumerable.Range(1, startingNand.Antecedent.Length)) //.2
                        .Concat(Enumerable.Range(startingNand.Antecedent.Length + nandSubNand.Antecedent.Length + 4, nandSubNand.Subsequent.Length)) //.3
                        .ToImmutableList();
                    var result = new Reduction(startingNand, reducedFormula, "|.2|T|.1.3 => |.1|T|.2.3", mapping);
                    return result;
                }
            }
        }



        //// |.1||T.2|T.3 => |T||.1.2|.1.3
        //// Reducible by rewriting using rule |a|bc -> |T||a|Tb|a|Tc.
        //// => |T||.1|T|T.2|.1|T|T.3 ,|a|bc -> |T||a|Tb|a|Tc
        //// => |T||.1|T|T.2|.1.3
        //// => |T||.1.2|.1.3
        //{
        //    if (startingNand.Subsequent is Formulas.Nand nandSub
        //        && nandSub.Antecedent is Formulas.Nand nandSubAnt
        //        && nandSub.Subsequent is Formulas.Nand nandSubSub
        //        && nandSubAnt.Antecedent.Equals(Constant.TRUE)
        //        && nandSubSub.Antecedent.Equals(Constant.TRUE))
        //    {
        //        var reducedFormula = Formulas.Nand.NewNand(
        //            Constant.TRUE,
        //            Formulas.Nand.NewNand(
        //                Formulas.Nand.NewNand(
        //                    startingNand.Antecedent,
        //                    nandSubAnt.Subsequent),
        //                Formulas.Nand.NewNand(
        //                    startingNand.Antecedent,
        //                    nandSubSub.Subsequent)));

        //        if (reducedFormula.CompareTo(startingNand) < 0)
        //        {
        //            // |.1||T.2|T.3 => |T||.1.2|.1.3
        //            var mapping = Enumerable.Repeat(0, 1) // |
        //                .Concat(Enumerable.Repeat(-1, 3)) // T|| note: ||.1.2|.1.3, |.1.2, and |.1.3 dont exist in the starting formula
        //                .Concat(Enumerable.Range(1, startingNand.Antecedent.Length)) // .1
        //                .Concat(Enumerable.Range(startingNand.Antecedent.Length + 4, nandSubAnt.Subsequent.Length)) //.2
        //                .Concat(Enumerable.Range(-1, 1)) // | note:|.1.3 doesnt exists in starting formula
        //                .Concat(Enumerable.Range(1, startingNand.Antecedent.Length)) // .1
        //                .Concat(Enumerable.Range(startingNand.Antecedent.Length + nandSubAnt.Subsequent.Length + 6, nandSubSub.Subsequent.Length)) // .3
        //                .ToImmutableList(); 

        //            reductionResult = new Reduction(startingNand, reducedFormula, "|.1||T.2|T.3 => |T||.1.2|.1.3", mapping);
        //            return true;
        //        }
        //    }
        //}



        // ||.1.2|.2|T.3 => |T|.2|.3|T.1
        // reducible by rewriting using rule ||ab|ac -> |T|a||Tb|Tc
        //  => |T|.2||T.1|T|T.3 where a= .2 b=.1 c= |T.3
        //  => |T|.2||T.1.3
        //  => |T|.2|.3|T.1
        {
            if (startingNand.Antecedent is Formulas.Nand nandAnt // inherited
                && startingNand.Subsequent is Formulas.Nand nandSub // inherited
                && nandSub.Subsequent is Formulas.Nand nandSubSub // inherited
                && nandAnt.Subsequent.Equals(nandSub.Antecedent)
                && nandSubSub.Antecedent.Equals(Constant.TRUE)) // inherited
            {
                var reducedFormula = Formulas.Nand.NewNand(
                    Constant.TRUE,
                    Formulas.Nand.NewNand(
                        nandAnt.Subsequent,
                        Formulas.Nand.NewNand(
                            nandSubSub.Subsequent,
                            Formulas.Nand.NewNand(
                                Constant.TRUE,
                                nandAnt.Antecedent))));

                if (reducedFormula.CompareTo(startingNand) < 0)
                {
                    var mappingOne = Enumerable.Range(2, nandAnt.Antecedent.Length);
                    var mappingTrue = Enumerable.Range(nandAnt.Length + nandSub.Antecedent.Length + 3, 1);
                    var mappingThree = Enumerable.Range(nandAnt.Length + nandSub.Antecedent.Length + 4, nandSubSub.Subsequent.Length);
                    var mapping = SystemExtensions.ConcatAll(
                        Enumerable.Repeat(-1, nandAnt.Subsequent.Length + 4),    // |T|.2|
                        mappingThree,               // .3
                        Enumerable.Repeat(-1, 1),   // | note: |T.1 does not exist in starting formula
                        mappingTrue,                // T
                        mappingOne                  // .1
                    ).ToImmutableList();

                    var result = new Reduction(startingNand, reducedFormula, "||.1.2|.2|T.3 => |T|.2|.3|T.1", mapping);
                    return result;
                }
            }
        }


        // ||.1|.2.3|.2|T.1 => ||.1|T.2|.2|.1.3
        // A pure ordering rule, just a reordering of formula symbols
        // NOT reducible via wildcard analysis (verified)
        // Basically the .3 and the T trade places
        // An example of a 'T-slider' rule.
        {
            if (startingNand.Antecedent is Formulas.Nand nandAnt
                && nandAnt.Subsequent is Formulas.Nand nandAntSub
                && startingNand.Subsequent is Formulas.Nand nandSub
                && nandSub.Subsequent is Formulas.Nand nandSubSub
                && nandAntSub.Antecedent.Equals(nandSub.Antecedent)
                && nandAnt.Antecedent.Equals(nandSubSub.Subsequent)
                && nandSubSub.Antecedent.Equals(Constant.TRUE)
                && nandAnt.Antecedent.CompareTo(nandAntSub.Antecedent) < 0
                && nandAntSub.Antecedent.CompareTo(nandAntSub.Subsequent) < 0)
            {
                var reducedFormula = Formulas.Nand.NewNand(
                    Formulas.Nand.NewNand(
                        nandAnt.Antecedent,
                        Formulas.Nand.NewNand(
                            Constant.TRUE,
                            nandAntSub.Antecedent)),
                    Formulas.Nand.NewNand(
                        nandAntSub.Antecedent,
                        Formulas.Nand.NewNand(
                            nandAnt.Antecedent,
                            nandAntSub.Subsequent)));
                if (reducedFormula.CompareTo(startingNand) < 0)
                {
                    var mapping1_1 = Enumerable.Range(2, nandAnt.Antecedent.Length);
                    var mapping2_1 = Enumerable.Range(nandAnt.Antecedent.Length + 2, nandAntSub.Antecedent.Length);
                    var mapping3 = Enumerable.Range(nandAnt.Length + nandSub.Antecedent.Length + 4, nandAntSub.Subsequent.Length);
                    var mapping2_2 = Enumerable.Range(nandAnt.Length + 2, nandAntSub.Antecedent.Length);
                    var mappingT = Enumerable.Range(nandAnt.Length + nandSub.Antecedent.Length + 3, 1);
                    var mapping1_2 = Enumerable.Range(nandAnt.Length + nandSub.Antecedent.Length + 4, nandAnt.Antecedent.Length);
                    var mapping = SystemExtensions.ConcatAll(
                        Enumerable.Repeat(0, nandAnt.Antecedent.Length + 3),    // ||.1|
                        mappingT,                                               // T
                        mapping2_1,                                             // .2
                        Enumerable.Repeat(nandAnt.Length + 1, 1),               // |
                        mapping2_2,                                             // .2
                        Enumerable.Repeat(nandAnt.Length + nandSub.Antecedent.Length + 2, 1),               // |
                        mapping1_2,                                             // .1
                        mapping3                                                // .3
                    ).ToImmutableList();

                    var result = new Reduction(startingNand, reducedFormula, "||.1|.2.3|.2|T.1 => ||.1|T.2|.2|.1.3", mapping);
                    return result;
                }
            }
        }

        // ||.1|.2.3|.3|T.1 => ||.1|T.3|.3|.1.2
        // A pure ordering rule, its simply a reordering of the symbols in the formula
        // An example of a 'T-slider' rule.
        {
            if (startingNand.Antecedent is Formulas.Nand nandAnt
                && nandAnt.Subsequent is Formulas.Nand nandAntSub
                && startingNand.Subsequent is Formulas.Nand nandSub
                && nandSub.Subsequent is Formulas.Nand nandSubSub
                && nandAntSub.Subsequent.Equals(nandSub.Antecedent)
                && nandAnt.Antecedent.Equals(nandSubSub.Subsequent)
                && nandSubSub.Antecedent.Equals(Constant.TRUE)
                //&& nandAnt.Antecedent.CompareTo(nandAntSub.Antecedent) < 0
                && nandAntSub.Antecedent.CompareTo(nandAntSub.Subsequent) < 0)
            {
                var reducedFormula = Formulas.Nand.NewNand(
                    Formulas.Nand.NewNand(
                        nandAnt.Antecedent,
                        Formulas.Nand.NewNand(
                            Constant.TRUE,
                            nandAntSub.Subsequent)),
                    Formulas.Nand.NewNand(
                        nandAntSub.Subsequent,
                        Formulas.Nand.NewNand(
                            nandAnt.Antecedent,
                            nandAntSub.Antecedent)));

                if (reducedFormula.CompareTo(startingNand) < 0)
                {
                    var mapping1_1 = Enumerable.Range(2, nandAnt.Antecedent.Length);
                    var mapping2 = Enumerable.Range(nandAnt.Antecedent.Length + 2, nandAntSub.Antecedent.Length);
                    var mapping3_1 = Enumerable.Range(nandAnt.Antecedent.Length + nandAntSub.Antecedent.Length + 3, nandAntSub.Subsequent.Length);
                    var mapping3_2 = Enumerable.Range(nandAnt.Antecedent.Length + 2, nandSub.Antecedent.Length);
                    var mappingT = Enumerable.Range(nandAnt.Length + nandSub.Antecedent.Length + 3, 1);
                    var mapping1_2 = Enumerable.Range(nandAnt.Length + nandSub.Antecedent.Length + 4, nandAnt.Antecedent.Length);
                    var reductionMapping = SystemExtensions.ConcatAll(
                        Enumerable.Repeat(0, nandAnt.Antecedent.Length + 3),    // ||.1|
                        mappingT,                                               // T
                        mapping3_1,                                             // .3
                        Enumerable.Repeat(nandAnt.Length + 1, 1),               // |
                        mapping3_2,                                             // .3
                        Enumerable.Repeat(nandAnt.Length + nandSub.Antecedent.Length + 2, 1),               // |
                        mapping1_2,                                             // .1
                        mapping2                                                // .2
                    ).ToImmutableList();

                    var result = new Reduction(startingNand, reducedFormula, "||.1|.2.3|.3|T.1 => ||.1|T.3|.3|.1.2", reductionMapping);
                    return result;
                }
            }
        }

        return Reduction.NoChange(startingNand);
    }
}