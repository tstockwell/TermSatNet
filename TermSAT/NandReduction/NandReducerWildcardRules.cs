using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using TermSAT.Common;
using TermSAT.Formulas;

namespace TermSAT.NandReduction;

public static class NandReducerWildcardRules
{

    /// <summary>
    //
    // This is the 'wildcard analysis' algorithm.
    // Wildcard analysis discovers subformulas that may be replaced 
    // with a constant based on the following algorithm...
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
    //              any instances of $a in f.S may be StartingNand by $c ? F:T, the opposite of C.
    //  Similarly for all $s in S* except T and F...
    //      For all $c in [T,F]
    //          If
    //              replacing all instances of $s in f.S with $c causes f to reduce to 
    //              an 'independent' formula that does not contain any instances of $s
    //          then
    //              any instances of $s in f.A may be StartingNand by $c ? F:T, the opposite of C.
    // ```
    //
    // The above theorem used to be simpler, see below.
    // I originally came up with a simpler idea by thinking about formulas in a logical way.
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
    //          then any instances of $a in S may be StartingNand by $c ? F:T, the opposite of C.
    //  For any $s in S*...
    //      For all $c in [T,F]
    //          If replacing all instances of $s in S with C causes S to reduce to F 
    //          then any instances of $s in A may be StartingNand by $c ? F:T, the opposite of C.
    // ```
    /// 
    /// </summary>
    public static Reduction ReduceWildcards(this Nand startingNand, Proof proof)
    {
        IEnumerable<Formula> commonTerms =
            startingNand.Antecedent.AsFlatTerm().Distinct().Where(f => !(f is Constant))
                .Intersect(startingNand.Subsequent.AsFlatTerm().Distinct().Where(f => !(f is Constant)));

        // skip common terms that contain any other common terms as a subterm.
        var independentTerms = commonTerms.Where(f => !commonTerms.Where(t => t.Length < f.Length && 0 <= f.PositionOf(t)).Any());

        foreach (var subterm in independentTerms)
        {
            {
                var replacedAntecedent = startingNand.Antecedent.ReplaceAll(subterm, Constant.TRUE).NandReduction();
                var replaced = Nand.NewNand(replacedAntecedent, startingNand.Subsequent);
                var targetFinder = new WildcardAnalyzer(replaced, subterm, Constant.TRUE, proof);
                var reduction = replaced.NandReduction(targetFinder);

                // if (subAntecedent == Constant.FALSE)
                if (targetFinder.FoundReductionTarget())
                {
#if DEBUG
                    if (!(replacedAntecedent.Length + 1 <= targetFinder.ReductionPosition))
                    {
                        throw new Exception("the reduction target should be in the subsequent");
                    }
                    if (!(subterm.Equals(replaced.GetFormulaAtPosition(targetFinder.ReductionPosition))))
                    {
                        throw new Exception($"an instance of {subterm} should have been found in {replaced} at position {targetFinder.ReductionPosition}");
                    }
#endif
                    var replaced2 = replaced.ReplaceAt(targetFinder.ReductionPosition, Constant.FALSE);

                    if (!replaced2.Equals(replaced))
                    {
                        var nandReplaced2 = replaced2 as Nand;
                        Debug.Assert(nandReplaced2.Antecedent.Equals(replaced.Antecedent));

                        var reducedFormula = Nand.NewNand(startingNand.Antecedent, nandReplaced2.Subsequent);
                        var reductionMapping = SystemExtensions.ConcatAll(
                            Enumerable.Range(0, targetFinder.ReductionPosition),  // everything left of the replacement target
                            Enumerable.Range(-1, 1), // T
                                                     // everything right of the replacement target
                            Enumerable.Range(targetFinder.ReductionPosition + subterm.Length, reducedFormula.Length - targetFinder.ReductionPosition - 1)
                        ).ToImmutableList();

                        var result= new Reduction(
                            startingNand,
                            reducedFormula,
                            $"wildcard in subsequent: {subterm}->F",
                            reductionMapping);
                        return result;
                    }
                }
            }

            {
                var replacedAntecedent = startingNand.Antecedent.ReplaceAll(subterm, Constant.FALSE).NandReduction();
                var replaced = Formulas.Nand.NewNand(replacedAntecedent, startingNand.Subsequent);
                var targetFinder = new WildcardAnalyzer(replaced, subterm, Constant.FALSE, proof);
                var reduction = replaced.NandReduction(targetFinder);

                if (targetFinder.FoundReductionTarget())
                {
                    Debug.Assert(replacedAntecedent.Length < targetFinder.ReductionPosition, $"the reduction target should be in the subsequent, TargetPosition = {targetFinder.ReductionPosition}");
                    Debug.Assert(subterm.Equals(replaced.GetFormulaAtPosition(targetFinder.ReductionPosition)), $"an instance of {subterm} should have been found in {replaced} at position {targetFinder.ReductionPosition}");

                    var replaced2 = replaced.ReplaceAt(targetFinder.ReductionPosition, Constant.TRUE);

                    if (!replaced2.Equals(replaced))
                    {
                        if (replaced2 is Formulas.Nand nandReplaced2 && nandReplaced2.Antecedent.Equals(replaced.Antecedent))
                        {
                            var reducedFormula = Formulas.Nand.NewNand(startingNand.Antecedent, nandReplaced2.Subsequent);
                            var reductionMapping = SystemExtensions.ConcatAll(
                                Enumerable.Range(0, targetFinder.ReductionPosition),  // everything left of the replacement target
                                Enumerable.Range(-1, 1), // T
                                                         // everything right of the replacement target
                                Enumerable.Range(targetFinder.ReductionPosition + subterm.Length, reducedFormula.Length - targetFinder.ReductionPosition - 1)
                            ).ToImmutableList();

                            var result = new Reduction(
                                startingNand,
                                reducedFormula,
                                $"wildcard in subsequent: {subterm}->T",
                                reductionMapping);
                            return result;
                        }
                    }
                }
            }

            {
                var replacedSubsequent = startingNand.Subsequent.ReplaceAll(subterm, Constant.TRUE).NandReduction();
                var replaced = Formulas.Nand.NewNand(startingNand.Antecedent, replacedSubsequent);
                var targetFinder = new WildcardAnalyzer(replaced, subterm, Constant.TRUE, proof);
                var reduction = replaced.NandReduction(targetFinder);

                // if (subAntecedent == Constant.FALSE)
                //if (!reductionTerms.Any() && !reduction.AllSubterms.Contains(Subterm))
                if (targetFinder.FoundReductionTarget())
                {
#if DEBUG
                    if (startingNand.Antecedent.Length < targetFinder.ReductionPosition)
                    {
                        throw new Exception("the reduction target should be in the antecedent");
                    }
#endif
                    Debug.Assert(subterm.Equals(replaced.GetFormulaAtPosition(targetFinder.ReductionPosition)), "an instance of the subterm should have been found");
                    var replaced2 = replaced.ReplaceAt(targetFinder.ReductionPosition, Constant.FALSE);

                    if (!replaced2.Equals(replaced))
                    {
                        if (replaced2 is Formulas.Nand nandReplaced2 && nandReplaced2.Subsequent.Equals(replaced.Subsequent))
                        {
                            var reducedFormula = Formulas.Nand.NewNand(nandReplaced2.Antecedent, startingNand.Subsequent);
                            var reductionMapping = SystemExtensions.ConcatAll(
                                Enumerable.Range(0, targetFinder.ReductionPosition),  // everything left of the replacement target
                                Enumerable.Range(-1, 1), // T
                                                         // everything right of the replacement target
                                Enumerable.Range(targetFinder.ReductionPosition + subterm.Length, reducedFormula.Length - targetFinder.ReductionPosition - 1)
                            ).ToImmutableList();

                            var result = new Reduction(
                                startingNand,
                                reducedFormula,
                                $"wildcard in antecedent: {subterm}->F",
                                reductionMapping);
                            return result;
                        }
                    }
                }
            }

            {
                var replacedSubsequent = startingNand.Subsequent.ReplaceAll(subterm, Constant.FALSE).NandReduction();
                var replaced = Formulas.Nand.NewNand(startingNand.Antecedent, replacedSubsequent);
                var targetFinder = new WildcardAnalyzer(replaced, subterm, Constant.FALSE, proof);
                var reduction = replaced.NandReduction(targetFinder);

                // if (subAntecedent == Constant.FALSE)
                if (targetFinder.FoundReductionTarget())
                {
                    Debug.Assert(targetFinder.ReductionPosition <= startingNand.Antecedent.Length, "the reduction target should be in the antecedent");
                    Debug.Assert(subterm.Equals(replaced.GetFormulaAtPosition(targetFinder.ReductionPosition)), "an instance of the subterm should have been found");
                    var replaced2 = replaced.ReplaceAt(targetFinder.ReductionPosition, Constant.TRUE);

                    if (!replaced2.Equals(replaced))
                    {
                        if (replaced2 is Formulas.Nand nandReplaced2 && nandReplaced2.Subsequent.Equals(replaced.Subsequent))
                        {
                            var reducedFormula = Formulas.Nand.NewNand(nandReplaced2.Antecedent, startingNand.Subsequent);
                            var reductionMapping = SystemExtensions.ConcatAll(
                                Enumerable.Range(0, targetFinder.ReductionPosition),  // everything left of the replacement target
                                Enumerable.Range(-1, 1), // T
                                                         // everything right of the replacement target
                                Enumerable.Range(targetFinder.ReductionPosition + subterm.Length, reducedFormula.Length - targetFinder.ReductionPosition - 1)
                            ).ToImmutableList();

                            var result = new Reduction(
                                startingNand,
                                reducedFormula,
                                $"wildcard in antecedent: {subterm}->T",
                                reductionMapping);
                            return result;
                        }
                    }
                }
            }
        }

        return Reduction.NoChange(startingNand);
    }
}