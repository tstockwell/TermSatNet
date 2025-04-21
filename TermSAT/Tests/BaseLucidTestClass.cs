using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.Tests
{
    [TestClass]
    public class BaseLucidTestClass
    {

        public LucidDbContext Lucid { get; set; }

        [TestInitialize]
        public async Task InitializeTest()
        {
            var options =
                new DbContextOptionsBuilder()
                .UseNpgsql($"Server=localhost;Database=rrtestdb;Port=5432;User Id=postgres;Password=password;Pooling=true;Include Error Detail=True")
                .EnableDetailedErrors() // doesn't seem to do anything
                .EnableSensitiveDataLogging() // doesn't seem to do anything
                .EnableThreadSafetyChecks()
                //.LogTo(msg => Trace.WriteLine(msg), new[] { DbLoggerCategory.Database.Command.Name })
                //.UseSqlite("Data Source=file:rules?mode=memory&cache=shared;Pooling=False;")
                //.UseSqlite("Data Source=file:rules?mode=memory&cache=shared")
                .Options;
            Lucid = new LucidDbContext(options);

            {
#if DEBUG
                try
                {
                    await Lucid.Database.EnsureDeletedAsync();
                }
                catch { }
#endif
            }

            if (!(await Lucid.Database.EnsureCreatedAsync()))
            {
                throw new TermSatException("!ruleDb.Database.EnsureCreatedAsync()");
            }

            {
                await Lucid.BootstrapAsync();


                //await foreach (var nonCanonical in ruleDb.FormulaRecords.AsNoTracking().GetAllNonCanonicalRecords().AsAsyncEnumerable())
                //{
                //    await ruleDb.AddGeneralizationAsync(nonCanonical);
                //}
                await Lucid.SaveChangesAsync();
            }


        }

        [TestCleanup]
        public void CleanupTest()
        {
            Lucid.Dispose();
        }

    }
}
