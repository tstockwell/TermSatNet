using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using TermSAT.Formulas;
using Microsoft.EntityFrameworkCore;

namespace TermSAT.RuleDatabase;

public static class RuleDatabaseExtensions
{

    /**
     * Finds the canonical form of for formulas with the given truth value.
     */
    static public FormulaRecord FindCanonicalByTruthValue(this RuleDatabaseContext ctx, string truthValue)
    {
        var recorrd = ctx.FormulaRecords
                .OrderBy(_ => _.Length).ThenBy(_ => _.Text)
                .Where(_ => _.TruthValue == truthValue)
                .FirstOrDefault();
#if DEBUG
        if (recorrd != null && !recorrd.IsCanonical)
        {
            throw new TermSatException($"The canonical form for a formula with truth value {truthValue} " +
                $"should be the first formula in the formula order with that truth value.");
        }
#endif 

        return recorrd;

    }

    static public Formula FindCanonicalFormula(this RuleDatabaseContext ctx, string truthValue) =>
        Formula.Parse(FindCanonicalByTruthValue(ctx, truthValue).Text);

    static public FormulaRecord FindById(this RuleDatabaseContext ctx, int id) =>
        ctx.FormulaRecords.AsNoTracking().Where(_ => _.Id == id).First();




    static public void DeleteAll(this RuleDatabaseContext ctx) => 
        ctx.Database.ExecuteSqlRaw("DELETE FROM FormulaRecords");

    static public Formula GetLastGeneratedFormula(this RuleDatabaseContext ctx)
    {
        var record = ctx.FormulaRecords.AsNoTracking()
            .OrderByDescending(f => f.Id)
            .FirstOrDefault();

        var formula = record != null ? Formula.Parse(record.Text) : null;
        return formula;
    }

    static public List<Formula> GetCanonicalFormulas(this RuleDatabaseContext ctx, TruthTable truthTable) =>  
        ctx.FormulaRecords.AsNoTracking()
            .Where(f => f.TruthValue == truthTable.ToString() && f.IsCanonical == true)
            .OrderBy(f => f.Id)
            .Select(_ => Formula.Parse(_.Text))
            .ToList();

    static public List<FormulaRecord> GetAllFormulaRecords(this RuleDatabaseContext ctx) =>  
        ctx.FormulaRecords.AsNoTracking()
            .OrderBy(f => f.Id)
            .ToList();

    static public List<Formula> GetNonCanonicalFormulas(this RuleDatabaseContext ctx, TruthTable truthTable) =>  
        ctx.FormulaRecords.AsNoTracking()
            .Where(f => f.TruthValue == truthTable.ToString() && f.IsCanonical == false)
            .OrderBy(f => f.Id)
            .Select(_ => Formula.Parse(_.Text))
            .ToList();


    static public int GetLengthOfLongestCanonicalFormula(this RuleDatabaseContext ctx)
    { 
        var formula = ctx.FormulaRecords.AsNoTracking()
                .Where(f => f.IsCanonical == true)
                .OrderByDescending(f => f.Length)
                .FirstOrDefault();

        if (formula == null)
            return 0;

        return formula.Length;
    }

    /**
     * Formulas longer than this length are guaranteed to be reducible with rules,
     * generated from previous formulas. 
     * Therefore processing can stop when formulas get this long.
     */
    static public int LengthOfLongestPossibleNonReducibleFormula(this RuleDatabaseContext ctx)
    {
        int maxLength = ctx.GetLengthOfLongestCanonicalFormula();
        if (maxLength <= 0) // we don't know the length of longest formula yet
            return int.MaxValue;
        return maxLength * 2 + 1;
    }


    static public List<Formula> FindCanonicalFormulasByLength(this RuleDatabaseContext ctx, int size)
    {
            var records = ctx.FormulaRecords.AsNoTracking()
                .Where(f => f.Length == size && f.IsCanonical == true)
                .OrderBy(f => f.Id)
                .ToList();
            var formulas = records.Select(r => r.Text.ToFormula()).ToList();
            return formulas;
    }

    /// <summary>
    /// Note, using 0 for id (a field in the primary key) causes the Sqlite driver to use the Sqlite auto increment value
    /// </summary>
    static public void AddFormula(this RuleDatabaseContext ctx, Formula formula, bool isCanonical) => 
        ctx.AddFormula(0, formula, isCanonical);

    static public void AddFormula(this RuleDatabaseContext ctx, int id, Formula formula, bool isCanonical)
    {
        var record = new FormulaRecord(id, formula, isCanonical);

        ctx.FormulaRecords.Add(record);
        ctx.SaveChanges();
        ctx.Clear();
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
        ctx.Clear();
    }

    static public List<Formula> GetAllNonCanonicalFormulas(this RuleDatabaseContext ctx, int maxLength)
    {
        var records = ctx.FormulaRecords.AsNoTracking()
            .Where(f => f.Length <= maxLength && f.IsCanonical == false)
            .OrderBy(f => f.Id)
            .ToList();
        var formulas = records.Select(r => r.Text.ToFormula()).ToList();
        return formulas;
    }
    static public List<Formula> GetAllNonCanonicalFormulas(this RuleDatabaseContext ctx)
    {
        var records = ctx.FormulaRecords.AsNoTracking()
            .Where(f => f.IsCanonical == false)
            .OrderBy(f => f.Id)
            .ToList();
        var formulas = records.Select(r => r.Text.ToFormula()).ToList();
        return formulas;
    }

    static public List<Formula> GetAllCanonicalFormulas(this RuleDatabaseContext ctx)
    {
        var records = ctx.FormulaRecords.AsNoTracking()
            .Where(f => f.IsCanonical == true)
            .OrderBy(f => f.Id)
            .ToList();
        var formulas = records.Select(r => r.Text.ToFormula()).ToList();
        return formulas;
    }
    static public List<Formula> GetAllCanonicalFormulasInLexicalOrder(this RuleDatabaseContext ctx)
    {
            var records = ctx.FormulaRecords.AsNoTracking()
                .Where(f => f.IsCanonical == true)
                .OrderBy(f => f.Text)
                .ToList();
            var formulas = records.Select(r => r.Text.ToFormula()).ToList();
            return formulas;
    }
    static public List<TruthTable> GetAllTruthTables(this RuleDatabaseContext ctx)
    {
            var truthValues = ctx.FormulaRecords.AsNoTracking()
                .OrderBy(f => f.TruthValue)
                .Select(f => f.TruthValue)
                .Distinct()
                .ToList();
            var truthTables = truthValues.Select(v => TruthTable.GetTruthTable(v)).ToList();
            return truthTables;
    }

    static public int GetLengthOfCanonicalFormulas(this RuleDatabaseContext ctx, TruthTable truthTable)
    {
            var formula = ctx.FormulaRecords.AsNoTracking()
                .Where(f => f.IsCanonical == true && f.TruthValue == truthTable.ToString())
                .OrderBy(f => f.Id)
                .FirstOrDefault();

            if (formula == null)
                return 0;

            return formula.Length;
    }

    /**
     * Finds the canonical form of the given formula.
     */
    static public Formula FindCanonicalFormula(this RuleDatabaseContext ctx, Formula formula) => 
        ctx.FindCanonicalFormula(TruthTable.GetTruthTable(formula).ToString());

    static public int CountNonCanonicalFormulas(this RuleDatabaseContext ctx)
    {
            var count = ctx.FormulaRecords.AsNoTracking()
                .Where(f => f.IsCanonical == false)
                .Count();
            return count;
    }

    static public int CountCanonicalFormulas(this RuleDatabaseContext ctx)
    {
            var count = ctx.FormulaRecords.AsNoTracking()
                .Where(f => f.IsCanonical == true)
                .Count();
            return count;
    }


    static public long CountCanonicalTruthTables(this RuleDatabaseContext ctx)
    {
            var count = ctx.FormulaRecords.AsNoTracking()
                .Where(f => f.IsCanonical == true)
                .Select(f => f.TruthValue)
                .Distinct()
                .Count();
            return count;
    }

    static public List<Formula> GetAllFormulas(this RuleDatabaseContext ctx, TruthTable truthTable)
    {
            var records = ctx.FormulaRecords.AsNoTracking()
                .Where(f => f.TruthValue == truthTable.ToString())
                .ToList();
            var formulas = records.Select(r => r.Text.ToFormula()).ToList();
            return formulas;
    }


}