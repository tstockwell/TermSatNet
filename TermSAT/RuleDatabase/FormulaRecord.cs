using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using TermSAT.Formulas;

namespace TermSAT.RuleDatabase;

public partial class FormulaRecord
{
    [Required]
    public Decimal Id { get; set; }

    /// <summary>
    /// The formula as text
    /// </summary>
    [Required]
    public string Text { get; set; }

    /// <summary>
    /// The length of this formula.
    /// Not the same of the length of the Text column.
    /// </summary>
    [Required]
    public int Length { get; set; }

    /// <summary>
    /// The number of the highest variable in this formula.
    /// Is used as part of the formula ordering.
    /// </summary>
    [Required]
    public int VarOrder { get; set; }

    /// <summary>
    /// Used to validate that when the nand-reduction algorithm cannot reduce a formula that the formula is canonical.  
    /// Only used for nand-reduction algorithm complexity proof, not needed in a production system.  
    /// </summary>
    public string TruthValue { get; set; }

    /// <summary>
    /// Indicates that this formula has been indexed.
    /// -1 = no value, 0 = false, 1 = true
    /// Only used for nand-reduction algorithm complexity proof, not needed in a production system.  
    /// </summary>
    public int IsIndexed { get; set; } = -1;

    /// <summary>
    /// Only used for nand-reduction algorithm complexity proof, not needed in a production system.  
    /// </summary>
    public string IsSubsumed { get; set; } = null;

    /// <summary>
    /// Indicates that this formula is canonical.
    /// -1 = no value, 0 = false, 1 = true
    /// Note: if == 0 then eventually IsIndexed should be 1
    /// Only used for nand-reduction algorithm complexity proof, not needed in a production system.  
    /// </summary>
    public int IsCanonical { get; set; } = -1;



    [NotMapped]
    public Formula Formula 
    { 
        get
        {
            if (_formula == null && !string.IsNullOrWhiteSpace(Text))
            {
                _formula = Formula.GetOrParse(Text);
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
        VarOrder = varCount;
        TruthValue = truthValue;
        _formula = formula;
    }
    private FormulaRecord()
    {
    }

    public static void OnModelCreating(ModelBuilder modelBuilder, string tableName)
    {
        modelBuilder.Entity<FormulaRecord>(f => f.ToTable(tableName));

        modelBuilder.Entity<FormulaRecord>().Ignore(_ => _.Formula);

        modelBuilder.Entity<FormulaRecord>().Property(f => f.Id).IsRequired().ValueGeneratedOnAdd(); 
        modelBuilder.Entity<FormulaRecord>().Property(f => f.Text).IsRequired();
        modelBuilder.Entity<FormulaRecord>().Property(f => f.VarOrder).IsRequired();
        modelBuilder.Entity<FormulaRecord>().Property(f => f.Length).IsRequired();
        //modelBuilder.Entity<FormulaRecord>().Property(f => f.TruthValue).IsRequired();
        //modelBuilder.Entity<FormulaRecord>().Property(f => f.Subsumed).HasDefaultValue(-1);
        //modelBuilder.Entity<FormulaRecord>().Property(f => f.Evaluated).HasDefaultValue(-1);
        //modelBuilder.Entity<FormulaRecord>().Property(f => f.Indexed).HasDefaultValue(-1);
        //modelBuilder.Entity<FormulaRecord>().Property(f => f.Closed).HasDefaultValue(-1);

        modelBuilder.Entity<FormulaRecord>().HasKey(f => f.Id);

        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.VarOrder);
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.Length);
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.Text).IsUnique();
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.TruthValue);
        modelBuilder.Entity<FormulaRecord>().HasIndex(_ => new { _.VarOrder, _.Length, _.Text }); // formula order
        modelBuilder.Entity<FormulaRecord>().HasIndex(_ => new { _.TruthValue, _.VarOrder, _.Length, _.Text }); 

        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.IsIndexed);
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.IsCanonical);
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.IsSubsumed);
    }

}
