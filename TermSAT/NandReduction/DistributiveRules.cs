using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TermSAT.Common;
using TermSAT.Formulas;

namespace TermSAT.NandReduction;

public static class NandReducerDistributiveRules
{


    /// <summary>
    /// Distributive rules.
    /// Rules that seem to join or expand term instances.
    /// 
    /// 
    /// 
    /// |T|a|bc <- ||a|Tb|a|Tc    ;TT=54    ;this is just a special case of wildcard swapping, T <-> a
    /// ||a|Tb|a|Tc    
    /// => |T|a||T|Tb|T|Tc    
    /// => |T|a|bc
    /// 
    /// |T||ab|ac <- |a||Tb|Tc ;this is just an example of wildcard swapping, T <-> a
    /// 
    /// ||bc||ab|ac <- |T||b|ac|c|ab ;TT=EB
    /// ||bc||ab|ac <- |T||b|ac|c|ab ;TT=EB
    ///  
    /// </summary>
    public static Reduction ReduceDistributiveFormulas(this Nand startingNand, out Reduction result)
    {

        List<Reduction> incompleteProofs = new List<Reduction>();

        // The current logic reduces to these reduction rules...


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

                var reducedFormula = reductionTemplate.NandReduction();
                if (reducedFormula.CompareTo(startingNand) < 0)
                {
                    var reductionProof = Proof.GetReductionProof(reductionTemplate);
                    result = new Reduction(startingNand, reducedFormula, "|a|bc -> |T||a|Tb|a|Tc -> *", reductionProof.Mapping);
                    return result;
                }

                var mapping = SystemExtensions.ConcatAll(
                    Enumerable.Repeat(-1, nandAnt.Length + 6),                           // |T||a|T
                    Enumerable.Range(nandAnt.Length + 2, nandSub.Antecedent.Length),     // b                                                                                         
                    Enumerable.Repeat(-1, nandAnt.Length + 3),                           // |a|T
                    Enumerable.Range(nandAnt.Length + nandSub.Antecedent.Length + 2, nandSub.Subsequent.Length)      // c
                ).ToImmutableList();

                result = new Reduction(startingNand, reductionTemplate, "|a|bc -> |T||a|Tb|a|Tc", mapping, null, childProof);
                if (childProof.AddReduction(firstReduction))
                {
                    var reducedFormula = reductionTemplate.NandReduction(childProof);
                    if (reducedFormula.CompareTo(startingNand) < 0)
                    {
                        var result = new Reduction(startingNand, reducedFormula, "|a|bc -> |T||a|Tb|a|Tc -> *", childProof.Mapping, null, childProof);
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

                var firstReduction = new Reduction(startingNand, reductionTemplate, "|T|a|bc -> ||a|Tb|a|Tc", mapping, null, childProof);
                if (childProof.AddReduction(firstReduction))
                {
                    var reducedFormula = reductionTemplate.NandReduction(childProof);
                    if (reducedFormula.CompareTo(startingNand) < 0)
                    {
                        var result = new Reduction(startingNand, reducedFormula, "|T|a|bc -> ||a|Tb|a|Tc -> *", childProof.Mapping, null, childProof);
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

                var firstReduction = new Reduction(startingNand, reductionTemplate, "||ab|ac -> |T|a||Tb|Tc", mapping, null, childProof);
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

        // truth value AB
        // Distributive rule is subsumed by wildcard reduction
        // |a||b|Tc|c|Tb => ||bc||ab|ac
        // => |T||b|ac|c|ab => ;swap wildcard a
        // => |T||b|Tc|c|ab   ;wildcard substitution a, **canonical**

        // Distributive rule should be 
        // |.1||.2|T.3|.3|T.2 => ||.2.3||.1.2|.1.3
        // |a||b|Tc|c|ab => ||bc||ab|ac
        // => |T||b|ac|c|ab     ;swap wildcard a
        // => |T||b|Tc|c|ab     ;substitute wildcard a,  **canonical**
        // => ||bc||Tc|ab     ;|T||b|Tc|c|ab => 
        // => ||bc||ab|ac       ;distribution

        // truth value B1
        // |T||.1|T.2|.3|T.1 => ||.1.2||T.1|T.3
        // ||.1.2||T.1|T.3
        //  => |T|||.1.2.1||.1.2.3      ;swap T <-> |.1.2
        //  => |T||.1|.1.2|.3|.1.2      ;reorder   !!!!!!! THIS IS A CRITICAL TERM!!!!!!!
        //  => |T||.1|T.2|.3|.1.2    ;wildcard .1
        //  => |T||.1|T.2|.3|.1T    ;wildcard .2
        //  => |T||.1|T.2|.3|T.1    ; |T||a|Tb|c|Ta -> ||ab||Ta|Tc
        //  => ||.1.2||.1||.1.2.2|.3||.1.2.1        ;swap T <-> |.1.2
        //  => ||.1.2||.1|.2|T.1|.3|.1|T.2        ;reorder
        //  => ||.1.2||.1|.2|TT|.3|.1|T.2        
        //  => ||.1.2||.1|.2F|.3|.1|T.2        
        //  => ||.1.2||.1T|.3|.1|T.2        
        //  => ||.1.2||.1T|.3|F|T.2        
        //  => ||.1.2||.1T|.3T
        //  => ||.1.2||T.1|T.3
        // NOTE: ||.1.2||T.1|T.3 and |T||.1|T.2|.3|T.1 are a critical pair.  
        // They can both be derived from |T||.1|.1.2|.3|.1.2 using different rules


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

                var firstReduction = new Reduction(startingNand, reductionTemplate, "|.1||.2|T.3|.3|T.2 => ||.2.3||.1.2|.1.3", mapping, null, childProof);
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