using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

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
    ///         Then... all instances of S in C, that are inherited from F, and that are irrelevant to P, 
    ///         may be replaced with V?F:T to create a reduced formula R.
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

        IEnumerable<Formula> commonTerms =
            startingNand.Antecedent.AsFlatTerm().Distinct().Where(f => !(f is Constant))
                .Intersect(startingNand.Subsequent.AsFlatTerm().Distinct().Where(f => !(f is Constant)));

        // Skip common terms that contain any other common terms as a targetTerm.  
        // This should go away after 'term blacklisting' has been implemented, see NandSchemeReductionTests.ReduceFormulaWithDeepProof.
        var independentTerms = commonTerms.Where(f => !commonTerms.Where(t => t.Length < f.Length && 0 <= f.PositionOf(t)).Any());

        foreach (var targetTerm in independentTerms)
        {
            // The following sections are arranged so that...
            //  - the left-side will be reduced before the right-side
            //      > because any wildcardReduction in the left-side of a formula is a bigger wildcardReduction than any wildcardReduction in the right-side.  
            //  - and terms that can be reduced to F will be reduced before terms that can be reduced to T
            //      > because a wildcardReduction to F is guaranteed to shorten the length of the formula,
            //      > whereas you cant say the same about a wildcardReduction to T.  
            {
                // construct test case formula and make sure its been fully reduced.
                var testValue = Constant.TRUE;
                var replacedSubsequent = startingNand.Subsequent.ReplaceAll(targetTerm, testValue);
                var replacedFormula = Nand.NewNand(startingNand.Antecedent, replacedSubsequent);
                var replaced = await db.GetReductionRecordAsync(replacedFormula);
                //var reduction = await db.GetCanonicalRecordAsync(replaced);
                var wildcardReduction = await db.TryGetWildcardReductionAsync(replaced, targetTerm, testValue);

                //if (reduction.CompareTo(replaced) < 0 && reduction.PositionOf(subterm) < 0)
                if (wildcardReduction.Reduction != null)
                {
                    var replaceValue = testValue == Constant.TRUE ? Constant.FALSE : Constant.TRUE;
                    // var replaced2 = replacedFormula.ReplaceAll(targetTerm, replaceValue);
                    var replaced2 = replacedFormula.ReplaceAt(wildcardReduction.StartingPosition, replaceValue);
                    Debug.Assert(!replaced2.Equals(replaced));

                    if (replaced2 is Nand nandReplaced2 && nandReplaced2.Subsequent.Equals(replacedFormula.Subsequent))
                    {
                        Debug.Assert(startingRecord.NextReductionId <= 0, $"we should not be attempting to reduce a formula that is already reduced.");

                        var reducedFormula = Nand.NewNand(nandReplaced2.Antecedent, startingNand.Subsequent);
                        var nextReduction = await db.GetReductionRecordAsync(reducedFormula);

                        startingRecord.RuleDescriptor = $"wildcard in antecedent: {targetTerm}->F";
                        startingRecord.Mapping =  Enumerable.Repeat(-1, reducedFormula.Length).ToArray();
                        startingRecord.NextReductionId =  nextReduction.Id;

                        await db.SaveChangesAsync();

                        return nextReduction;
                    }
                }
            }

            {
                var testValue = Constant.FALSE;
                var replacedSubsequent = startingNand.Subsequent.ReplaceAll(targetTerm, testValue);
                var replacedFormula = Nand.NewNand(startingNand.Antecedent, replacedSubsequent);
                var replaced = await db.GetReductionRecordAsync(replacedFormula);
                var wildcardReduction = await db.TryGetWildcardReductionAsync(replaced, targetTerm, testValue);

                if (wildcardReduction.Reduction != null)
                {
                    var replaced2 = replacedFormula.ReplaceAll(targetTerm, Constant.TRUE);

                    if (!replaced2.Equals(replacedFormula))
                    {
                        if (replaced2 is Nand nandReplaced2 && nandReplaced2.Subsequent.Equals(replacedFormula.Subsequent))
                        {
                            Debug.Assert(startingRecord.NextReductionId <= 0, $"we should not be reducing a formula that is already reduced.");

                            var reducedFormula = Nand.NewNand(nandReplaced2.Antecedent, startingNand.Subsequent);
                            var nextReduction = await db.GetReductionRecordAsync(reducedFormula);

                            startingRecord.RuleDescriptor = $"wildcard in antecedent: {targetTerm}->T";
                            startingRecord.Mapping =  Enumerable.Repeat(-1, reducedFormula.Length).ToArray();
                            startingRecord.NextReductionId =  nextReduction.Id;

                            await db.SaveChangesAsync();

                            return nextReduction;
                        }
                    }
                }
            }

            {
                var testValue = Constant.TRUE;
                var replacedAntecedent = startingNand.Antecedent.ReplaceAll(targetTerm, testValue);
                var replacedFormula = Nand.NewNand(replacedAntecedent, startingNand.Subsequent);
                var replaced = await db.GetReductionRecordAsync(replacedFormula);
                var wildcardReduction = await db.TryGetWildcardReductionAsync(replaced, targetTerm, testValue);

                if (wildcardReduction.Reduction != null)
                {
                    var replaced2 = replacedFormula.ReplaceAll(targetTerm, Constant.FALSE);

                    if (!replaced2.Equals(replacedFormula))
                    {
                        Debug.Assert(startingRecord.NextReductionId <= 0, $"we should not be reducing a formula that is already reduced.");

                        var nandReplaced2 = replaced2 as Nand;
                        Debug.Assert(nandReplaced2.Antecedent.Equals(replacedFormula.Antecedent));

                        var reducedFormula = Nand.NewNand(startingNand.Antecedent, nandReplaced2.Subsequent);
                        var nextReduction = await db.GetReductionRecordAsync(reducedFormula);

                        startingRecord.RuleDescriptor = $"wildcard in subsequent: {targetTerm}->F";
                        startingRecord.Mapping =  Enumerable.Repeat(-1, reducedFormula.Length).ToArray();
                        startingRecord.NextReductionId =  nextReduction.Id;

                        await db.SaveChangesAsync();

                        return nextReduction;
                    }
                }
            }

            {
                var testValue = Constant.FALSE;
                var replacedAntecedent = startingNand.Antecedent.ReplaceAll(targetTerm, testValue);
                var replacedFormula = Nand.NewNand(replacedAntecedent, startingNand.Subsequent);
                var replaced = await db.GetReductionRecordAsync(replacedFormula);
                var wildcardReduction = await db.TryGetWildcardReductionAsync(replaced, targetTerm, testValue);

                if (wildcardReduction.Reduction != null)
                {
                    var replaced2 = replacedFormula.ReplaceAll(targetTerm, Constant.TRUE);

                    if (!replaced2.Equals(replacedFormula))
                    {
                        if (replaced2 is Nand nandReplaced2 && nandReplaced2.Antecedent.Equals(replacedFormula.Antecedent))
                        {
                            Debug.Assert(startingRecord.NextReductionId <= 0, $"we should not be reducing a formula that is already reduced.");

                            var reducedFormula = Nand.NewNand(startingNand.Antecedent, nandReplaced2.Subsequent);
                            var nextReduction = await db.GetReductionRecordAsync(reducedFormula);

                            startingRecord.RuleDescriptor = $"wildcard in subsequent: {targetTerm}->T @ {wildcardReduction.StartingPosition}";
                            startingRecord.Mapping =  Enumerable.Repeat(-1, reducedFormula.Length).ToArray();
                            startingRecord.NextReductionId =  nextReduction.Id;

                            await db.SaveChangesAsync();

                            return nextReduction;
                        }
                    }
                }
            }
        }

        return null;
    }
}