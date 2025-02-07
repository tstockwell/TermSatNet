
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction;

public record struct RelevanceResult(Formula RelevantTerm, Formula UnifiedFormula) { }

public static class RelevanceProofs
{
    /// <summary>
    /// Discovers 'material terms' in the given formula and records them in the ReRiteDbContext.MaterialTerms table.
    /// </summary>
    static public async Task CompleteMaterialTermDiscoveryAsync(this ReRiteDbContext db, ReductionRecord startingRecord)
    {
        if (startingRecord.IsDiscoveryComplete)
        {
            return;
        }

        if (startingRecord.Formula is Nand startingNand)
        {
            var allTerms = startingRecord.Formula.AsFlatTerm().Distinct().ToArray();
            var leftRecord = await db.Formulas.Where(_ => _.Text == startingNand.Antecedent.Text).FirstAsync();
            var rightRecord = await db.Formulas.Where(_ => _.Text == startingNand.Subsequent.Text).FirstAsync();

            // Before doing wildcard analysis, make sure all the subterms are completed.
            // Processing from the end to the start of the flat term is a way of processing from bottom up.
            // Processing backwards is meant to avoid blowing out the stack like would happen if processing from top down.
            foreach (var term in allTerms.Skip(1).Reverse())
            {
                var record = await db.GetReductionRecordAsync(term);
                if (!record.IsDiscoveryComplete)
                {
                    await db.CompleteMaterialTermDiscoveryAsync(record);
                }
            }

            // add all material term records that can force this formula to be false.
            // Besides the formula itself, only material terms that can compel BOTH the left and right sides
            // to be TRUE can compel this formula to be false
            {
                await db.MaterialTerms.AddAsync(new(startingRecord.Id, false, startingRecord.Id, false));
                var materialTerms = await db.MaterialTerms
                    .Where(_ => _.FormulaId == leftRecord.Id && _.FormulaValue == true)
                    .Join(
                        db.MaterialTerms.Where(_ => _.FormulaId == rightRecord.Id && _.FormulaValue == true),
                         _ => _.TermId, 
                         _ => _.TermId,
                         (l,r) => new { Left = l, Right = r }
                        )
                    .ToArrayAsync();
                foreach (var materialTerm in materialTerms)
                {
                    await db.MaterialTerms.AddAsync(new(startingRecord.Id, false, materialTerm.Le, false));
                }
            }

            foreach (var targetTerm in allTerms)
            {
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



            startingFormula.IsDiscoveryComplete = true;
            await db.SaveChangesAsync();
        }
        else if (startingRecord.Formula is Variable)
        {
            await db.MaterialTerms.AddAsync(new(startingRecord.Id, true, startingRecord.Id, true));
            await db.MaterialTerms.AddAsync(new(startingRecord.Id, false, startingRecord.Id, false));
            await db.SaveChangesAsync();
        }
        else if (startingRecord.Formula is Constant)
        {
            if (startingRecord.Formula == Constant.TRUE)
            {
                await db.MaterialTerms.AddAsync(new(startingRecord.Id, true, startingRecord.Id, true));
            }
            else
            {
                await db.MaterialTerms.AddAsync(new(startingRecord.Id, false, startingRecord.Id, false));
            }
            await db.SaveChangesAsync();
        }
        else
        {
            throw new TermSatException($"unhandled formula type:{startingRecord.Formula.GetType().FullName}");
        }
        if (!startingRecord.IsDiscoveryComplete)
        {

        }
    }

    /// <summary>
    /// Emit all material terms that can compel the given starting formula to (formulaValue ? Constant.TRUE : Constant.FALSE).
    /// </summary>
    static public async IAsyncEnumerable<RelevanceResult> GetAllMaterialTermsAsync(this ReRiteDbContext db, ReductionRecord startingRecord, bool formulaValue)
    {
        if (startingRecord.Formula is Nand startingNand)
        {
            await db.CompleteRelevanceProofAsync(startingRecord);

            if (formulaValue)
            {
                // return 
            }

            // Emit every term in the starting formula, using the starting formula as the unified formula.
            // Then, include, 

            if (!startingFormula.IsRelevanceProofComplete)
            {
                startingFormula.IsRelevanceProofComplete = true;
                await db.SaveChangesAsync();
            }
        }
        else if (startingRecord.Formula is Variable)
        {
            yield return new(RelevantTerm:startingRecord.Formula, UnifiedFormula:startingRecord.Formula);
        }
        else if (startingRecord.Formula is Constant)
        {
            if (formulaValue == (startingRecord.Formula == Constant.TRUE))
            {
                yield return new(RelevantTerm: startingRecord.Formula, UnifiedFormula: startingRecord.Formula);
            }
        }
        else
        {
            throw new TermSatException($"unhandled formula type:{startingRecord.Formula.GetType().FullName}");
        }
    }
}
