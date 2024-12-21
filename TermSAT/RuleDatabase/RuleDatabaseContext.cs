using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TermSAT.RuleDatabase;

/// <summary>
/// The rule database is an enumeration of all formulas with [VARIABLE_COUNT] 
/// variables, up to a length determined by the RuleGenerator.
/// The database is used to create a set of rewrite rules.
/// 
/// The rule database is a single table named FORMULA with the following columns...
///  		FORMULA 	- a textual representation of the formula
///  		LENGTH 		- the length of the textual representation
///  		TRUTHVALUE 	- the truth value
///  		CANONICAL 	- indicates that the formula is one of the shortest formulas 
///  					  with the associated truth value
///  		ID 			- a number assigned to the formula  
///  		
/// 
/// </summary>
public class RuleDatabaseContext : DbContext
{
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
        modelBuilder.Entity<FormulaRecord>().HasIndex(_ => new { _.Length, _.Text, _.TruthValue });
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.TruthValue);
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.IsCanonical);
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.IsCompleted);
        modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.IsSubsumedByScheme);

        modelBuilder.Entity<FormulaRecord>().Ignore(_ => _.Formula);

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
        public static RuleDatabaseContext GetDatabaseContext(string datasource)
        {
            var connectionString = "DataSource=" + datasource;
            var options = new DbContextOptionsBuilder()
                .UseSqlite(connectionString)
                .Options;

            return new RuleDatabaseContext(options);
        }

    //public void DeleteAll()
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
