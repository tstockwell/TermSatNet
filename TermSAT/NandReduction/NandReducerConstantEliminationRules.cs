using System.Collections.Immutable;
using System.Linq;
using TermSAT.Formulas;

namespace TermSAT.NandReduction;

public static class NandReducerConstantEliminationRules
{

    public static bool TryReduceConstants(this Nand startingNand, out Reduction result)
    {
        if (startingNand.Antecedent == Constant.TRUE)
        {
            if (startingNand.Subsequent is Constant constantConsequent)
            {
                // |TT => F, and |TF => T
                var descriptor = constantConsequent.Equals(Constant.TRUE) ? "|TT => F" : "|TF => T";
                var reducedFormula = constantConsequent.Equals(Constant.TRUE) ? Constant.FALSE : Constant.TRUE;
                var mapping = new int[] { -1 }.ToImmutableList();
                result = new Reduction(startingNand, reducedFormula, descriptor, mapping);
                return true;
            }
            if (startingNand.Subsequent is Nand nandConsequent)
            {
                if (nandConsequent.Antecedent.Equals(Constant.TRUE))
                {
                    // |T|T.1 => .1
                    var reducedFormula = nandConsequent.Subsequent;
                    var mapping = Enumerable.Range(4, nandConsequent.Subsequent.Length).ToImmutableList();
                    result = new Reduction(startingNand, reducedFormula, "|T|T.1 => .1", mapping);
                    return true;
                }
            }
        }
        if (startingNand.Antecedent == Constant.FALSE)
        {
            // |F.1 => T
            var mapping = new int[] { -1 }.ToImmutableList();
            result = new Reduction(startingNand, Constant.TRUE, "|F.1 => T", mapping);
            return true;
        }
        if (startingNand.Subsequent.Equals(Constant.TRUE))
        {
            // |.1T => |T.1
            var reducedFormula = Nand.NewNand(Constant.TRUE, startingNand.Antecedent);
            var mapping = Enumerable.Empty<int>()
                .Append(-1)
                .Append(startingNand.Antecedent.Length + 1)
                .Concat(Enumerable.Range(1, startingNand.Antecedent.Length))
                .ToImmutableList();
            result = new Reduction(startingNand, reducedFormula, "|.1T => |T.1", mapping);
            return true;
        }
        if (startingNand.Subsequent == Constant.FALSE)
        {
            // |.1F => T
            var mapping = new int[] { -1 }.ToImmutableList();
            result = new Reduction(startingNand, Constant.TRUE, "|.1F => T", mapping);
            return true;
        }

        result = null;
        return false;
    }
}