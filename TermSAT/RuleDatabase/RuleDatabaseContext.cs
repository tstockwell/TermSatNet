using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TermSAT.Formulas;

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

    public static RuleDatabaseContext GetDatabaseContext(string datasource)
    {
        var connectionString = "DataSource=" + datasource;
        var options = new DbContextOptionsBuilder()
            .UseSqlite(connectionString)
            .Options;

        return new RuleDatabaseContext(options);
    }

    public RuleDatabaseContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<FormulaRecord> FormulaRecords { get; set; }
    public DbSet<MetaRecord> Meta { get; set; }
    public DbSet<FormulaIndex.Node> Lookup { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        FormulaRecord.OnModelCreating(modelBuilder, nameof(FormulaRecords));
        MetaRecord.OnModelCreating(modelBuilder, nameof(Meta));
        FormulaIndex.OnModelCreating(modelBuilder, nameof(Lookup));
    }
}
