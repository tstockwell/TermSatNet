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
    /// The Lookup table contains a trie of all previously reduced formulas.
    /// If another formula is a substitution instance of one of the formulas in the Lookup  
    /// table then we already know how to reduce it.
    /// 
    /// </summary>
    public static async Task<ReductionRecord> TryLookupReductionAsync(this ReRiteDbContext ctx, ReductionRecord startingRecord)
    {
        // if given formula is not a nand then it must be a variable or constant and is not reducible.
        if (!(startingRecord.Formula is Nand startingNand))
        {
            return null;
        }

        var searchResults = await ctx.Lookup.FindGeneralizationsAsync(startingRecord.Formula, maxMatchCount: 1);
        foreach (var searchResult in searchResults)
        {
            // This check is required because rules don't necessarily produce shorter formulas if you use substitutions
            // that don't respect the order between the generalization's terms.
            // That is, a rule like |.2|.1.2 => |.2|.1.1 doesn't produce a shorter record if you use substitutions where .1 > .2.
            // Instead of checking that the substitutions respect this order,
            // its easier (I think) to just apply the rule and then confirm that the result is reduced.  
            var substitutions = new Dictionary<Variable, Formula>();
            foreach (var substitution in searchResult.Substitutions)
            {
                substitutions.Add(Variable.NewVariable(substitution.Key), substitution.Value);
            }
#if DEBUG
            {
                if (searchResult.Node.Value <= 0)
                {
                    throw new TermSatException($"not a valid formula id:{searchResult.Node.Value}");
                }
                if (await ctx.Formulas.FindAsync(searchResult.Node.Value) == null)
                {
                    throw new TermSatException($"not a valid formula id:{searchResult.Node.Value}");
                }
            }
#endif
            Debug.Assert(0 < searchResult.Node.Value, "not a valid formula id");
            var nonCanonicalRecord = await ctx.Formulas.AsNoTracking()
                .Where(_ => _.Id == searchResult.Node.Value)
                .FirstAsync();
            var canonicalRecord = await ctx.Formulas.AsNoTracking()
                .OrderBy(_ => _.VarCount).ThenBy(_ => _.Length).ThenBy(_ => _.Text)
                .Where(_ => _.TruthValue == nonCanonicalRecord.TruthValue)
                .FirstAsync();

            var reducedFormula = canonicalRecord.Formula.CreateSubstitutionInstance(substitutions);
            if (reducedFormula.CompareTo(startingNand) < 0)
            {
                var reducedRecord = await ctx.GetReductionRecordAsync(reducedFormula);
                return reducedRecord;
            }
        }


        return null;
    }
}