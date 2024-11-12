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
    // The distributive rules...
    //  |a|bc -> |T||a|Tb|a|Tc -> * and
    //  ||ab|ac -> |T|a||Tb|Tc -> *
    //  ?? ||bc||ab|ac -> |a||b|Tc|c|Tb -> *

    //
    // One day I just came upon the rules '|a|bc -> |T||a|Tb|a|Tc -> *' and '|T|a|bc -> ||a|Tb|a|Tc -> *' .  
    // And they're exceptionally powerful. 
    // And I have not the first clue why exactly.  
    // One thing about these rules is that the right side is more complex than the left side, 
    // so they're like an anti-reduction, or expansion rule.  
    // But they're capable of reducing a large, infinite, slice of the 'ordering' reduction rules that NandSAT requires.
    // That's not conjecture, NandSAT testing confirms that this rule can replace many reduction rules.
    //
    // For instance, the rule |a|bc -> |T||a|Tb|a|Tc will reduce |T||T.1||T.2|T.3 to ||.2|T.1|.3|T.1
    // and reduce |.1||T.2|T.3 => |T||.1.2|.1.3.
    // Also note that the rule |a|bc -> |T||a|Tb|a|Tc will reduce |T||a|Tb|a|Tc to |a|bc (proof below).
    // Note that these rules transform a formula with a single instance of .1 to a *simpler* formula with two instances of .1.  
    // In these rules the starting formulas are non-canonical formulas that are not reducible via wildcard analysis.   
    //
    // ||ab|ac -> |T|a||Tb|Tc -> * is a similar rule, but opposite.
    // This rule can, for instance, reduce ||.1.2|.1|T.3 to |T|.1|.3|T.2 (which also contains no wildcards);
    // I see this rule as a compliment to |a|bc -> |T||a|Tb|a|Tc -> *.  
    // The |a|bc -> |T||a|Tb|a|Tc rule 'splits' a variable instance by repeating it.
    // The ||ab|ac -> |T|a||Tb|Tc -> * rule 'joins' two variables instances.
    //
    // I suspect that these two rules illustrate some sort of distributive property of nandSAT formulas.
    //
    // PS...
    //  |T||a|Tb|a|Tc cannot be reduced using wildcard analysis,
    //  |a|bc and |T||a|Tb|a|Tc are equivalent formulas, |a|bc is the canonical form of |T||a|Tb|a|Tc.
    //  However, |T||a|Tb|a|Tc *can* be reduced using the rule |a|bc => |T||a|Tb|a|Tc => *
    //  Proof...
    //      |T||a|Tb|a|Tc
    //      => |T|T|||.1|T.2|T.1||.1|T.2|T|T.3, reduce subsequent 1st.  |a|bc -> |T||a|Tb|a|Tc 
    //      => |T|T|||T.1|.1|T.2||.1|T.2|T|T.3, |.2.1 -> |.1.2
    //      => |T|T|||T.1|F|T.2||.1|T.2|T|T.3 , wildcard: .1->F: 
    //      => |T|T|||T.1T||.1|T.2|T|T.3 
    //      => |T|T|.1||.1|T.2|T|T.3
    //      => |T|T|.1||.1|T.2.3
    //      => |T|T|.1|.3|.1|T.2
    //      => |T|T|.1|.3|T|T.2
    //      => |T|T|.1|.3.2}
    //      => |T|T|.1|.2.3}
    //      => |.1|.2.3}
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
        // |a|bc -> -||a-b|a-c -> * 
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

                var firstReduction = new Reduction(startingNand, reductionTemplate, "|a|bc -> |T||a|Tb|a|Tc", mapping, incompleteProofs.ToImmutableList(), childProof);
                if (childProof.AddReduction(firstReduction))
                {
                    var reducedFormula = reductionTemplate.NandReduction(childProof);
                    if (reducedFormula.CompareTo(startingNand) < 0)
                    {
                        var result = new Reduction(startingNand, reducedFormula, "|a|bc -> |T||a|Tb|a|Tc -> *", childProof.Mapping, incompleteProofs.ToImmutableList(), childProof);
                        return result;
                    }
                }
                else
                {
                    incompleteProofs.Add(firstReduction);
                }
            }
        }

//#if DEBUG
//        if (startingNand.Equals("|T||.1|T.2|.2|T.1"))
//        {
//            Debugger.Break();
//        }
//#endif

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
                    Enumerable.Range(nandSub.Antecedent.Length + nandSubSub.Antecedent.Length + 4, nandSubSub.Subsequent.Length)      // c
                ).ToImmutableList();

                var firstReduction = new Reduction(startingNand, reductionTemplate, "|T|a|bc -> ||a|Tb|a|Tc", mapping, incompleteProofs.ToImmutableList(), childProof);
                if (childProof.AddReduction(firstReduction))
                {
                    var reducedFormula = reductionTemplate.NandReduction(childProof);
                    if (reducedFormula.CompareTo(startingNand) < 0)
                    {
                        var result = new Reduction(startingNand, reducedFormula, "|T|a|bc -> ||a|Tb|a|Tc -> *", childProof.Mapping, incompleteProofs.ToImmutableList(), childProof);
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
        // ||ab|ac -> -|a|-b-c -> *
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

                var firstReduction = new Reduction(startingNand, reductionTemplate, "||ab|ac -> |T|a||Tb|Tc", mapping, incompleteProofs.ToImmutableList(), childProof);
                if (childProof.AddReduction(firstReduction))
                {
                    var reducedFormula = reductionTemplate.NandReduction(childProof);
                    if (reducedFormula.CompareTo(startingNand) < 0)
                    {
                        var result = new Reduction(startingNand, reducedFormula, "||ab|ac -> |T|a||Tb|Tc -> *", childProof.Mapping, null, childProof);
                        return result;
                    }
                }
                else
                {
                    incompleteProofs.Add(firstReduction);
                }
            }
        }

        // |c||a|Tb|b|aT => ||ab||ac|bc


        // expansion rule: ||bc||ab|ac => |a||b|Tc|c|Tb 

        // |.1||.2|T.3|.3|T.2 => ||.2.3||.1.2|.1.3
        // expansion rule: ||bc||ab|ac => |a||b|Tc|c|Tb 
        // Unlike other distributive rules, the expansion rule doesnt help reduce its base formula
        // For now, implementing like a plain production rule
        // |.1||.2|T.3|.3|T.2 => ||.2.3||.1.2|.1.3
        // NOTE:
        // |.3||.1|T.2|.2|T.1 => ||.1.2||.1.3|.2.3 is another equivalent rule, so...
        // todo: explore the possibility of replacing 'expansion rules' with un-ordered distributive reduction rules
        // |T||.1|T.2|.3|T.1 => ||.1.2||T.1|T.3


        {
            if (startingNand.Subsequent is Nand nandSub
                && nandSub.Subsequent is Nand nandSubSub
                && nandSub.Antecedent is Nand nandSubAnt
                && nandSubAnt.Subsequent is Nand nandSubAntSub
                && nandSubSub.Subsequent is Nand nandSubSubSub
                && nandSubAntSub.Subsequent.Equals(nandSubSub.Antecedent)
                && nandSubAntSub.Antecedent.Equals(Constant.TRUE)
                && nandSubAnt.Antecedent.Equals(nandSubSubSub.Subsequent)
                && nandSubSubSub.Antecedent.Equals(Constant.TRUE))
            {
                var childProof = new Proof(proof);
                var reductionTemplate = Nand.NewNand(
                    Nand.NewNand(nandSubSubSub.Subsequent, nandSubSub.Antecedent),
                    Nand.NewNand(
                        Nand.NewNand(startingNand.Antecedent, nandSubSubSub.Subsequent),
                        Nand.NewNand(startingNand.Antecedent, nandSubAntSub.Subsequent)));

                var mapping =
                    Enumerable.Repeat(-1, 2) // ||
                    .Concat(Enumerable.Range(startingNand.Antecedent.Length + 3, nandSubAnt.Antecedent.Length)) // .2
                    .Concat(Enumerable.Range(startingNand.Antecedent.Length + nandSub.Antecedent.Length + 3, nandSubAntSub.Subsequent.Length)) // .3
                    .Concat(Enumerable.Repeat(-1, 2)) // ||
                    .Concat(Enumerable.Range(1, startingNand.Antecedent.Length)) // .1
                    .Concat(Enumerable.Range(startingNand.Antecedent.Length + nandSub.Antecedent.Length + nandSubSub.Antecedent.Length + 5, nandSubSubSub.Subsequent.Length)) // .2
                    .Concat(Enumerable.Repeat(-1, 1)) // |
                    .Concat(Enumerable.Range(1, startingNand.Antecedent.Length)) // .1
                    .Concat(Enumerable.Range(startingNand.Antecedent.Length + nandSubAnt.Antecedent.Length + nandSubAntSub.Subsequent.Length + 6, nandSub.Antecedent.Length)) // .3
                    .ToImmutableList();

                var firstReduction = new Reduction(startingNand, reductionTemplate, "|.1||.2|T.3|.3|T.2 => ||.2.3||.1.2|.1.3", mapping, incompleteProofs.ToImmutableList(), childProof);
                if (childProof.AddReduction(firstReduction))
                {
                    var reducedFormula = reductionTemplate.NandReduction(childProof);
                    if (reducedFormula.CompareTo(startingNand) < 0)
                    {
                        var result = new Reduction(startingNand, reducedFormula, "|.1||.2|T.3|.3|T.2 => ||.2.3||.1.2|.1.3 => *", childProof.Mapping, null, childProof);
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