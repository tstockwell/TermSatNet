using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using TermSAT.Common;
using TermSAT.Formulas;

namespace TermSAT.NandReduction;

public static class NandReducerDistributiveRules
{


    /// <summary>
    // The distributive rules |a|bc -> |T||a|Tb|a|Tc -> * and ||ab|ac -> |T|a||Tb|Tc -> *
    //
    // One day I just came upon the rules '|a|bc -> |T||a|Tb|a|Tc -> *' and '|T|a|bc -> ||a|Tb|a|Tc -> *' .  
    // And they're exceptionally powerful. 
    // And I have not the first clue why exactly.  
    // One thing about these rules is that the right side is more complex than the left side, 
    // so they're like an anti-reduction, or expansion rule.  
    // But they're capable of reducing a large, infinite, slice of the 'ordering' reduction rules that NandSAT requires.
    //
    // For instance, the rules will reduce |T||T.1||T.2|T.3 to ||.2|T.1|.3|T.1 and reduce |.1||T.2|T.3 => |T||.1.2|.1.3.
    // Note that these rules transform a formula with a single instance of .1 to a *simpler* formula with two instances of .1.  
    // In these rules the starting formulas are non-canonical formulas that are not reducible via wildcard analysis.   
    //
    // ||ab|ac -> |T|a||Tb|Tc -> * is a similar rule.
    // This rule can, for instance, reduce ||.1.2|.1|T.3 to |T|.1|.3|T.2 (which also contains no wildcards);
    // I see this rules as a compliment to |a|bc -> |T||a|Tb|a|Tc -> *.  
    // The |a|bc -> |T||a|Tb|a|Tc rule 'splits' a variable instance by repeating it.
    // The ||ab|ac -> |T|a||Tb|Tc -> * rule 'joins' two variables instances.
    //
    // I suspect that these two rules illustrate some sort of distributive property of nandSAT formulas.
    //
    // PS...
    //  It's also necessary to implement rules that reduce 'negated' formulas.
    //  That is, formulas that start with "|T".  
    //  So, we need the rule |T|a|bc -> ||a|Tb|a|Tc in addition to |a|bc -> |T||a|Tb|a|Tc.
    //  And |T||ab|ac -> |a||Tb|Tc in addition to ||ab|ac -> |T|a||Tb|Tc.
    //
    // PS...
    //  It's also necessary to implement rules that reduce 'unordered' formulas.
    //  That is, in addition to ||ab|ac -> |T|a||Tb|Tc we need...
    //      ||ab|ca -> |T|a||Tb|Tc, and...  
    //      ||ba|ac -> |T|a||Tb|Tc 
    //      ||ba|ca -> |T|a||Tb|Tc 
    //  And in addition to |a|bc -> |T||a|Tb|a|Tc we need ||bca -> |T||a|Tb|a|Tc.  
    //
    // PS...
    // NandSAT formulas are NOT associative, just like in 'normal' propositional calculus.  
    // However, unlike 'normal' propositional calculus, NandSAT formulas are NOT commutative.  
    // Because NandSAT formulas are ordered, the order of arguments matters.  
    /// 
    /// </summary>
    public static Reduction ReduceDistributiveFormulas(this Nand startingNand, Proof proof)
    {
        List<Reduction> incompleteProofs = new List<Reduction>();

        // |a|bc -> |T||a|Tb|a|Tc -> * 
        {
            if (startingNand.Subsequent is Nand nandSub
                && startingNand.Antecedent is Formula nandAnt)
            {
                var reductionTemplate = Nand.NewNand(
                    Constant.TRUE,
                    Nand.NewNand(
                        Nand.NewNand(
                            nandAnt,
                            Nand.NewNand(
                                Constant.TRUE,
                                nandSub.Antecedent)),
                        Nand.NewNand(
                            nandAnt,
                            Nand.NewNand(
                                Constant.TRUE,
                                nandSub.Subsequent))));

                var childProof = new Proof(proof); 

                var mapping = SystemExtensions.ConcatAll(
                    Enumerable.Repeat(-1, nandAnt.Length + 6),                           // |T||a|T
                    Enumerable.Range(nandAnt.Length + 2, nandSub.Antecedent.Length),     // b                                                                                         
                    Enumerable.Repeat(-1, nandAnt.Length + 3),                           // |a|T
                    Enumerable.Range(nandAnt.Length + nandSub.Antecedent.Length + 2, nandSub.Subsequent.Length)      // c
                ).ToImmutableList();

                var firstReduction = new Reduction(startingNand, reductionTemplate, "|a|bc -> |T||a|Tb|a|Tc", mapping, incompleteProofs.ToImmutableList());
                if (childProof.AddReduction(firstReduction))
                {
                    var reducedFormula = reductionTemplate.NandReduction(childProof);
                    if (reducedFormula.CompareTo(startingNand) < 0)
                    {
                        var result = new Reduction(startingNand, reducedFormula, "|a|bc -> |T||a|Tb|a|Tc -> *", childProof.ReductionMapping, incompleteProofs.ToImmutableList());
                        return result;
                    }
                }
                else
                {
                    incompleteProofs.Add(firstReduction);
                }
            }
        }

#if DEBUG
        if (startingNand.Equals("|T||.1|T.2|.2|T.1"))
        {
            Debugger.Break();
        }
#endif

        // |T|a|bc -> ||a|Tb|a|Tc -> * 
        {
            if (startingNand.Subsequent is Formulas.Nand nandSub
                && nandSub.Subsequent is Formulas.Nand nandSubSub
                && startingNand.Antecedent.Equals(Constant.TRUE))
            {
                var reductionTemplate =
                    Nand.NewNand(
                        Nand.NewNand(
                            nandSub.Antecedent,
                            Nand.NewNand(
                                Constant.TRUE,
                                nandSubSub.Antecedent)),
                        Nand.NewNand(
                            nandSub.Antecedent,
                            Nand.NewNand(
                                Constant.TRUE,
                                nandSubSub.Subsequent)));

                var childProof = new Proof(proof);

                var mapping = SystemExtensions.ConcatAll(
                    Enumerable.Repeat(-1, nandSub.Antecedent.Length + 4),                // ||a|T
                    Enumerable.Range(nandSub.Antecedent.Length + 4, nandSubSub.Antecedent.Length),     // b                                                                                         
                    Enumerable.Repeat(-1, nandSub.Antecedent.Length + 3),                           // |a|T
                    Enumerable.Range(nandSub.Antecedent.Length + nandSubSub.Antecedent.Length + 2, nandSub.Subsequent.Length)      // c
                ).ToImmutableList();

                var firstReduction = new Reduction(startingNand, reductionTemplate, "|T|a|bc -> ||a|Tb|a|Tc", mapping, incompleteProofs.ToImmutableList());
                if (childProof.AddReduction(firstReduction))
                {
                    var reducedFormula = reductionTemplate.NandReduction(childProof);
                    if (reducedFormula.CompareTo(startingNand) < 0)
                    {
                        var result = new Reduction(startingNand, reducedFormula, "|T|a|bc -> ||a|Tb|a|Tc -> *", childProof.ReductionMapping, incompleteProofs.ToImmutableList());
                        return result;
                    }
                }
                else
                {
                    incompleteProofs.Add(firstReduction);
                }
            }
        }

        // ||ab|ac -> |T|a||Tb|Tc -> *
        {
            if (startingNand.Subsequent is Formulas.Nand nandSub
                && startingNand.Antecedent is Formulas.Nand nandAnt
                && nandAnt.Antecedent.Equals(nandSub.Antecedent))
            {
                var reductionTemplate = Nand.NewNand(
                    Constant.TRUE,
                    Nand.NewNand(
                        nandAnt.Antecedent,
                        Nand.NewNand(
                            Nand.NewNand(
                                Constant.TRUE,
                                nandAnt.Subsequent),
                            Nand.NewNand(
                                Constant.TRUE,
                                nandSub.Subsequent))));

                var childProof = new Proof(proof);

                var mapping = SystemExtensions.ConcatAll(
                    Enumerable.Repeat(-1, nandAnt.Antecedent.Length + 6),                           // |T|a||T
                    Enumerable.Range(nandAnt.Antecedent.Length + 2, nandAnt.Subsequent.Length),     // b
                    Enumerable.Repeat(-1, 2),                                                       // |T
                    Enumerable.Range(startingNand.Antecedent.Length + nandAnt.Antecedent.Length + 2, nandSub.Subsequent.Length)   // c
                ).ToImmutableList();

                var firstReduction = new Reduction(startingNand, reductionTemplate, "||ab|ac -> |T|a||Tb|Tc", mapping, incompleteProofs.ToImmutableList());
                if (childProof.AddReduction(firstReduction))
                {
                    var reducedFormula = reductionTemplate.NandReduction(childProof);
                    if (reducedFormula.CompareTo(startingNand) < 0)
                    {
                        var result = new Reduction(startingNand, reducedFormula, "||ab|ac -> |T|a||Tb|Tc -> *", childProof.ReductionMapping);
                        return result;
                    }
                }
                else
                {
                    incompleteProofs.Add(firstReduction);
                }
            }
        }

        // ||ab|ca -> |T|a||Tb|Tc -> *
        {
            if (startingNand.Subsequent is Formulas.Nand nandSub
                && startingNand.Antecedent is Formulas.Nand nandAnt
                && nandAnt.Antecedent.Equals(nandSub.Subsequent))
            {
                var reductionTemplate = Nand.NewNand(
                    Constant.TRUE,
                    Nand.NewNand(
                        nandAnt.Antecedent,
                        Nand.NewNand(
                            Nand.NewNand(
                                Constant.TRUE,
                                nandAnt.Subsequent),
                            Nand.NewNand(
                                Constant.TRUE,
                                nandSub.Antecedent))));

                var childProof = new Proof(proof);

                var mapping = SystemExtensions.ConcatAll(
                    Enumerable.Repeat(-1, nandAnt.Antecedent.Length + 6),                           // |T|a||T
                    Enumerable.Range(nandAnt.Antecedent.Length + 2, nandAnt.Subsequent.Length),     // b
                    Enumerable.Repeat(-1, 2),                                                       // |T
                    Enumerable.Range(startingNand.Antecedent.Length + 2, nandSub.Antecedent.Length)  // c
                ).ToImmutableList();

                var firstReduction = new Reduction(startingNand, reductionTemplate, "||ab|ca -> |T|a||Tb|Tc", mapping, incompleteProofs.ToImmutableList());
                if (childProof.AddReduction(firstReduction))
                {
                    var reducedFormula = reductionTemplate.NandReduction(childProof);
                    if (reducedFormula.CompareTo(startingNand) < 0)
                    {
                        var result = new Reduction(startingNand, reducedFormula, "||ab|ca -> |T|a||Tb|Tc -> *", childProof.ReductionMapping);
                        return result;
                    }
                }
                else
                {
                    incompleteProofs.Add(firstReduction);
                }
            }
        }

        return Reduction.NoChange(startingNand, incompleteProofs.ToImmutableList());
    }
}