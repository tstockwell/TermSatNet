using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;
using static TermSAT.NandReduction.NandReducer;

namespace TermSAT.NandReduction;

public static class WildcardSwapping
{

    /// <summary>
    /// 
    /// This is wildcard swapping....  
    ///     wildcard swapping is chiral, this form replaces many terms with a constant value...
    ///         Let F be a formula of the form |LR, where one of L and R is the constant T. 
    ///         Let V (for test value) be a variable that has a constant value of T or F.
    ///         Let S be any term in F, including wildcard values that can be obtained from direct ancestor formulas.
    ///         Let C (for test case) be the formula created by replacing all instances of S in one side of F with V.  
    ///         If C reduces to F then all instances of S in F may be replaced with T, 
    ///         and the leading T replaced with S, if .  
    ///         
    ///     The other form of swapping, where constants are replaced by terms, is only valid when 
    ///     S.Length == 1, and is covered by some hard-coded rules in NandReducerCommutativeRules.
    /// 
    /// </summary>
    public static async Task<ReductionRecord> TryGetSwappedWildcardReductionAsync(this ReRiteDbContext db, ReductionRecord startingRecord)
    {
        // if given formula is not a nand then it must be a variable or constant and is not reducible.
        if (!(startingRecord.Formula is Nand startingNand))
        {
            return null;
        }

        if (startingNand.Antecedent == Constant.TRUE)
        {
            var rightRecord = await db.GetMostlyCanonicalRecordAsync(startingNand.Subsequent);

            var reductiveGroundings = await db.Groundings
                .Where(_ => _.FormulaId == rightRecord.Id && _.FormulaValue == false)
                .ToArrayAsync();

            foreach (var reductiveGrounding in reductiveGroundings)
            {
                var replaceValue = reductiveGrounding.TermValue ? Constant.FALSE : Constant.TRUE;
                var targetTerm = await db.Formulas.FindAsync(reductiveGrounding.TermId);

                // todo: using ReplaceAll will not be good enough in the long run.
                // In the long run it will be necessary to look for terms that can be unified to targetTerm.
                var reducedRight = rightRecord.Formula.ReplaceAll(targetTerm.Formula, replaceValue);

                var reducedFormula = Nand.NewNand(targetTerm.Formula, reducedRight);

                if (reducedFormula.CompareTo(startingNand) < 0) // applying the reduction doesn't always produced a 'reduced' formula 
                {
                    Debug.Assert(startingRecord.NextReductionId <= 0, $"we should not be attempting to reduce a formula that is already reduced.");

                    // this call creates a record, AND groundings, AND completes proof tree for reducedFormula (if not already done)
                    var nextReduction = await db.GetMostlyCanonicalRecordAsync(reducedFormula);

                    startingRecord.RuleDescriptor = $"wildcard swap: {targetTerm}";
                    startingRecord.Mapping =  Enumerable.Repeat(-1, reducedFormula.Length).ToArray();
                    startingRecord.NextReductionId =  nextReduction.Id;

                    await db.SaveChangesAsync();

                    // return the first reduction found
                    return nextReduction;
                }
            }
        }

        else if (startingNand.Antecedent is Variable)
        {
            var rightRecord = await db.GetMostlyCanonicalRecordAsync(startingNand.Subsequent);

            var reductiveGroundings = await db.Groundings
                .Where(_ => _.FormulaId == rightRecord.Id && _.FormulaValue == false)
                .ToArrayAsync();

            foreach (var reductiveGrounding in reductiveGroundings)
            {
                var replaceValue = startingNand.Antecedent;
                var targetTerm = await db.Formulas.FindAsync(reductiveGrounding.TermId);

                // todo: using ReplaceAll will not be good enough in the long run.
                // In the long run it will be necessary to look for terms that can be unified to targetTerm.
                var reducedRight = rightRecord.Formula.ReplaceAll(targetTerm.Formula, replaceValue);

                var reducedFormula = Nand.NewNand(targetTerm.Formula, reducedRight);

                if (reducedFormula.CompareTo(startingNand) < 0) // applying the reduction doesn't always produced a 'reduced' formula 
                {
                    Debug.Assert(startingRecord.NextReductionId <= 0, $"we should not be attempting to reduce a formula that is already reduced.");

                    // this call creates a record, AND groundings, AND completes proof tree for reducedFormula (if not already done)
                    var nextReduction = await db.GetMostlyCanonicalRecordAsync(reducedFormula);

                    startingRecord.RuleDescriptor = $"wildcard swap: {targetTerm}";
                    startingRecord.Mapping =  Enumerable.Repeat(-1, reducedFormula.Length).ToArray();
                    startingRecord.NextReductionId =  nextReduction.Id;

                    await db.SaveChangesAsync();

                    // return the first reduction found
                    return nextReduction;
                }
            }
        }

        return null;
    }
}