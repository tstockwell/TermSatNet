using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
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
    /// 
    /// >> instead of doing this all the time its now just done when needed.
    /// >> Moved to wildcard swapping. 
    /// >> - the formula should also be analyzed to discover material terms
    /// </summary>
    public static async Task<ReductionRecord> CreateReductionRecordAsync(this ReRiteDbContext db, Formula startingFormula)
    {

    }



    public static async Task<ReductionRecord> GetReductionRecordAsync(this ReRiteDbContext db, Formula startingFormula)
    {
        var reductionRecord = await db.Formulas.Where(_ => _.Text == startingFormula.Text).FirstOrDefaultAsync();
        if (reductionRecord == null)
        {
            // todo: records should not be added without verifying that they are not already reducible.
            bool isCanonical = false;
            var reducedFormula = reductionRecord.Formula;
            if (startingFormula is Nand startingNand)
            {
                var leftRecord = await db.GetReductionRecordAsync(startingNand.Antecedent);
                var rightRecord = await db.GetReductionRecordAsync(startingNand.Subsequent);
                reducedFormula = Nand.NewNand(leftRecord.Formula, rightRecord.Formula);
                reductionRecord = await db.Formulas.Where(_ => _.Text == reducedFormula.Text).FirstOrDefaultAsync();
            }
            else
            {
                isCanonical = true;
            }

            if (reductionRecord == null)
            {
                reductionRecord = new(reducedFormula, isCanonical);

                await db.AddAsync(reductionRecord);
                await db.SaveChangesAsync();
            }
        }
        return reductionRecord;
    }

    /// <summary>
    /// Returns a distinct enumeration of all the terms in a formula, including terms in all valid substitutions 
    /// </summary>
    public static async IAsyncEnumerable<ReductionRecord> GetAllDistinctReachableTermsAsync(this ReRiteDbContext db, ReductionRecord startingRecord, HashSet<long> values = null)
    {
        if (values == null)
        {
            values = new HashSet<long>();
        }
        var todo = new List<Formula>(startingRecord.Formula.AsFlatTerm());

        while (todo.Any())
        {
            var term = todo.First();
            todo.RemoveAt(0);

            var record = await db.GetReductionRecordAsync(term);

            if (!values.Contains(record.Id))
            {
                values.Add(record.Id);
                yield return record;
            }

            var substitutionRecords = await db.Formulas.Where(_ => _.NextReductionId == record.Id).ToListAsync();
            foreach (var substitution in substitutionRecords)
            {
                if (!values.Contains(substitution.Id))
                {
                    todo.Insert(0, substitution.Formula);
                }
            }
        }
    }




    /// <summary>
    /// This method is a work in progress.
    /// When considering substitutions, a formula could be reduced to many possibilities.
    /// Yet, this method returns just one, which indicates that I dont know what I'm doing.
    /// It seems pretty likely that method is going to change, but I'm not sure how yet.
    /// 
    /// Returns a new formula that is the same as the given starting formula with 
    /// all instances of the given target term with the given replacement term.
    /// Also considers wildcard substitutions.  
    /// 
    /// </summary>
    public static async Task<ReductionRecord> ReplaceAllReachableTermsAsync(this ReRiteDbContext db, Formula startingFormula, Formula targetTerm, Formula replacementTerm)
    {
        var flatTerm = startingFormula.AsFlatTerm();
        var values = new Dictionary<long, ReductionRecord>();
        var replacements = new Dictionary<int,Formula>();
        for (int i = 0; i < flatTerm.Length; )
        {
            var term = flatTerm[i];
            if (term.Equals(targetTerm))
            {
                replacements.Add(i, replacementTerm);
                goto ReplacementFound;
            }
            else
            {
                var termRecord = await db.GetReductionRecordAsync(term);
                var substitutionRecords = await db.Formulas.Where(_ => _.NextReductionId == termRecord.Id).ToListAsync();
                foreach (var substitution in substitutionRecords)
                {
                    if (substitution.Formula.Contains(targetTerm))
                    {
                        replacements.Add(i, substitution.Formula);
                        goto ReplacementFound;
                    }
                }
            }

            // no match found, move to next term
            i++;
            continue;

            ReplacementFound:
                i += term.Length;
        }

        if (!replacements.Any())
        {
            return await db.GetReductionRecordAsync(startingFormula);
        }

        var reducedFormula = flatTerm.WithReplacements(replacements);
        var reducedRecord = await db.GetReductionRecordAsync(reducedFormula);
        return reducedRecord;
    }



    //static public async Task<ReductionRecord> FindByIdAsync(this ReRiteDbContext ctx, int id) =>
    //    await ctx.Formulas.AsNoTracking().Where(_ => _.Id == id).FirstAsync();




    //static public void DeleteAll(this ReRiteDbContext ctx) => 
    //    ctx.Database.ExecuteSqlRaw($"DELETE FROM {nameof(ctx.Formulas)}");

    //static public Formula GetLastGeneratedFormula(this ReRiteDbContext ctx)
    //{
    //    var record = ctx.Formulas.AsNoTracking()
    //        .OrderByDescending(f => f.Id)
    //        .FirstOrDefault();

    //    var formula = record != null ? Formula.GetOrParse(record.Text) : null;
    //    return formula;
    //}

    //static public async Task<int> GetLengthOfCanonicalFormulasAsync(this DbSet<ReductionRecord> formulas, string truthTable) => 
    //    (await formulas.GetCanonicalRecordByTruthTable(truthTable).FirstAsync()).Length;


    //static public async Task<int> CountNonCanonicalFormulas(this DbSet<ReductionRecord> records) => 
    //    await records.GetAllNonCanonicalRecords().CountAsync();



}