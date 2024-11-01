using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using TermSAT.Common;
using TermSAT.Formulas;

namespace TermSAT.Nand;

public static class NandReducer
{
    /// <summary>
    /// Reduces formulas composed of constants, variables, and nand operators to their canonical form.  
    /// Repeatedly discovers reducible sub-formulas and reduces them.
    /// Reductions are added to the given Proof.
    /// Returns a canonical formula.
    /// </summary>
    public static Formula NandReduction(this Formula startingFormula, Proof proof)
    {
        // if given formula is not a nand then it must be a variable or constant and is not reducible.
        if (!(startingFormula is Formulas.Nand startingNand))
        {
            return startingFormula;
        }

        Formula reducedFormula = startingFormula;

        { // first, reduce both the antecedent and sequent 
            {
                var childProof = new Proof();
                var reducedAntecedent = startingNand.Antecedent.NandReduction(childProof);
                if (!reducedAntecedent.Equals(startingNand.Antecedent))
                {
                    // lift the reductions in the child proof up to the parent proof.  
                    // the child proof will eventually be abandoned.
                    foreach (var r in childProof.Reductions)
                    {
                        var reduced = Formulas.Nand.NewNand(r.ReducedFormula, startingNand.Subsequent);
                        var mapping = SystemExtensions.ConcatAll(
                            new[] { 0 },
                            r.Mapping.Select(i => i + 1),
                            Enumerable.Range(r.StartingFormula.Length + 1, startingNand.Subsequent.Length)
                        );
                        var parentReduction = new Reduction(reducedFormula, reduced, r.RuleDescriptor, mapping);
                        if (!proof.AddReduction(parentReduction)) 
                        { 
                            break; 
                        }
                        reducedFormula = reduced;
                    }
                }
            }
            {
                var childProof = new Proof();
                var reducedSubsequent = startingNand.Subsequent.NandReduction(childProof);
                if (!reducedSubsequent.Equals(startingNand.Subsequent))
                {
                    // lift the reductions in the child proof up to the parent proof.  
                    foreach (var r in childProof.Reductions)
                    {
                        var reduced = Formulas.Nand.NewNand(startingNand.Antecedent, r.ReducedFormula);
                        var mapping = SystemExtensions.ConcatAll(
                            Enumerable.Range(0, startingNand.Antecedent.Length + 1),
                            r.Mapping.Select(i => i + startingNand.Antecedent.Length)
                        );
                        var parentReduction = new Reduction(reducedFormula, reduced, r.RuleDescriptor, mapping);
                        if (!proof.AddReduction(parentReduction))
                        {
                            break;
                        }
                        reducedFormula = reduced;
                    }
                }
            }
        }

        // now render the two canonical parts together
        // note: just calling NandReduction will result in an infinite loop.
        // Repeatedly calling SingleNandReduction avoids that problem.
        {
            while (true)
            {
                var result = reducedFormula.SingleNandReduction(proof);
                if (result.RuleDescriptor == Reduction.RULE_NO_CHANGE || !proof.AddReduction(result))
                {
                    break;
                }
                reducedFormula = result.ReducedFormula;
            }
        }

        return reducedFormula;
    }

    static private ConditionalWeakTable<Formula, Reduction> __reductionResults = new ConditionalWeakTable<Formula, Reduction>();

    public static Reduction SingleNandReduction(this Formula startingFormula, Proof proof)
    {
        Reduction reductionResult = null;
        {   // return any cached value
            if (__reductionResults.TryGetValue(startingFormula, out reductionResult))
            {
                return reductionResult;
            }
        }
        lock (__reductionResults)
        {

            // if given formula is not a nand then it must be a variable or constant and is not reducible.
            if (!(startingFormula is Formulas.Nand startingNand))
            {
                goto ReductionComplete;
            }

            { // if either antecedent or sequent is reducible then done
                {
                    var childProof = new Proof();
                    var result = startingNand.Antecedent.SingleNandReduction(childProof);
                    if (result.RuleDescriptor != Reduction.RULE_NO_CHANGE)
                    {
                        var mapping = SystemExtensions.ConcatAll(
                            new[] { 0 },
                            result.Mapping.Select(i => i + 1),
                            Enumerable.Range(startingNand.Antecedent.Length + 1, startingNand.Subsequent.Length)
                        );
                        reductionResult= new(
                            startingFormula,
                            Formulas.Nand.NewNand(result.ReducedFormula, startingNand.Subsequent),
                            result.RuleDescriptor,
                            mapping);
                        goto ReductionComplete;
                    }
                }
                {
                    var childProof = new Proof();
                    var result = startingNand.Subsequent.SingleNandReduction(childProof);
                    if (result.RuleDescriptor != Reduction.RULE_NO_CHANGE)
                    {
                        var mapping = SystemExtensions.ConcatAll(
                            Enumerable.Range(0, startingNand.Antecedent.Length + 1),
                            result.Mapping.Select(i => i + startingNand.Antecedent.Length + 1)
                        );
                        reductionResult= new(
                            startingFormula,
                            Formulas.Nand.NewNand(startingNand.Antecedent, result.ReducedFormula),
                            result.RuleDescriptor,
                            mapping);
                        goto ReductionComplete;
                    }
                }
            }

            // rules.
            // These are production rules that were discovered by the 'nand-rule-generation-3' script that 
            // are not reducible by the 'wildcard analysis' algorithm, see below.
            // There are three kinds of rules...
            //  - constant elimination
            //  - ordering rules
            //  - wildcard analysis.

            // constant elimination
            if (startingNand.Antecedent == Constant.TRUE)
            {
                if (startingNand.Subsequent is Constant constantConsequent)
                {
                    // |TT => F, and |TF => T
                    var descriptor = constantConsequent.Equals(Constant.TRUE) ? "|TT => F" : "|TF => T";
                    var reducedFormula = constantConsequent.Equals(Constant.TRUE) ? Constant.FALSE : Constant.TRUE;
                    var mapping = new int[] { 0 };
                    reductionResult = new(startingFormula, reducedFormula, descriptor, mapping);
                    goto ReductionComplete;
                }
                if (startingNand.Subsequent is Formulas.Nand nandConsequent)
                {
                    if (nandConsequent.Antecedent.Equals(Constant.TRUE))
                    {
                        // |T|T.1 => .1
                        var reducedFormula = nandConsequent.Subsequent;
                        var mapping = Enumerable.Range(4, nandConsequent.Subsequent.Length);
                        reductionResult = new Reduction(startingFormula, reducedFormula, "|T|T.1 => .1", mapping);
                        goto ReductionComplete;
                    }
                }
                // not true
                //// |T.1, where .1 is canonical, is also canonical.
                //return ReductionResult.NoChange(startingFormula);
            }
            if (startingNand.Antecedent == Constant.FALSE)
            {
                // |F.1 => T
                var mapping = new int[] { 0 };
                reductionResult = new Reduction(startingFormula, Constant.TRUE, "|F.1 => T", mapping);
                goto ReductionComplete;
            }
            if (startingNand.Subsequent.Equals(Constant.TRUE))
            {
                // |.1T => |T.1
                var reducedFormula = Formulas.Nand.NewNand(Constant.TRUE, startingNand.Antecedent);
                var mapping = new int[] { 0, startingNand.Antecedent.Length + 1 }.AsEnumerable()
                    .Concat(Enumerable.Range(1, startingNand.Antecedent.Length));
                reductionResult = new Reduction(startingFormula, reducedFormula, "|.1T => |T.1", mapping.ToList());
                goto ReductionComplete;
            }
            if (startingNand.Subsequent == Constant.FALSE)
            {
                // |.1F => T
                var mapping = new int[] { 0 };
                reductionResult = new Reduction(startingFormula, Constant.TRUE, "|.1F => T", mapping);
                goto ReductionComplete;
            }


            // nand is commutative, the order of operatives doesnt matter.
            //  but, when reducing, the 'smaller' formula must go first.
            // An ordering rule, because the length doesn't change.
            // |.2.1 => |.1.2 
            {
                if (startingNand.Subsequent.CompareTo(startingNand.Antecedent) < 0)
                {
                    var reducedFormula = Formulas.Nand.NewNand(startingNand.Subsequent, startingNand.Antecedent);
                    var mapping = Enumerable.Repeat(-1, 1)
                        .Concat(Enumerable.Range(startingNand.Antecedent.Length + 1, startingNand.Subsequent.Length))
                        .Concat(Enumerable.Range(1, startingNand.Antecedent.Length));
                    reductionResult = new Reduction(startingFormula, reducedFormula, "|.2.1 => |.1.2", mapping);
                    goto ReductionComplete;
                }
            }

            // |.2|T|.1.3 => |.1|T|.2.3.  
            // An ordering rule, because the length doesn't change.
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
                    if (reducedFormula.CompareTo(startingFormula) < 0)
                    {
                        var mapping = Enumerable.Repeat(0, 1) // |
                            .Concat(Enumerable.Range(startingNand.Antecedent.Length + 4, nandSubNand.Antecedent.Length)) // .1
                            .Concat(Enumerable.Range(startingNand.Antecedent.Length + 1, 3)) // |T|
                            .Concat(Enumerable.Range(1, startingNand.Antecedent.Length)) //.2
                            .Concat(Enumerable.Range(startingNand.Antecedent.Length + nandSubNand.Antecedent.Length + 4, nandSubNand.Subsequent.Length)); //.3
                        reductionResult = new Reduction(startingFormula, reducedFormula, "|.2|T|.1.3 => |.1|T|.2.3", mapping);
                        goto ReductionComplete;
                    }
                }
            }

            // Apply the rules ||ab|ac -> |T|a||Tb|Tc and |a||Tb|Tc -> |T||ab|ac 
            // Note: |T||ab|ac -> |T|T|a||Tb|Tc -> |a||Tb|Tc
            // and |T|a||Tb|Tc -> |T|T||ab|ac  -> ||ab|ac
            //
            // |T|a||Tb|Tc -> ||ab|ac 
            {
                if (startingNand.Subsequent is Formulas.Nand nandSub
                    && nandSub.Subsequent is Formulas.Nand nandSubSub
                    && nandSubSub.Antecedent is Formulas.Nand nandSubSubAnt
                    && nandSubSub.Subsequent is Formulas.Nand nandSubSubSub
                    && startingNand.Antecedent.Equals(nandSubSubAnt.Antecedent)
                    && startingNand.Antecedent.Equals(nandSubSubSub.Antecedent)
                    && startingNand.Antecedent.Equals(Constant.TRUE))
                {
                    var reductionTemplate =
                            Formulas.Nand.NewNand(
                                Formulas.Nand.NewNand(
                                    nandSub.Antecedent,
                                    nandSubSubAnt.Subsequent),
                                Formulas.Nand.NewNand(
                                    nandSub.Antecedent,
                                    nandSubSubSub.Subsequent));

                    var childProof = new Proof(proof); 
                    {
                        var mapping = SystemExtensions.ConcatAll(
                            Enumerable.Repeat(-1, nandSub.Antecedent.Length + 2),                                // ||a
                            Enumerable.Range(nandSub.Antecedent.Length + 6, nandSubSubAnt.Subsequent.Length),    // b
                            Enumerable.Repeat(-1, nandSub.Antecedent.Length + 1),                                // |a
                            Enumerable.Range(nandSub.Antecedent.Length + nandSubSubAnt.Subsequent.Length + 8, nandSubSubSub.Subsequent.Length) // c
                        );
                        var firstReduction = new Reduction(startingFormula, reductionTemplate, "|T|a||Tb|Tc -> ||ab|ac", mapping);
                        if (childProof.AddReduction(firstReduction))
                        {
                            var reducedFormula = reductionTemplate.NandReduction(childProof);
                            if (reducedFormula.CompareTo(startingFormula) < 0)
                            {
                                reductionResult = new(startingFormula, reducedFormula, "order: |T|a||Tb|Tc -> ||ab|ac", childProof.ReductionMapping);
                                goto ReductionComplete;
                            }
                        }
                    }
                }
            }



            // |.1||T.2|T.3 => |T||.1.2|.1.3
            // Only valid for length(.1) == 1
            // An ordering rule, because the length doesn't change.
            // This so-called reduction doesn't always produce shorter formulas.
            // Consider substituting |.3.4 for .1.
            // Reducible by rewriting using rule |a||Tb|Tc -> |T||ab|ac.
            //  (essentially the same rule as ||ab|ac -> |T|a||Tb|Tc)
            // => |T||.1.2|.1.3 directly
            //
            {
                if (startingNand.Subsequent is Formulas.Nand nandSub
                    && nandSub.Antecedent is Formulas.Nand nandSubAnt
                    && nandSub.Subsequent is Formulas.Nand nandSubSub
                    && nandSubAnt.Antecedent.Equals(Constant.TRUE)
                    && nandSubSub.Antecedent.Equals(Constant.TRUE))
                {
                    var reducedFormula = Formulas.Nand.NewNand(
                        Constant.TRUE,
                        Formulas.Nand.NewNand(
                            Formulas.Nand.NewNand(
                                startingNand.Antecedent,
                                nandSubAnt.Subsequent),
                            Formulas.Nand.NewNand(
                                startingNand.Antecedent,
                                nandSubSub.Subsequent)));

                    if (reducedFormula.CompareTo(startingFormula) < 0)
                    {
                        // |.1||T.2|T.3 => |T||.1.2|.1.3
                        var mapping = Enumerable.Repeat(0, 1) // |
                            .Concat(Enumerable.Repeat(-1, 3)) // T|| note: ||.1.2|.1.3, |.1.2, and |.1.3 dont exist in the starting formula
                            .Concat(Enumerable.Range(1, startingNand.Antecedent.Length)) // .1
                            .Concat(Enumerable.Range(startingNand.Antecedent.Length + 4, nandSubAnt.Subsequent.Length)) //.2
                            .Concat(Enumerable.Range(-1, 1)) // | note:|.1.3 doesnt exists in starting formula
                            .Concat(Enumerable.Range(1, startingNand.Antecedent.Length)) // .1
                            .Concat(Enumerable.Range(startingNand.Antecedent.Length + nandSubAnt.Subsequent.Length + 6, nandSubSub.Subsequent.Length)); // .3

                        reductionResult = new Reduction(startingFormula, reducedFormula, "|.1||T.2|T.3 => |T||.1.2|.1.3", mapping);
                        goto ReductionComplete;
                    }
                }
            }


            // ||.1.2|.1|T.3 => |T|.1|.3|T.2
            // Only valid for length(.1) == 1
            // An ordering rule, because the length doesn't change (when length(.1) == 1).
            // Also a shortening rule when 1 < length(.1)
            // reducible by rewriting using rule ||ab|ac -> |T|a||Tb|Tc
            // => |T|.1||T.2|T|T.3 
            // => |T|.1||T.2.3 
            // => |T|.1|.3|T.2
            {
                if (startingNand.Antecedent is Formulas.Nand nandAnt // inherited
                    && startingNand.Subsequent is Formulas.Nand nandSub // inherited
                    && nandSub.Subsequent is Formulas.Nand nandSubSub // inherited
                    && nandAnt.Antecedent.Equals(nandSub.Antecedent)
                    && nandSubSub.Antecedent.Equals(Constant.TRUE)) // inherited
                {
                    var reducedFormula = Formulas.Nand.NewNand(
                        Constant.TRUE,
                        Formulas.Nand.NewNand(
                            nandAnt.Antecedent,
                            Formulas.Nand.NewNand(
                                nandSubSub.Subsequent,
                                Formulas.Nand.NewNand(
                                    Constant.TRUE,
                                    nandAnt.Subsequent))));

                    if (reducedFormula.CompareTo(startingFormula) < 0)
                    {
                        var mappingTwo = Enumerable.Range(nandAnt.Antecedent.Length + 2, nandAnt.Subsequent.Length);
                        var mappingTrue = Enumerable.Range(nandAnt.Length + nandSubSub.Subsequent.Length + 3, 1);
                        var mappingThree = Enumerable.Range(nandAnt.Length + nandSub.Antecedent.Length + 4, nandSubSub.Subsequent.Length);
                        var mapping = SystemExtensions.ConcatAll(
                            Enumerable.Repeat(0, 1),    // |
                            Enumerable.Repeat(-1, 1),   // T
                            Enumerable.Repeat(-1, nandAnt.Antecedent.Length + 2),   // |.1|  
                            mappingThree,               // .3
                            Enumerable.Repeat(-1, 1),   // | note: |T.2 does not exist in starting formula
                            mappingTrue,                // T
                            mappingTwo                  // .2
                        );

                        reductionResult = new(startingFormula, reducedFormula, "||.1.2|.1|T.3 => |T|.1|.3|T.2", mapping);
                        goto ReductionComplete;
                    }
                }
            }

            // ||.1.2|.2|T.3 => |T|.2|.3|T.1
            // An ordering rule, because the length doesn't change (when length(.1) == 1).
            // Also a shortening rule when 1 < length(.1)
            // reducible by rewriting using rule ||ab|ac -> |T|a||Tb|Tc
            //  => |T|.2||T.1|T|T.3
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

                    if (reducedFormula.CompareTo(startingFormula) < 0)
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
                        );

                        reductionResult = new(startingFormula, reducedFormula, "||.1.2|.2|T.3 => |T|.2|.3|T.1", mapping);
                        goto ReductionComplete;
                    }
                }
            }


            // ||.1.2|.1|.3|T.2 => |T|.1|.3|T.2
            // consolidate common subformula in nand arguments, using negation: .1. in this case
            // reducible by rewriting using rule ||ab|ac -> |T|a||Tb|Tc
            //  => |T|.1||T.2|T|.3|T.2
            //  => |T|.1||T.2|T|.3|T.F
            //  => |T|.1||T.2|T|.3T
            //  => |T|.1||T.2.3
            //  => |T|.1|.3|T.2
            {
                if (startingNand.Antecedent is Formulas.Nand nandAnt // inherited
                    && startingNand.Subsequent is Formulas.Nand nandSub // inherited
                    && nandSub.Subsequent is Formulas.Nand nandSubSub // inherited
                    && nandSubSub.Subsequent is Formulas.Nand nandSubSubSub // inherited
                    && nandAnt.Antecedent.Equals(nandSub.Antecedent)
                    && nandAnt.Subsequent.Equals(nandSubSubSub.Subsequent) // inherited
                    && nandSubSubSub.Antecedent.Equals(Constant.TRUE))
                {
                    var reducedFormula = Formulas.Nand.NewNand(
                        Constant.TRUE,
                        Formulas.Nand.NewNand(
                            nandAnt.Antecedent,
                            Formulas.Nand.NewNand(
                                nandSubSub.Antecedent,
                                Formulas.Nand.NewNand(
                                    Constant.TRUE,
                                    nandAnt.Subsequent))));

                    if (reducedFormula.CompareTo(startingFormula) < 0)
                    {
                        var mappingTwo = Enumerable.Range(nandAnt.Antecedent.Length + 2, nandAnt.Subsequent.Length);
                        var mappingThree = Enumerable.Range(nandAnt.Length + nandSub.Antecedent.Length + 4, nandSubSub.Antecedent.Length);
                        var mappingTrue = Enumerable.Range(nandAnt.Length + nandSub.Antecedent.Length + nandSubSub.Antecedent.Length + 5, 1);
                        var mapping = SystemExtensions.ConcatAll(
                            Enumerable.Repeat(0, 1),    // |
                            mappingTrue,                // T
                            Enumerable.Repeat(-1, 3),   // |.1|  
                            mappingThree,               // .3
                            Enumerable.Repeat(-1, 1),   // | note: |T.1 does not exist in starting formula
                            mappingTrue,                // T
                            mappingTwo                  // .2
                        );

                        reductionResult = new(startingFormula, reducedFormula, "||.1.2|.1|.3|T.2 => |T|.1|.3|T.2", mapping);
                        goto ReductionComplete;
                    }
                }
            }

            // ||.1|.2.3|.2|T.1 => ||.1|T.2|.2|.1.3
            // Reducible by rewriting using rule ||a|bc|b|Ta -> ||a|Tb|b|ac.
            // => ||.1|.2.3|.2|T.1 => ||.1|T.2|.2|.1.3 directly
            // NOT reducible via wildcard analysis (verified)
            // Basically the .3 and the T trade places
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
                    if (reducedFormula.CompareTo(startingFormula) < 0)
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
                        );

                        reductionResult = new(startingFormula, reducedFormula, "||.1|.2.3|.2|T.1 => ||.1|T.2|.2|.1.3", mapping);
                        goto ReductionComplete;
                    }
                }
            }

            // ||.1|.2.3|.3|T.1 => ||.1|T.3|.3|.1.2
            // Only valid for length(.1) == 1
            // An ordering rule, because the length doesn't change.
            // Reducible by rewriting using rule ||a|bc|c|Ta -> ||a|Tc|c|ab.
            //  => ||.1|T.3|.3|.1.2 directly
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

                    if (reducedFormula.CompareTo(startingFormula) < 0)
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
                        );

                        reductionResult = new(startingFormula, reducedFormula, "||.1|.2.3|.3|T.1 => ||.1|T.3|.3|.1.2", reductionMapping);
                        goto ReductionComplete;
                    }
                }
            }


            // |T||T.1||T.2|T.3 => ||.2|T.1|.3|T.1
            // Reducible by rewriting using rule |T|a||Tb|Tc -> ||ab||ac 
            // => ||.2|T.1||.3|T.1 directly
            // Subsumed by rule |T|a|bc -> ||a|Tb|a|Tc -> *...
            //  => |||T.1|T|T.2||T.1|T|T.3 where a= |T.1 ,b= |T.2, c= |T.3
            //  => |||T.1|T|T.2||T.1.3
            //  => |||T.1|T|T.2|.3|T.1
            //  => |||T.1.2|.3|T.1
            //  => ||.2|T.1|.3|T.1

            {
                if (startingNand.Subsequent is Formulas.Nand nandSub
                    && nandSub.Antecedent is Formulas.Nand nandSubAnt
                    && nandSub.Subsequent is Formulas.Nand nandSubSub
                    && nandSubSub.Antecedent is Formulas.Nand nandSubSubAnt
                    && nandSubSub.Subsequent is Formulas.Nand nandSubSubSub
                    && startingNand.Antecedent.Equals(Constant.TRUE)
                    && nandSubAnt.Antecedent.Equals(Constant.TRUE)
                    && nandSubSubAnt.Antecedent.Equals(Constant.TRUE)
                    && nandSubSubSub.Antecedent.Equals(Constant.TRUE))
                {
                    Formula reducedNand = Formulas.Nand.NewNand(
                        Formulas.Nand.NewNand(
                            nandSubSubAnt.Subsequent,
                            Formulas.Nand.NewNand(
                                Constant.TRUE,
                                nandSubAnt.Subsequent)),
                        Formulas.Nand.NewNand(
                            nandSubSubSub.Subsequent,
                            Formulas.Nand.NewNand(
                                Constant.TRUE,
                                nandSubAnt.Subsequent)));
                    Formula reducedFormula = reducedNand;


                    /////////////////////////////////

                    var mapping2 = Enumerable.Range(nandSubAnt.Length + 6, nandSubSubAnt.Subsequent.Length);
                    var mapping3 = Enumerable.Range(nandSub.Antecedent.Length + nandSubSubAnt.Subsequent.Length + 6, nandSubSubSub.Subsequent.Length);
                    var reductionMapping = SystemExtensions.ConcatAll(
                        Enumerable.Repeat(0, 2),                                    // ||
                        mapping2,                                                   // .2
                        Enumerable.Repeat(-1, nandSubAnt.Subsequent.Length + 3),    // |T.1|
                        mapping3,                                                   // .3
                        Enumerable.Repeat(-1, nandSubAnt.Subsequent.Length + 2)     // |T.1
                    );
                    var firstReduction = new Reduction(startingFormula, reducedFormula, "|T||T.1||T.2|T.3 => ||.2|T.1|.3|T.1", reductionMapping);

                    var isReduced = reducedFormula.CompareTo(startingFormula) < 0;
                    if (isReduced)
                    {
                        reductionResult = firstReduction;
                        goto ReductionComplete;
                    }

                    var childProof = new Proof(proof);
                    if (childProof.AddReduction(firstReduction))
                    {
                        var reducedFormula2 = reducedFormula.NandReduction(childProof);
                        if (reducedFormula2.CompareTo(startingFormula) < 0)
                        {
                            reductionResult = new (startingFormula, reducedFormula2, "|T||T.1||T.2|T.3 => ||.2|T.1|.3|T.1 => *", childProof.ReductionMapping);
                            goto ReductionComplete;
                        }
                    }
                }
            }


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
            //              any instances of $a in f.S may be StartingFormula by $c ? F:T, the opposite of C.
            //  Similarly for all $s in S* except T and F...
            //      For all $c in [T,F]
            //          If
            //              replacing all instances of $s in f.S with $c causes f to reduce to 
            //              an 'independent' formula that does not contain any instances of $s
            //          then
            //              any instances of $s in f.A may be StartingFormula by $c ? F:T, the opposite of C.
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
            //          then any instances of $a in S may be StartingFormula by $c ? F:T, the opposite of C.
            //  For any $s in S*...
            //      For all $c in [T,F]
            //          If replacing all instances of $s in S with C causes S to reduce to F 
            //          then any instances of $s in A may be StartingFormula by $c ? F:T, the opposite of C.
            // ```
            IEnumerable<Formula> commonTerms =
                startingNand.Antecedent.AsFlatTerm().Distinct().Where(f => !(f is Constant))
                    .Intersect(startingNand.Subsequent.AsFlatTerm().Distinct().Where(f => !(f is Constant)));

            // skip common terms that contain any other common terms as a subterm.
            var independentTerms = commonTerms.Where(f => !commonTerms.Where(t => t.Length < f.Length && 0 <= f.PositionOf(t)).Any());

            foreach (var subterm in independentTerms)
            {
                {
                    var replacedAntecedent = startingNand.Antecedent.ReplaceAll(subterm, Constant.TRUE).NandReduction();
                    var replaced = Formulas.Nand.NewNand(replacedAntecedent, startingNand.Subsequent);
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
                            var nandReplaced2 = replaced2 as Formulas.Nand;
                            Debug.Assert(nandReplaced2.Antecedent.Equals(replaced.Antecedent));

                            var reducedFormula = Formulas.Nand.NewNand(startingNand.Antecedent, nandReplaced2.Subsequent);
                            var reductionMapping = SystemExtensions.ConcatAll(
                                Enumerable.Range(0, targetFinder.ReductionPosition),  // everything left of the replacement target
                                Enumerable.Range(-1, 1), // T
                                                         // everything right of the replacement target
                                Enumerable.Range(targetFinder.ReductionPosition + subterm.Length, reducedFormula.Length - targetFinder.ReductionPosition - 1));

                            reductionResult= new Reduction(
                                startingFormula,
                                reducedFormula,
                                $"wildcard in subsequent: {subterm}->F",
                                reductionMapping);
                            goto ReductionComplete;
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
                                    Enumerable.Range(targetFinder.ReductionPosition + subterm.Length, reducedFormula.Length - targetFinder.ReductionPosition - 1));

                                reductionResult = new Reduction(
                                    startingFormula,
                                    reducedFormula,
                                    $"wildcard in subsequent: {subterm}->T",
                                    reductionMapping);
                                goto ReductionComplete;
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
                                    Enumerable.Range(targetFinder.ReductionPosition + subterm.Length, reducedFormula.Length - targetFinder.ReductionPosition - 1));

                                reductionResult = new Reduction(
                                    startingFormula,
                                    reducedFormula,
                                    $"wildcard in antecedent: {subterm}->F",
                                    reductionMapping);
                                goto ReductionComplete;
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
                                    Enumerable.Range(targetFinder.ReductionPosition + subterm.Length, reducedFormula.Length - targetFinder.ReductionPosition - 1));

                                reductionResult = new Reduction(
                                    startingFormula,
                                    reducedFormula,
                                    $"wildcard in antecedent: {subterm}->T",
                                    reductionMapping);
                                goto ReductionComplete;
                            }
                        }
                    }
                }
            }


            //
            // |T|a|bc -> ||a|Tb|a|Tc ->*
            {
                if (startingNand.Subsequent is Formulas.Nand nandSub
                    && nandSub.Subsequent is Formulas.Nand nandSubSub
                    && startingNand.Antecedent.Equals(Constant.TRUE))
                {
                    var reductionTemplate = Formulas.Nand.NewNand(
                        Formulas.Nand.NewNand(
                            nandSub.Antecedent,
                            Formulas.Nand.NewNand(
                                Constant.TRUE,
                                nandSubSub.Antecedent)),
                        Formulas.Nand.NewNand(
                            nandSub.Antecedent,
                            Formulas.Nand.NewNand(
                                Constant.TRUE,
                                nandSubSub.Subsequent)));

                    var childProof = new Proof(proof);

                    var mapping = Enumerable.Repeat(-1, reductionTemplate.Length); // WARNING: map is bogus, me lazy && C no need to figure out correct mapping
                    var firstReduction = new Reduction(startingFormula, reductionTemplate, "|T|a|bc -> ||a|Tb|a|Tc", mapping);
                    if (childProof.AddReduction(firstReduction))
                    {
                        var reducedFormula = reductionTemplate.NandReduction(childProof);
                        if (reducedFormula.CompareTo(startingFormula) < 0)
                        {
                            reductionResult = new (startingFormula, reducedFormula, "|T|a|bc -> ||a|Tb|a|Tc ->*", childProof.ReductionMapping);
                            goto ReductionComplete;
                        }
                    }
                }
            }

        }

        ReductionComplete:

        // the formula was not reduced
        if (reductionResult == null)
        {
            reductionResult = Reduction.NoChange(startingFormula);
        }

        return reductionResult;
    }
    public static Formula NandReduction(this Formula startingFormula)
    {
        Proof proof = new Proof();
        return NandReduction(startingFormula, proof);
    }
}