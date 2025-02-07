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
            var subsequentRecord = await db.GetReductionRecordAsync(startingNand.Subsequent);
            Debug.Assert(subsequentRecord.Formula.Equals(startingNand.Subsequent), "The starting formula is expected to be 'mostly canonical', that is, all terms in the formula should be canonical");

            await foreach (var relevanceResult in db.GetAllMaterialTermsAsync(subsequentRecord, formulaValue:false))
            {

                //var testValue = Constant.TRUE;
                //var replacedSubsequent = relevanceResult.UnifiedFormula; // await db.ReplaceRelevantTermAsync(subsequentRecord, relevanceResult.Formula , Constant.TRUE);
                if (relevanceResult.UnifiedFormula.PositionOf(startingNand.Subsequent) < 0) 
                {
                    var unifiedRecord = await db.GetReductionRecordAsync(relevanceResult.UnifiedFormula);
                    var canonicalTestCaseRecord = await db.GetCanonicalRecordAsync(unifiedRecord);

                    if (canonicalTestCaseRecord.Formula == Constant.FALSE)
                    {

                        var reducedFormula = Nand.NewNand(
                            relevanceResult.RelevantTerm, 
                            relevanceResult.UnifiedFormula.ReplaceAll(relevanceResult.RelevantTerm, Constant.TRUE));
                        var reducedRecord = await db.GetReductionRecordAsync(reducedFormula);

                        startingRecord.RuleDescriptor = $"wildcard swap: {relevanceResult}";
                        startingRecord.Mapping =  Enumerable.Repeat(-1, reducedFormula.Length).ToArray();
                        startingRecord.NextReductionId =  reducedRecord.Id;

                        await db.SaveChangesAsync();

                        return reducedRecord;
                    }
                }
            }
        }

        if (startingNand.Antecedent is Variable)
        {
            // construct test case formula and make sure its been fully reduced.
            var replacedSubsequent = startingNand.Subsequent.ReplaceAll(Constant.TRUE, Constant.FALSE);
            var testCaseFormula = Nand.NewNand(startingNand.Antecedent, replacedSubsequent);
            if (testCaseFormula.PositionOf(startingRecord.Formula) < 0)
            {
                var testCaseRecord = await db.GetReductionRecordAsync(testCaseFormula);
                var canonicalTestCaseRecord = await db.GetCanonicalRecordAsync(testCaseRecord);

                if (canonicalTestCaseRecord.Formula == Constant.TRUE)
                {
                    var reducedFormula = Nand.NewNand(
                        Constant.TRUE, 
                        startingNand.Subsequent.ReplaceAll(Constant.TRUE, startingNand.Antecedent));
                    var reducedRecord = await db.GetReductionRecordAsync(reducedFormula);

                    startingRecord.RuleDescriptor = $"wildcard swap inverse: {startingNand.Antecedent}";
                    startingRecord.Mapping =  Enumerable.Repeat(-1, reducedFormula.Length).ToArray();
                    startingRecord.NextReductionId =  reducedRecord.Id;

                    await db.SaveChangesAsync();

                    return reducedRecord;
                }
            }
        }

        return null;
    }
}