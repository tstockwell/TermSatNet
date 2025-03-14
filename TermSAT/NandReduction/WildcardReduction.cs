using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;
using static TermSAT.NandReduction.NandReducer;

namespace TermSAT.NandReduction;

public static class WildcardReduction
{

    /// <summary>
    /// 
    /// This is the 'wildcard wildcardReduction' algorithm.
    /// Wildcard wildcardReduction discovers sub-formulas that may be reduced to a constant.
    /// 
    ///  The concept of a wildcard as used here refers to the fact that when side of a nand formula is F 
    ///  then the value of the side of the formula is irrelevant to the value of the formula.  
    ///  RR calls these terms wildcards because they can often be replaced with multiple values without changing 
    ///  the truth table of the formula.  
    ///  This method searches/builds the proof tree associated with a given formula 
    ///  and returns the first wildcardReduction that identifies a wildcard.
    /// 
    /// This is how RR discovers wildcards...
    ///         Let F be a formula where a term S appears in both sides of the formula
    ///         Let V (for test value) be a constant value of T or F.
    ///         Let C (for test case) be the formula created by replacing all instances of S, in one side of F, with V.  
    ///         Let P (for proof) be the proof that reduces C to its canonical form.
    ///         If there are terms in C, that are inherited from F, and that are irrelevant to P...  
    ///         Then... those terms may be replaced with V?F:T to create a reduced formula R.
    ///         
    /// This method doesnt try to find *all* wildcards, just the first wildcard.  
    /// Because just finding the first is much easier to implement.
    /// 
    /// 
    /// </summary>
    public static async Task<ReductionRecord> TryGetWildcardReductionAsync(this ReRiteDbContext db, ReductionRecord startingRecord)
    {
        // if given formula is not a nand then it must be a variable or constant and is not reducible.
        if (!(startingRecord.Formula is Nand startingNand))
        {
            return null;
        }


        // The following sections are arranged so that...
        //  - the left-side will be reduced before the right-side
        //      > because any wildcardReduction in the left-side of a formula is a bigger wildcardReduction than any wildcardReduction in the right-side.  
        //  - and terms that can be reduced to F will be reduced before terms that can be reduced to T
        //      > because a wildcardReduction to F is guaranteed to shorten the length of the formula,
        //      > whereas you cant say the same about a wildcardReduction to T.  
        {
            var antecedentRecord = await db.GetMostlyCanonicalRecordAsync(startingNand.Antecedent);
            var subsequentRecord = await db.GetMostlyCanonicalRecordAsync(startingNand.Subsequent);

            var leftRecord = antecedentRecord;
            var rightRecord = subsequentRecord;

            RetryWildcardTest:;

            var reductiveGroundings = await db.Groundings
                .Where(_ => _.FormulaId == rightRecord.Id && _.FormulaValue == false)
                .ToArrayAsync();

            foreach (var reductiveGrounding in reductiveGroundings)
            {
                var replaceValue = reductiveGrounding.TermValue ? Constant.FALSE : Constant.TRUE;
                var targetTerm = await db.Formulas.FindAsync(reductiveGrounding.TermId);

                // todo: using ReplaceAll will not be good enough in the long run.
                // In the long run it will be necessary to look for terms that can be unified to targetTerm.
                var reducedLeft = leftRecord.Formula.ReplaceAll(targetTerm.Formula, replaceValue);

                var reducedFormula = (leftRecord.Id == antecedentRecord.Id) ?
                    Nand.NewNand(reducedLeft, rightRecord.Formula) :
                    Nand.NewNand(rightRecord.Formula, reducedLeft);

                if (reducedFormula.CompareTo(startingNand) < 0) // applying the reduction doesn't always produced a 'reduced' formula 
                {
                    Debug.Assert(startingRecord.NextReductionId <= 0, $"we should not be attempting to reduce a formula that is already reduced.");

                    // this call creates a record, AND groundings, AND completes proof tree for reducedFormula (if not already done)
                    var nextReduction = await db.GetMostlyCanonicalRecordAsync(reducedFormula);

                    startingRecord.RuleDescriptor = $"wildcard in antecedent: {targetTerm}->F";
                    startingRecord.Mapping =  Enumerable.Repeat(-1, reducedFormula.Length).ToArray();
                    startingRecord.NextReductionId =  nextReduction.Id;

                    await db.SaveChangesAsync();

                    // return the first reduction found
                    return nextReduction;
                }
            }

            // look for a reductive groundings in the right side first, and then the left.
            // here, if we just got done with the right, we flip the record refs and retry to do the left.
            if (leftRecord.Id == antecedentRecord.Id)
            {
                leftRecord = subsequentRecord; 
                rightRecord = antecedentRecord;
                goto RetryWildcardTest;
            }
        }

        return null;
    }
}