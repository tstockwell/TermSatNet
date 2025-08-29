using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction;

public static class ConstantEliminationRules
{

    public static async Task<ReductionRecord> TryReduceConstantsAsync(this LucidDbContext db, ReductionRecord startingRecord)
    {
        // if given formula is not a nand then it must be a variable or constant and is not reducible.
        if (!(startingRecord.Formula is Nand startingNand))
        {
            return null;
        }

        if (startingNand.Antecedent == Constant.TRUE)
        {
            if (startingNand.Subsequent is Constant constantConsequent)
            {
                // |TT => F, and |TF => T
                var reducedFormula = constantConsequent.Equals(Constant.TRUE) ? Constant.FALSE : Constant.TRUE;
                var nextReduction = await db.GetMostlyCanonicalRecordAsync(reducedFormula);
                startingRecord.RuleDescriptor = constantConsequent.Equals(Constant.TRUE) ? "cut elim: |TT => F" : "erasure: |TF => T";
                startingRecord.NextReductionId =  nextReduction.Id;

                await db.SaveChangesAsync();

                return nextReduction;
            }
            if (startingNand.Subsequent is Nand nandConsequent)
            {
                if (nandConsequent.Antecedent.Equals(Constant.TRUE))
                {
                    // |T|T.1 => .1
                    var reducedFormula = nandConsequent.Subsequent;
                    var nextReduction = await db.GetMostlyCanonicalRecordAsync(reducedFormula);
                    startingRecord.RuleDescriptor = "dbl-neg elim: |T|T.1 => .1";
                    startingRecord.NextReductionId =  nextReduction.Id;

                    await db.SaveChangesAsync();

                    return nextReduction;
                }
            }
        }
        if (startingNand.Antecedent == Constant.FALSE)
        {
            // |F.1 => T
            var nextReduction = await db.GetMostlyCanonicalRecordAsync(Constant.TRUE);
            startingRecord.RuleDescriptor = "erasure: |F.1 => T";
            startingRecord.NextReductionId =  nextReduction.Id;

            await db.SaveChangesAsync();

            return nextReduction;
        }
        if (startingNand.Subsequent.Equals(Constant.TRUE))
        {
            // |.1T => |T.1
            var reducedFormula = Nand.NewNand(Constant.TRUE, startingNand.Antecedent);
            var nextReduction = await db.GetMostlyCanonicalRecordAsync(reducedFormula);
            startingRecord.RuleDescriptor = "ordering: |.1T => |T.1";
            startingRecord.NextReductionId =  nextReduction.Id;

            await db.SaveChangesAsync();

            return nextReduction;
        }
        if (startingNand.Subsequent == Constant.FALSE)
        {
            // |.1F => T
            var nextReduction = await db.GetMostlyCanonicalRecordAsync(Constant.TRUE);
            startingRecord.RuleDescriptor = "erasure: |.1F => T";
            startingRecord.NextReductionId =  nextReduction.Id;

            await db.SaveChangesAsync();

            return nextReduction;
        }

        return null;
    }
}