using Microsoft.EntityFrameworkCore;
using TermSAT.Formulas;
using TermSAT.NandReduction;

namespace TermSAT.RuleDatabase;

/// <summary>
/// The database for the Lucid system.
/// </summary>
public class LucidDbContext : DbContext
{
    public static LucidDbContext GetDatabaseContext(string datasource)
    {
        var connectionString = "DataSource=" + datasource;
        var options = new DbContextOptionsBuilder()
            .UseSqlite(connectionString)
            .Options;

        return new LucidDbContext(options);
    }

    public LucidDbContext(DbContextOptions options) : base(options)
    {
    }


    /// <summary>
    /// A table of all the expressions the system has encountered by system 
    /// that were not immediately reducible by the rules in the Lookup table.  
    /// Some of these 'mostly canonical' rules will be subsumed by new rules can be cleaned up later.
    /// </summary>
    public DbSet<ReductionRecord> Expressions { get; set; }
    public DbSet<CofactorRecord> Cofactors { get; set; }

    /// <summary>
    /// A trie-like table that indexes all known expressions and makes it possible 
    /// to efficiently apply all known rewrite rules to a given expression.  
    /// </summary>
    public DbSet<FormulaIndex.Node> Lookup { get; set; }

    /// <summary>
    /// For saving configuration values and such.
    /// </summary>
    public DbSet<MetaRecord> Meta { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // doesnt seem to work, ids are not set before before SaveChanges
        //modelBuilder.Entity<ReductionRecord>()
        //        .Property(x => x.Id)
        //        .UseHiLo($"Sequence-{nameof(ReductionRecord)}");
        //modelBuilder.UseHiLo();

        ReductionRecord.OnModelCreating(modelBuilder, nameof(Expressions));
        MetaRecord.OnModelCreating(modelBuilder, nameof(Meta));
        FormulaIndex.OnModelCreating(modelBuilder, nameof(Lookup));
        CofactorRecord.OnModelCreating(modelBuilder, nameof(Cofactors));
    }

}
