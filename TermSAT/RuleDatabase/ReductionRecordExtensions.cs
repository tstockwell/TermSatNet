using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.NandReduction;

namespace TermSAT.RuleDatabase;

public static class ReductionRecordExtensions
{
    public static IQueryable<ReductionRecord> InFormulaOrder(this IQueryable<ReductionRecord> dbset) => 
        dbset.OrderBy(_ => _.VarCount).ThenBy(_ => _.Length).ThenBy(_ => _.Text);

    public static IQueryable<ReductionRecord> WhereCanonical(this IQueryable<ReductionRecord> db) => 
        db.Where(_ => _.RuleDescriptor == ReductionRecord.PROOF_IS_COMPLETE);



    /// <summary>
    /// returns the last reduction of the given starting formula.
    /// If the starting formula has not yet been reduced then just returns the given starting formula.
    /// 
    /// Use this method 
    /// </summary>
    public static async Task<ReductionRecord> GetLastReductionAsync(this DbSet<ReductionRecord> db, ReductionRecord proof)
    {
        var nextProof = proof;
        while (nextProof != null)
        {
            if (nextProof.RuleDescriptor == ReductionRecord.PROOF_IS_COMPLETE)
            {
                break;
            }
            if (nextProof.NextReductionId <= 0)
            {
                break;
            }
            nextProof = await db.FindAsync(nextProof.NextReductionId);
        }
        return nextProof;
    }

    static public IQueryable<ReductionRecord> GetAllCanonicalRecords(this IQueryable<ReductionRecord> db) =>
        db.Select(_ => _.TruthValue).Distinct()
        .Select(t =>
            db.OrderBy(_ => _.VarCount)
            .ThenBy(_ => _.Length)
            .ThenBy(_ => _.Text)
            .Where(_ => _.TruthValue == t).First());


    static public IQueryable<string> GetAllTruthValues(this IQueryable<ReductionRecord> db) =>
        db.Select(_ => _.TruthValue).Distinct();


    static public IQueryable<ReductionRecord> GetAllNonCanonicalRecords(this IQueryable<ReductionRecord> db) =>
        db.GetAllTruthValues()
                .SelectMany(t =>
                    db.OrderBy(_ => _.VarCount)
                    .ThenBy(_ => _.Length)
                    .ThenBy(_ => _.Text)
                    .Where(_ => _.TruthValue == t)
                    .Skip(1));

    static public Task<int> GetLengthOfLongestCanonicalFormulaAsync(this IQueryable<ReductionRecord> db) =>
        db.GetAllNonCanonicalRecords().OrderBy(_ => _.Length).Select(_ => _.Length).FirstAsync();


    /**
     * Expressions longer than this length are guaranteed to be reducible with rules,
     * generated from previous formulas. 
     * Therefore processing can stop when formulas get this long.
     */
    static public async Task<int> LengthOfLongestPossibleNonReducibleFormulaAsync(this IQueryable<ReductionRecord> db)
    {
        int maxLength = await db.GetLengthOfLongestCanonicalFormulaAsync();
        if (maxLength <= 0) // we don't know the length of longest formula yet
            return int.MaxValue;
        return maxLength * 2 + 1;
    }



    static public IQueryable<Formula> GetAllNonCanonicalFormulas(this IQueryable<ReductionRecord> db, int maxLength) =>
        db.GetAllNonCanonicalRecords().Where(_ => _.Length <= maxLength).ToFormulas();

    static public IQueryable<Formula> GetAllNonCanonicalFormulas(this IQueryable<ReductionRecord> db) =>
        db.GetAllCanonicalRecords().ToFormulas();

    static public IQueryable<Formula> GetAllCanonicalFormulas(this IQueryable<ReductionRecord> db) =>
        db.GetAllCanonicalRecords().ToFormulas();

    static public IQueryable<Formula> GetAllCanonicalRecordsInLexicalOrder(this IQueryable<ReductionRecord> db) =>
        db.GetAllCanonicalRecords().InFormulaOrder().ToFormulas();
    static public IQueryable<ReductionRecord> GetCanonicalRecordByTruthTable(this IQueryable<ReductionRecord> db, string truthTable) =>
        db.InFormulaOrder().Where(_ => _.TruthValue == truthTable).Take(1);

    static public int CountCanonicalFormulas(this IQueryable<ReductionRecord> db) =>
        db.GetAllCanonicalRecords().Count();


}