using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using TermSAT.Formulas;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Runtime.CompilerServices;
using TermSAT.NandReduction;

namespace TermSAT.RuleDatabase;

public static class ReRiteDbContextExtensions
{

    static public IQueryable<Formula> ToFormulas(this IQueryable<ReductionRecord>db) =>
        db.Select(_ => Formula.GetOrParse(_.Text));

    static public async Task IsSubsumedBySchemeAsync(this ReRiteDbContext ctx, ReductionRecord record, string value)
    {
        record.IsSubsumed = value;

        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
    }

    /// <summary>
    /// Use this method, not DbContext.Add, to add a ReductionRecord to the DB because 
    /// besides creating a reduction record...
    /// - the formula should also be added to the LOOKUP table
    /// - GroundingRecords need to be calculated and created for the new formula
    /// 
    /// NOTE: The given formula should be 'mostly canonical'.
    /// 
    /// </summary>
    public static async Task InsertFormulaRecordAsync(this ReRiteDbContext db, ReductionRecord record)
    {
        await db.AddAsync(record);

        // have to save here in order for id to be populated before calling AddGeneralizationAsync
        await db.SaveChangesAsync();

        // NOTE: The given formula should not be reducible using the current lookups.
#if DEBUG
        //{
        //    await foreach (var generalization in db.Lookup.FindGeneralizationsAsync(record.Formula))
        //    {
        //        if (generalization != null)
        //        {
        //            var generalizationRecord = await db.Formulas.FindAsync(generalization.Node.Value);
        //            var canonicalRecord = await db.GetCanonicalRecordAsync(generalizationRecord);
        //            if (canonicalRecord.Formula.CompareTo(generalizationRecord.Formula) < 0)
        //            {
        //                throw new TermSatException("Attempt to add lookup record for a formula that is currently reducible via lookup");
        //            }
        //        }
        //    }
        //}
#endif
        // it used to be that RR only put non-canonical records in the lookup table.  
        // But that was when RR populated the formula table in a pre-defined order,
        // so it could immediately know which formulas are canonical and which were not.   
        // But the formula db is no longer populated in any particular order.
        // Now, all formulas (canonical and mostly canonical) are added to the lookup db, because...
        //  - if a mostly-canonical formula matches a non-canonical lookup value then
        //      RR knows immediately that the formula may be reduced
        //      using the non-canonical and its canonical as a reduction rule.
        //  - if a mostly-canonical formula matches a canonical lookup value then
        //      RR knows immediately that the formula is not totally reducible.
        await db.AddGeneralizationAsync(record);

        // NOTE...
        // Groundings are calculated when a formula is inserted because wildcard analysis requires the 'proof tree' to be complete.  
        // In other words, groundings *will* eventually be calculated, so there's no benefit to calculating them lazily.  
        // And for many formulas, RR currently calculates groundings for a formula more than once, so there's benefit to saving them.  
        // And calculating them here makes RR's logic simpler.
        await db.AddGroundings(record);
    }


    /// <summary>
    /// Returns a formula that is reduced as much as possible using just the reduction rules produced by first reducing sub-formulas.
    /// The idea is that reducing sub-terms first, and applying resulting reduction rules to the outmost formula represents 
    /// a starting formula that should have a corresponding record in the rule DB since it can't be reduced by any of the current 
    /// reduction rules.
    /// </summary>
    public static async Task<ReductionRecord> GetMostlyCanonicalRecordAsync(this ReRiteDbContext db, Formula startingFormula)
    {
        var reductionRecord = await db.Formulas.Where(_ => _.Text == startingFormula.Text).FirstOrDefaultAsync();
        if (reductionRecord == null)
        {
            bool isCanonical = false;
            var mostlyCanonicalFormula = startingFormula;
            if (startingFormula is Nand startingNand)
            {
                // RR assumes that the 'proof tree' for shorter is always complete
                // and that new formulas are always 'mostly canonical'.  
                // That is, all terms in the formula are canonical except for the formula itself.  
                // So, get the 'mostly canonical' formula from the given formula.
                var leftRecord = await db.GetMostlyCanonicalRecordAsync(startingNand.Antecedent);
                leftRecord = await db.GetCanonicalRecordAsync(leftRecord);
                var rightRecord = await db.GetMostlyCanonicalRecordAsync(startingNand.Subsequent);
                rightRecord = await db.GetCanonicalRecordAsync(rightRecord);

                mostlyCanonicalFormula = Nand.NewNand(leftRecord.Formula, rightRecord.Formula);
                reductionRecord = await db.Formulas.Where(_ => _.Text == mostlyCanonicalFormula.Text).FirstOrDefaultAsync();
                if (reductionRecord == null)
                {
                    var nextReductionRecord = await db.TryLookupReductionAsync(mostlyCanonicalFormula);
                    while (nextReductionRecord != null)
                    {
                        reductionRecord = nextReductionRecord;
                        mostlyCanonicalFormula = nextReductionRecord.Formula;
                        nextReductionRecord = await db.TryLookupReductionAsync(mostlyCanonicalFormula);
                    }
                }
            }
            else
            {
                isCanonical = true;
            }

            if (reductionRecord == null)
            {
                reductionRecord = new(mostlyCanonicalFormula, isCanonical);

                await db.InsertFormulaRecordAsync(reductionRecord);
            }
        }
        return reductionRecord;
    }

    /// <summary>
    /// This method discovers groundings in the given formula and creates records in the db.Groundings table.
    /// 
    /// RR used to call this 'wildcard reduction'.  
    /// 
    /// This is how RR discovers wildcards/groundings...
    ///         Let F be a formula where a term S appears in both sides of the formula
    ///         Let V (for test value) be a constant value of T or F.
    ///         Let C (for test case) be the formula created by replacing all instances of S, in one side of F, with V.  
    ///         Let P (for proof) be the proof that reduces C to its canonical form.
    ///         Then... all instances of S in C, that are inherited from F, and that are irrelevant to P, 
    ///         may be replaced with V?F:T to create a reduced formula R.
    ///         
    /// </summary>
    public static async Task AddGroundings(this ReRiteDbContext db, ReductionRecord startingRecord)
    {
        if (startingRecord.Length <= 1)
        {
            //if (startingRecord.Formula is Variable)
            //{
                // A variable can be forced to T or F by setting it to T or F.
                // So variables start with two groundings, one that compels the variable to T, 
                // and one that compels the formula to F.
                // Same for constants.
                await db.Groundings.AddAsync(new()
                {
                    FormulaId = startingRecord.Id,
                    FormulaValue = true,
                    TermId = startingRecord.Id,
                    TermValue = true,
                    Positions = new[] { 0 }
                });
                await db.Groundings.AddAsync(new()
                {
                    FormulaId = startingRecord.Id,
                    FormulaValue = false,
                    TermId = startingRecord.Id,
                    TermValue = false,
                    Positions = new[] { 0 }
                });
                await db.SaveChangesAsync();
            //}

            return;
        }

        // add groundings for a nand
        // first, add groundings to T, when either side is F.
        // then add grounding to F when both sides are true

        var startingNand = startingRecord.Formula as Nand;

        var leftRecord = await db.Formulas.Where(_ => _.Text == startingNand.Antecedent.Text).FirstAsync();
        var leftFGroundings = await db.Groundings.Where(_ => _.FormulaId == leftRecord.Id && _.FormulaValue == false).ToArrayAsync();

        foreach (var leftGrounding in leftFGroundings)
        {
            await db.Groundings.AddAsync(new()
            {
                FormulaId = startingRecord.Id,
                FormulaValue = true,
                TermId = leftGrounding.TermId,
                TermValue = leftGrounding.TermValue,
                Positions = new[] { 1 }
            });
        }

        var rightRecord = await db.Formulas.Where(_ => _.Text == startingNand.Subsequent.Text).FirstAsync();
        var rightFGroundings = await db.Groundings.Where(_ => _.FormulaId == rightRecord.Id && _.FormulaValue == false).ToArrayAsync();
        foreach (var rightGrounding in rightFGroundings)
        {
            await db.Groundings.AddAsync(new()
            {
                FormulaId = startingRecord.Id,
                FormulaValue = true,
                TermId = rightGrounding.TermId,
                TermValue = rightGrounding.TermValue,
                Positions = new[] { leftRecord.Length + 1 }
            });
        }

        var leftTGroundings = await db.Groundings.Where(_ => _.FormulaId == leftRecord.Id && _.FormulaValue == true).ToArrayAsync();
        var rightTGroundings = await db.Groundings.Where(_ => _.FormulaId == rightRecord.Id && _.FormulaValue == true).ToArrayAsync();
        var groundingTerms = leftTGroundings.Select(_ => _.TermId).Intersect(rightTGroundings.Select(_ => _.TermId));
        foreach (var groundingTermId in groundingTerms)
        {
            var groundingRecord = leftTGroundings.Where(_ => _.TermId == groundingTermId).First();
            var positions = groundingRecord.Positions.Select(_ => _ + 1).ToArray();
            await db.Groundings.AddAsync(new()
            {
                FormulaId = startingRecord.Id,
                FormulaValue = false,
                TermId = groundingTermId,
                TermValue = groundingRecord.TermValue,
                Positions = positions
            });
        }

        await db.SaveChangesAsync();
    }


    public static async Task AddCompletionMarkerAsync(this ReRiteDbContext db, ReductionRecord proof)
    {
        Debug.Assert(string.IsNullOrEmpty(proof.RuleDescriptor), "Cant complete a formula that's already been reduced");
        proof.RuleDescriptor = ReductionRecord.PROOF_IS_COMPLETE;
        await db.SaveChangesAsync();
    }




}