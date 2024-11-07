using System.Collections.Immutable;
using System.Linq;
using TermSAT.Formulas;

namespace TermSAT.NandReduction;

public static class NandReducerConstantEliminationRules
{

    public static Reduction ReduceConstants(this Nand startingNand, Proof proof)
    {
        if (startingNand.Antecedent == Constant.TRUE)
        {
            if (startingNand.Subsequent is Constant constantConsequent)
            {
                // |TT => F, and |TF => T
                var descriptor = constantConsequent.Equals(Constant.TRUE) ? "|TT => F" : "|TF => T";
                var reducedFormula = constantConsequent.Equals(Constant.TRUE) ? Constant.FALSE : Constant.TRUE;
                var mapping = new int[] { 0 }.ToImmutableList();
                var reduction = new Reduction(startingNand, reducedFormula, descriptor, mapping);
                return reduction;
            }
            if (startingNand.Subsequent is Nand nandConsequent)
            {
                if (nandConsequent.Antecedent.Equals(Constant.TRUE))
                {
                    // |T|T.1 => .1
                    var reducedFormula = nandConsequent.Subsequent;
                    var mapping = Enumerable.Range(4, nandConsequent.Subsequent.Length).ToImmutableList();
                    var reduction = new Reduction(startingNand, reducedFormula, "|T|T.1 => .1", mapping);
                    return reduction;
                }
            }
        }
        if (startingNand.Antecedent == Constant.FALSE)
        {
            // |F.1 => T
            var mapping = new int[] { 0 }.ToImmutableList();
            var reduction = new Reduction(startingNand, Constant.TRUE, "|F.1 => T", mapping);
            return reduction;
        }
        if (startingNand.Subsequent.Equals(Constant.TRUE))
        {
            // |.1T => |T.1
            var reducedFormula = Nand.NewNand(Constant.TRUE, startingNand.Antecedent);
            var mapping = new int[] { 0, startingNand.Antecedent.Length + 1 }.AsEnumerable()
                .Concat(Enumerable.Range(1, startingNand.Antecedent.Length)
            ).ToImmutableList();
            var reduction = new Reduction(startingNand, reducedFormula, "|.1T => |T.1", mapping);
            return reduction;
        }
        if (startingNand.Subsequent == Constant.FALSE)
        {
            // |.1F => T
            var mapping = new int[] { 0 }.ToImmutableList();
            var reduction = new Reduction(startingNand, Constant.TRUE, "|.1F => T", mapping);
            return reduction;
        }

        return Reduction.NoChange(startingNand);
    }
}