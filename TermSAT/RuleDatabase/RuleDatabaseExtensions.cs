using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using TermSAT.Formulas;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace TermSAT.RuleDatabase;

public static class RuleDatabaseExtensions
{


    static public async Task<FormulaRecord> FindByIdAsync(this RuleDatabaseContext ctx, int id) =>
        await ctx.FormulaRecords.AsNoTracking().Where(_ => _.Id == id).FirstAsync();




    static public void DeleteAll(this RuleDatabaseContext ctx) => 
        ctx.Database.ExecuteSqlRaw("DELETE FROM FormulaRecords");

    static public Formula GetLastGeneratedFormula(this RuleDatabaseContext ctx)
    {
        var record = ctx.FormulaRecords.AsNoTracking()
            .OrderByDescending(f => f.Id)
            .FirstOrDefault();

        var formula = record != null ? Formula.GetOrParse(record.Text) : null;
        return formula;
    }

    static public IQueryable<FormulaRecord> GetAllCanonicalRecords(this IQueryable<FormulaRecord> db) =>
        db.Select(_ => _.TruthValue).Distinct()
        .Select(t =>
            db.OrderBy(_ => _.VarCount)
            .ThenBy(_ => _.Length)
            .ThenBy(_ => _.Text)
            .Where(_ => _.TruthValue == t).First());

    static public IQueryable<FormulaRecord> GetCanonicalRecordByTruthTable(this IQueryable<FormulaRecord> db, string truthTable) =>
        db.InFormulaOrder().Where(_ => _.TruthValue == truthTable).Take(1);

    static public IQueryable<Formula> ToFormulas(this IQueryable<FormulaRecord>db) =>
        db.Select(_ => Formula.GetOrParse(_.Text));


    static public IQueryable<FormulaRecord> GetAllNonCanonicalRecordsByTruthValue(this IQueryable<FormulaRecord> db, string truthTable) =>
        db.InFormulaOrder().Where(_ => _.TruthValue == truthTable).Skip(1);

    static public IQueryable<string> GetAllTruthValues(this IQueryable<FormulaRecord> db) =>
        db.Select(_ => _.TruthValue).Distinct();

    static public IQueryable<FormulaRecord> GetAllNonCanonicalRecords(this IQueryable<FormulaRecord> db) => 
        db.GetAllTruthValues()
        .SelectMany(t => 
            db.OrderBy(_ => _.VarCount)
            .ThenBy(_ => _.Length)
            .ThenBy(_ => _.Text)
            .Where(_ => _.TruthValue == t)
            .Skip(1));

    static public Task<int> GetLengthOfLongestCanonicalFormulaAsync(this IQueryable<FormulaRecord> db) =>
        db.GetAllNonCanonicalRecords().OrderBy(_ => _.Length).Select(_ => _.Length).FirstAsync();


    /**
     * Formulas longer than this length are guaranteed to be reducible with rules,
     * generated from previous formulas. 
     * Therefore processing can stop when formulas get this long.
     */
    static public async Task<int> LengthOfLongestPossibleNonReducibleFormulaAsync(this IQueryable<FormulaRecord> db)
    {
        int maxLength = await db.GetLengthOfLongestCanonicalFormulaAsync();
        if (maxLength <= 0) // we don't know the length of longest formula yet
            return int.MaxValue;
        return maxLength * 2 + 1;
    }




    static public async Task IsSubsumedBySchemeAsync(this RuleDatabaseContext ctx, Formula formula, string value)
    {
        var record = await ctx.FormulaRecords
            .Where(f => f.Text.Equals(formula.ToString()))
            .FirstOrDefaultAsync();
        if (record == null)
        {
            throw new Exception($"formula not found in database:{formula}");
        }

        record.IsSubsumedByScheme = value;

        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
    }

    static public IQueryable<Formula> GetAllNonCanonicalFormulas(this IQueryable<FormulaRecord> db, int maxLength) => 
        db.GetAllNonCanonicalRecords().Where(_ => _.Length <= maxLength).ToFormulas();

    static public IQueryable<Formula> GetAllNonCanonicalFormulas(this IQueryable<FormulaRecord> db) => 
        db.GetAllCanonicalRecords().ToFormulas();

    static public IQueryable<Formula> GetAllCanonicalFormulas(this IQueryable<FormulaRecord> db) =>
        db.GetAllCanonicalRecords().ToFormulas();

    static public IQueryable<Formula> GetAllCanonicalRecordsInLexicalOrder(this IQueryable<FormulaRecord> db) => 
        db.GetAllCanonicalRecords().InFormulaOrder().ToFormulas();

    static public async Task<int> GetLengthOfCanonicalFormulasAsync(this DbSet<FormulaRecord> formulas, string truthTable) => 
        (await formulas.GetCanonicalRecordByTruthTable(truthTable).FirstAsync()).Length;


    static public async Task<int> CountNonCanonicalFormulas(this DbSet<FormulaRecord> records) => 
        await records.GetAllNonCanonicalRecords().CountAsync();

    static public int CountCanonicalFormulas(this IQueryable<FormulaRecord> db) =>
            db.GetAllCanonicalRecords().Count();


    static public long CountCanonicalTruthTables(this IQueryable<FormulaRecord> db) => 
            db.GetAllCanonicalRecords().Select(_ => _.TruthValue).Distinct().Count();

    static public IQueryable<FormulaRecord> GetAllByTruthTable(this IQueryable<FormulaRecord> db, string truthTable) => 
            db.Where(f => f.TruthValue == truthTable);


}