using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using TermSAT.Formulas;

namespace TermSAT.RuleDatabase;

public partial class FormulaRecord
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Text { get; set; }
    [Required]
    public int Length { get; set; }
    [Required]
    public int VarCount { get; set; }
    [Required]
    public string TruthValue { get; set; }

    //public int Subsumed { get; set; } = -1;
    //public int Evaluated {  get; set; } = -1;
    //public int Indexed { get; set; } = -1;
    //public int Closed { get; set; } = -1;


    [NotMapped]
    public Formula Formula 
    { 
        get
        {
            if (_formula == null && !string.IsNullOrWhiteSpace(Text))
            {
                _formula = Formula.Parse(Text);
            }
            return _formula;
        }
    }
    private Formula _formula {  get; set; }

    public FormulaRecord(int id, Formula formula, int varCount, string truthValue)
    {
        Id = id;
        Text = formula.ToString();
        Length = formula.Length;
        VarCount = varCount;
        TruthValue = truthValue;
        _formula = formula;
    }
    private FormulaRecord()
    {
    }



    /// <summary>
    /// Set to an internal scheme name: basic, ordering, distributive 
    /// </summary>
    public string IsSubsumedByScheme {  get; set; }
}


public partial class FormulaRecord
{
    public static void OnModelCreating(ModelBuilder modelBuilder, string tableName)
    {
        modelBuilder.Entity<FormulaRecord>(f => f.ToTable(tableName));

        modelBuilder.Entity<FormulaRecord>().Ignore(_ => _.Formula);

        modelBuilder.Entity<FormulaRecord>().Property(f => f.Id).IsRequired().ValueGeneratedOnAdd(); 
        modelBuilder.Entity<FormulaRecord>().Property(f => f.Text).IsRequired();
        modelBuilder.Entity<FormulaRecord>().Property(f => f.VarCount).IsRequired();
        modelBuilder.Entity<FormulaRecord>().Property(f => f.Length).IsRequired();
        //modelBuilder.Entity<FormulaRecord>().Property(f => f.TruthValue).IsRequired();
        //modelBuilder.Entity<FormulaRecord>().Property(f => f.Subsumed).HasDefaultValue(-1);
        //modelBuilder.Entity<FormulaRecord>().Property(f => f.Evaluated).HasDefaultValue(-1);
        //modelBuilder.Entity<FormulaRecord>().Property(f => f.Indexed).HasDefaultValue(-1);
        //modelBuilder.Entity<FormulaRecord>().Property(f => f.Closed).HasDefaultValue(-1);

        modelBuilder.Entity<FormulaRecord>().HasKey(f => f.Id);

        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.VarCount);
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.Length);
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.Text);
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.TruthValue);
        modelBuilder.Entity<FormulaRecord>().HasIndex(_ => new { _.VarCount, _.Length, _.Text }); // formula order
        modelBuilder.Entity<FormulaRecord>().HasIndex(_ => new { _.TruthValue, _.VarCount, _.Length, _.Text }); 

        //modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.Subsumed);
        //modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.Evaluated);
        //modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.Indexed);
        //modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.Closed);
        //modelBuilder.Entity<FormulaRecord>().HasIndex(_ => new { _.Subsumed, _.Evaluated, _.Indexed, _.Closed }); // build order
    }

}

public static class FormulaRecordExtensions
{
    public static IQueryable<FormulaRecord> InFormulaOrder(this IQueryable<FormulaRecord> dbset) 
        => dbset.OrderBy(_ => _.VarCount).ThenBy(_ => _.Length).ThenBy(_ => _.Text);
}