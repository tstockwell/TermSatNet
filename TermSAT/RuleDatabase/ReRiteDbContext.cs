using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.NandReduction;

namespace TermSAT.RuleDatabase;

/// <summary>
/// The database for the ReRite system.
/// </summary>
public class ReRiteDbContext : DbContext
{
    public static ReRiteDbContext GetDatabaseContext(string datasource)
    {
        var connectionString = "DataSource=" + datasource;
        var options = new DbContextOptionsBuilder()
            .UseSqlite(connectionString)
            .Options;

        return new ReRiteDbContext(options);
    }

    public ReRiteDbContext(DbContextOptions options) : base(options)
    {
    }


    public DbSet<ReductionRecord> Formulas { get; set; }
    public DbSet<MaterialTermRecord> MaterialTerms { get; set; }
    public DbSet<FormulaIndex.Node> Lookup { get; set; }
    public DbSet<MetaRecord> Meta { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // doesnt seem to work, ids are not set before before SaveChanges
        //modelBuilder.Entity<ReductionRecord>()
        //        .Property(x => x.Id)
        //        .UseHiLo($"Sequence-{nameof(ReductionRecord)}");
        //modelBuilder.UseHiLo();

        ReductionRecord.OnModelCreating(modelBuilder, nameof(TermSAT.Formulas));
        MetaRecord.OnModelCreating(modelBuilder, nameof(Meta));
        FormulaIndex.OnModelCreating(modelBuilder, nameof(Lookup));
    }

    ReductionRecord _true;
    ReductionRecord _false;

    public async Task<ReductionRecord> GetConstant(bool value)
    {
        if (value)
        {
            if (_true == null)
            {
                _true = await this.Formulas.Where(_ => _.Text == "T").FirstAsync();
            }
            return _true;
        }
        if (_false == null)
        {
            _true = await this.Formulas.Where(_ => _.Text == "F").FirstAsync();
        }
        return _false;
    }
}
