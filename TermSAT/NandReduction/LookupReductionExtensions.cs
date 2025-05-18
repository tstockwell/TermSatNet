using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction;

public static class LookupReductionExtensions
{

    /// <summary>
    /// Returns any match of the given formula in the LOOKUP table.
    /// Returns the reduced formula.
    /// 
    /// The Lookup table contains a trie of all discovered rules.
    /// If another formula is a substitution instance of one of the formulas in the Lookup  
    /// table then we already know how to reduce it.
    /// 
    /// </summary>
    public static async Task<ReductionRecord> TryLookupReductionAsync(this LucidDbContext ctx, Formula startingFormula)
    {
        // if given formula is not a nand then it must be a variable or constant and is not reducible.
        if (!(startingFormula is Nand startingNand))
        {
            return null;
        }

        await foreach (var searchResult in ctx.Lookup.FindGeneralizationsAsync(startingNand))
        {
            var nonCanonicalRecord = await ctx.Expressions.FindAsync(searchResult.Node.Value);
            Debug.Assert(nonCanonicalRecord != null, $"not a valid formula id:{searchResult.Node.Value}");
            var canonicalRecord = await ctx.Expressions.GetLastReductionAsync(nonCanonicalRecord);
            if (nonCanonicalRecord.Id != canonicalRecord.Id)
            {
                var reducedFormula = canonicalRecord.Formula.CreateSubstitutionInstance(searchResult.Substitutions);

                // This check is required because rules don't necessarily produce shorter formulas if you use substitutions
                // that don't respect the order between the generalization's terms.
                // That is, a rule like |.2|.1.2 => |.2|.1.1 doesn't produce a shorter record if you use substitutions where .1 > .2.
                // Instead of checking that the substitutions respect this order,
                // its easier (I think) to just apply the rule and then confirm that the result is reduced.  
                if (reducedFormula.CompareTo(startingNand) < 0)
                {
                    var reducedRecord = await ctx.GetMostlyCanonicalRecordAsync(reducedFormula);
                    return reducedRecord;
                }
            }
        }


        return null;
    }
}