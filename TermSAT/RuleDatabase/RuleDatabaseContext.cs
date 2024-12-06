/*******************************************************************************
 *     termsat SAT solver
 *     Copyright (C) 2010 Ted Stockwell <emorning@yahoo.com>
 * 
 *     This program is free software: you can redistribute it and/or modify
 *     it under the terms of the GNU Affero General Public License as
 *     published by the Free Software Foundation, either version 3 of the
 *     License, or (at your option) any later version.
 * 
 *     This program is distributed in the hope that it will be useful,
 *     but WITHOUT ANY WARRANTY; without even the implied warranty of
 *     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *     GNU Affero General Public License for more details.
 * 
 *     You should have received a copy of the GNU Affero General Public License
 *     along with this program.  If not, see <http://www.gnu.org/licenses/>.
 ******************************************************************************/
using Microsoft.EntityFrameworkCore;
using System.Linq;
using TermSAT.Formulas;

namespace TermSAT.RuleDatabase;

public class RuleDatabaseContext : DbContext
{
    //
    // Summary:
    //     Initializes a new instance of the Microsoft.EntityFrameworkCore.DbContext class
    //     using the specified options. The Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)
    //     method will still be called to allow further configuration of the options.
    //
    // Parameters:
    //   options:
    //     The options for this context.
    public RuleDatabaseContext(DbContextOptions options) : base(options)
    {
    }
    public DbSet<FormulaRecord> FormulaRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FormulaRecord>().Property(f => f.Id).IsRequired();
        modelBuilder.Entity<FormulaRecord>().Property(f => f.Text).IsRequired();
        modelBuilder.Entity<FormulaRecord>().Property(f => f.VarCount).IsRequired();
        modelBuilder.Entity<FormulaRecord>().Property(f => f.Length).IsRequired();
        modelBuilder.Entity<FormulaRecord>().Property(f => f.TruthValue).IsRequired();
        modelBuilder.Entity<FormulaRecord>().Property(f => f.IsCanonical).HasDefaultValue(false);
        modelBuilder.Entity<FormulaRecord>().Property(f => f.IsCompleted).HasDefaultValue(false);
        modelBuilder.Entity<FormulaRecord>().Property(f => f.IsSubsumedByScheme).HasDefaultValue(false);

        modelBuilder.Entity<FormulaRecord>().HasKey(f => f.Id);
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.Text);
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.VarCount);
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.Length);
        modelBuilder.Entity<FormulaRecord>().HasIndex(_ => new { _.Length, _.Text });
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.TruthValue);
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.IsCanonical);
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.IsCompleted);
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.IsSubsumedByScheme);

        modelBuilder.Entity<FormulaRecord>(f => f.ToTable("FormulaRecords"));
    }

    public override int SaveChanges()
    {
        var result = base.SaveChanges();
        return result;
    }

    /// <summary>
    /// I think that ef still tracks after an add/ssavechanges.
    /// So, periodically detach any entries.
    /// </summary>
    public void Clear() => this.ChangeTracker.Clear();

    //public void Clear()
    //{
    //    var changedEntriesCopy = this.ChangeTracker.Entries()
    //        .Where(e => e.State == EntityState.Added ||
    //                    e.State == EntityState.Modified ||
    //                    e.State == EntityState.Deleted)
    //        .ToList();

    //    foreach (var entry in changedEntriesCopy)
    //        entry.State = EntityState.Detached;
    //}
}

public static class RuleDatabaseContextExtensions
{

    /**
     * Finds the canonical form of for formulas with the given truth value.
     */
    static public FormulaRecord FindCanonicalRecord(this RuleDatabaseContext ctx, TruthTable truthTable)
    {
        var tv = truthTable.ToString();
        var recorrd = ctx.FormulaRecords
                .OrderBy(_ => _.Length).ThenBy(_ => _.Text)
                .Where(_ => _.TruthValue == tv)
                .FirstOrDefault();
#if DEBUG
        if (recorrd != null && !recorrd.IsCanonical)
        {
            throw new TermSatException($"The canonical form for a formula with truth value {tv} " +
                $"should be the first formula in the formula order with that truth value.");
        }
#endif 

        return recorrd;

    }

    static public Formula FindCanonicalFormula(this RuleDatabaseContext ctx, TruthTable truthTable) =>
        Formula.Parse(
            ctx.FormulaRecords
                .OrderBy(_ => _.Length).ThenBy(_ => _.Text)
                .Where(_ => _.TruthValue == truthTable.ToString())
                .FirstOrDefault().Text);

}