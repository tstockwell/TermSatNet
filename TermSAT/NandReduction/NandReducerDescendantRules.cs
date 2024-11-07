using System.Collections.Immutable;
using System.Linq;
using TermSAT.Common;
using TermSAT.Formulas;

namespace TermSAT.NandReduction;

public static class NandReducerDescendantRules
{

    public static Reduction ReduceDescendants(this Nand startingNand, Proof proof)
    {
        Reduction reductionResult;
        { // if either antecedent or sequent is reducible then done
            {
                var childProof = new Proof(proof);
                var result = startingNand.Antecedent.SingleNandReduction(childProof);
                if (result.RuleDescriptor != Reduction.FORMULA_IS_CANONICAL)
                {
                    var mapping = SystemExtensions.ConcatAll(
                        new[] { 0 },
                        result.Mapping.Select(i => i + 1),
                        Enumerable.Range(startingNand.Antecedent.Length + 1, startingNand.Subsequent.Length)
                    ).ToImmutableList();
                    reductionResult= new(
                        startingNand,
                        Nand.NewNand(result.ReducedFormula, startingNand.Subsequent),
                        result.RuleDescriptor,
                        mapping);
                    return reductionResult;
                }
            }
            {
                var childProof = new Proof(proof);
                var result = startingNand.Subsequent.SingleNandReduction(childProof);
                if (result.RuleDescriptor != Reduction.FORMULA_IS_CANONICAL)
                {
                    var mapping = SystemExtensions.ConcatAll(
                        Enumerable.Range(0, startingNand.Antecedent.Length + 1),
                        result.Mapping.Select(i => i + startingNand.Antecedent.Length + 1)
                    ).ToImmutableList();
                    reductionResult= new(
                        startingNand,
                        Nand.NewNand(startingNand.Antecedent, result.ReducedFormula),
                        result.RuleDescriptor,
                        mapping);
                    return reductionResult;
                }
            }
        }

        return Reduction.NoChange(startingNand);
    }
}