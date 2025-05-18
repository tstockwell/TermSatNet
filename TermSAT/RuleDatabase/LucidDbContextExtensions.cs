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
using Microsoft.VisualBasic;

namespace TermSAT.RuleDatabase;

public static class LucidDbContextExtensions
{
    private static readonly string KEY_TRUE = nameof(KEY_TRUE);
    private static readonly string KEY_FALSE = nameof(KEY_FALSE);

    static public async Task BootstrapAsync(this LucidDbContext lucid)
    {
        await lucid.Lookup.AddRootAsync();

        // bootstrap expressions and cofactors for constants
        var falseRecord = new ReductionRecord(Constant.FALSE, isCanonical: true);
        var trueRecord = new ReductionRecord(Constant.TRUE, isCanonical: true);
        await lucid.Expressions.AddAsync(falseRecord);
        await lucid.Expressions.AddAsync(trueRecord);
        await lucid.SaveChangesAsync();
        Debug.Assert(falseRecord.Id == 1);
        Debug.Assert(trueRecord.Id == 2);


        await lucid.Cofactors.AddAsync(
            new CofactorRecord(
                expressionId: trueRecord.Id, 
                subtermId: trueRecord.Id,
                replacementId: trueRecord.Id,
                conclusionId: trueRecord.Id));
        await lucid.Cofactors.AddAsync(
            new CofactorRecord(
                expressionId: trueRecord.Id,
                subtermId:  trueRecord.Id,
                replacementId: falseRecord.Id,
                conclusionId: falseRecord.Id));
        await lucid.Cofactors.AddAsync(
            new CofactorRecord(
                expressionId: falseRecord.Id,
                subtermId:  falseRecord.Id,
                replacementId: trueRecord.Id,
                conclusionId: trueRecord.Id));
        await lucid.Cofactors.AddAsync(
            new CofactorRecord(
                expressionId: falseRecord.Id,
                subtermId:  falseRecord.Id,
                replacementId: falseRecord.Id,
                conclusionId: falseRecord.Id));

        await lucid.Meta.AddAsync(new MetaRecord(KEY_TRUE, trueRecord.Id.ToString()));
        await lucid.Meta.AddAsync(new MetaRecord(KEY_FALSE, falseRecord.Id.ToString()));

        await lucid.SaveChangesAsync();


    }


    static public Task<long> GetConstantExpressionIdAsync(this LucidDbContext lucid, bool value)
    {
        return Task.FromResult(value ? 2L : 1L);
        //var keyName = value == true ? nameof(KEY_TRUE) : nameof(KEY_FALSE);
        //var rec = await lucid.Meta.FindAsync(keyName); 
        //if (rec == null)
        //{
        //    var c = value ? Constant.TRUE : Constant.FALSE;
        //    var r = await lucid.Expressions.Where(_ => _.Text == c.Text).FirstOrDefaultAsync();
        //    if (r == null)
        //    {
        //        r = new ReductionRecord(c, isCanonical: true);
        //        await lucid.InsertFormulaRecordAsync(r);
        //        Debug.Assert(0 < r.Id, "expected Id to be set");
        //    }
        //    await lucid.Meta.AddAsync(new(keyName, r.Id.ToString()));
        //    lucid.SaveChanges();

        //    rec = await lucid.Meta.FindAsync(keyName);
        //    if (rec == null)
        //    {
        //        throw new Exception($"wtf, failed to create constant expressions or something");
        //    }
        //}
        //return long.Parse(rec.Value);
    }

    static public IQueryable<Formula> ToFormulas(this IQueryable<ReductionRecord>db) =>
        db.Select(_ => Formula.GetOrParse(_.Text));

    static public async Task IsSubsumedBySchemeAsync(this LucidDbContext ctx, ReductionRecord record, string value)
    {
        record.IsSubsumed = value;

        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
    }

    /// <summary>
    /// Use this method, not DbContext.Add, to add a ReductionRecord to the DB 
    /// because besides creating a reduction record...
    /// 
    /// - the formula should also be added to the LOOKUP table
    /// - Cofactors need to be calculated and created for the new formula
    /// 
    /// NOTE: The given formula should be 'mostly canonical'.
    /// 
    /// </summary>
    public static async Task InsertFormulaRecordAsync(this LucidDbContext db, ReductionRecord record)
    {

#if DEBUG
        // NOTE: The given formula should not be reducible using the current lookups.
        {
            await foreach (var generalization in db.Lookup.FindGeneralizationsAsync(record.Formula))
            {
                var nonCanonicalRecord = await db.Expressions.FindAsync(generalization.Node.Value);
                var mostReducedRecord = await db.Expressions.GetLastReductionAsync(nonCanonicalRecord);
                if (0 < mostReducedRecord.NextReductionId)
                {
                    var reducedFormula = record.Formula.CreateSubstitutionInstance(generalization.Substitutions);
                    if (reducedFormula.CompareTo(record.Formula) < 0)
                    {
                        throw new TermSatException("Attempt to add lookup record for a formula that is currently reducible via lookup");
                    }
                }
            }
        }
#endif

        // before adding an expression, make sure that all it's subterms have already been added.
        // This is because we're gonna need the cofactors for all those subterms, and it's just
        // easier to create them now instead of checking everywhere.
        if (record.Formula is Nand nand)
        {
            await db.GetMostlyCanonicalRecordAsync(nand.Antecedent);
            await db.GetMostlyCanonicalRecordAsync(nand.Subsequent);
        }

        await db.AddAsync(record);

        // have to save here in order for id to be populated before calling AddGeneralizationAsync
        await db.SaveChangesAsync();

        // it used to be that RR only put non-canonical records in the lookup table.  
        // But that was when RR populated the formula table in a pre-defined order,
        // so it could immediately know which formulas are canonical and which were not.   
        // But the formula db is no longer populated in any particular order.
        // Now, all formulas (canonical and mostly canonical) are added to the lookup db
        // because any match can save a lot of work...
        //  - if a mostly-canonical formula matches a non-canonical lookup value then
        //      RR knows immediately that the formula may be reduced
        //      using the non-canonical and canonical expressions as a reduction rule.
        //  - if a mostly-canonical formula matches a canonical lookup value then
        //      RR knows immediately that the formula is NOT reducible and doesn't have
        //      to bother looking for a reduction.
        await db.AddGeneralizationAsync(record);

        // NOTE...
        // Cofactors are calculated when a formula is inserted
        // because wildcard analysis requires the 'proof tree' to be complete.  
        // In other words, cofactors *will* eventually be calculated, so there's no benefit to calculating them lazily.  
        // And for many formulas, RR currently calculates cofactors for a formula more than once,
        // so there's benefit to saving them.  
        // And calculating them here makes RR's logic simpler.
        await db.AddCofactors(record);
    }


    /// <summary>
    /// Returns a formula that is reduced as much as possible using just the reduction rules produced by first reducing sub-formulas.
    /// The idea is that reducing sub-terms first, and applying resulting reduction rules to the outmost formula represents 
    /// a starting formula that should have a corresponding record in the rule DB since it can't be reduced by any of the current 
    /// reduction rules.
    /// 
    /// If given a canonical expression then the record for the canonical expression is returned.
    /// 
    /// This method is commonly used to fetch or create a record for an expression.  
    /// </summary>
    public static async Task<ReductionRecord> GetMostlyCanonicalRecordAsync(this LucidDbContext db, Formula startingFormula)
    {
        var reductionRecord = await db.Expressions.Where(_ => _.Text == startingFormula.Text).FirstOrDefaultAsync();
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
                reductionRecord = await db.Expressions.Where(_ => _.Text == mostlyCanonicalFormula.Text).FirstOrDefaultAsync();
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
    /// Repeatedly reduces startingFormula until it reaches its canonical form.  
    /// The process stops when no more reductions can be made.
    /// Always returns a logically equivalent formula in canonical form.  
    /// </summary>
    public static async Task<ReductionRecord> GetCanonicalRecordAsync(this LucidDbContext db, ReductionRecord reductionRecord)
    {
        if (0 < reductionRecord.CanonicalReductionId)
        {
            var canonicalRecord = await db.Expressions.FindAsync(reductionRecord.CanonicalReductionId);
            Debug.Assert(canonicalRecord != null, $"Failed to find canonical formula: {reductionRecord.CanonicalReductionId}");
            return canonicalRecord;
        }

        if (reductionRecord.IsCanonical)
        {
            return reductionRecord;
        }

        var mostReducedRecord = await db.Expressions.GetLastReductionAsync(reductionRecord);

        while (!mostReducedRecord.IsCanonical)
        {
            var nextReduction = await db.TryGetNextReductionAsync(mostReducedRecord);
            if (nextReduction == null)
            {
                if (!mostReducedRecord.IsCanonical)
                {
                    // the given formula is canonical
                    await db.AddCompletionMarkerAsync(mostReducedRecord);
                }
                break;
            }

            mostReducedRecord = nextReduction; 
        }

        return mostReducedRecord;
    }




    public static async Task AddCompletionMarkerAsync(this LucidDbContext db, ReductionRecord proof)
    {
#if DEBUG
        if (!string.IsNullOrEmpty(proof.RuleDescriptor))
        {
            throw new TermSatException("Cant complete a formula that's already been reduced");
        }
#endif
        proof.RuleDescriptor = ReductionRecord.PROOF_IS_COMPLETE;
        await db.SaveChangesAsync();
    }




}