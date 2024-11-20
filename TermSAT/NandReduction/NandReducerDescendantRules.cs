using System.Collections.Immutable;
using System.Linq;
using TermSAT.Common;
using TermSAT.Formulas;

namespace TermSAT.NandReduction;

public static class NandReducerDescendantRules
{

    public static bool TryReduceDescendants(this Nand startingNand, out Reduction result)
    {

        var antecedent = startingNand.Antecedent.Reduce();
        var subsequent = startingNand.Subsequent.Reduce();
        var reduced = Nand.NewNand(antecedent, subsequent);
        if (reduced.CompareTo(startingNand) < 0)
        {
            var antProof = Proof.GetReductionProof(startingNand.Antecedent);
            var subProof = Proof.GetReductionProof(startingNand.Subsequent);
            var antMapping = antProof.Mapping.Select(_ => (_ < 0) ? _ : _ + 1);
            var submapping = subProof.Mapping.Select(_ => (_ < 0) ? _ : _ + startingNand.Antecedent.Length + 1);
            var mapping = Enumerable.Repeat(-1,1).Concat(antMapping).Concat(submapping).ToImmutableList();
            var reduction = new Reduction(
                startingNand,
                reduced,
                "reduce antecedent and subsequent",
                mapping);
            result = reduction;
            return true;
        }
        result = null;
        return false;
    }
}