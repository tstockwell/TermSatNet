using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TermSAT.Common;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction;

public static class DescendantRules
{

    public static async Task<ReductionRecord> TryReduceDescendantsAsync(this LucidDbContext db, ReductionRecord startingRecord)
    {
        // if given formula is not a nand then it must be a variable or constant and is not reducible.
        if (!(startingRecord.Formula is Nand startingNand))
        {
            return null;
        }

        var antecedentRecord = await db.GetMostlyCanonicalRecordAsync(startingNand.Antecedent);
        var canonicalAntecedent = await db.GetCanonicalRecordAsync(antecedentRecord);

        var subsequentRecord = await db.GetMostlyCanonicalRecordAsync(startingNand.Subsequent);
        var canonicalSubsequent = await db.GetCanonicalRecordAsync(subsequentRecord);
        var reduced = Nand.NewNand(canonicalAntecedent.Formula, canonicalSubsequent.Formula);
        if (reduced.CompareTo(startingNand) < 0)
        {
            var nextReduction = await db.GetMostlyCanonicalRecordAsync(reduced);

            startingRecord.RuleDescriptor = "reduce antecedent and subsequent";
            startingRecord.NextReductionId =  nextReduction.Id;

            await db.SaveChangesAsync();

            return nextReduction;
        }
        return null;
    }
}