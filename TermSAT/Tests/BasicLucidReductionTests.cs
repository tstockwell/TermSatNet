using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.NandReduction;
using TermSAT.RuleDatabase;

namespace TermSAT.Tests
{
    [TestClass]
    public class NandSchemeReductionTests
    {

        LucidDbContext Lucid { get; set; }

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

        public async Task<ReductionRecord> GetMostlyCanonicalRecordAsync(Formula startingFormula)
        {
            var rr = await Lucid.GetMostlyCanonicalRecordAsync(startingFormula);
            Assert.AreEqual(startingFormula.Text, rr.Text);
            return rr;
        }


        [TestMethod]
        public async Task CurrentNandReductionTests()
        {
            // |||T.1|.2.3||.1.2|.1.3 => |T||.1.2|.1.3
            //      test .1->F in antecedent
            //      => |||TF|.2.3||.1.2|.1.3
            //      => ||T|.2.3||.1.2|.1.3
            //          test .2->T in antecedent
            //          => ||T|T.3||.1.2|.1.3
            //          => |.3||.1.2|.1.3
            //          => |.3||.1.2|.1T .3->T in subsequent
            //          => |.3||F.2|.1T .1->F in antecedent 
            //          => |.3|T|.1T .2 is wildcard
            //          => |.3.1 
            //          => |.1.3
            //      => ||T|.2.3||.1F|.1.3   .2 ->F in subsequent
            //      => ||T|.2.3|T|.1.3      .1 is wildcard
            //      => ||T|.1.3|T|.2.3      
            //      => |.1|T|.2.3
            // => |||T.1|.2.3||T.2|.1.3     !!!!  .1->T in subsequent is not a valid reduction WTF !!!!!!
            // BUT... start by testing the subsequent first and it works, WTF!!!
            // NOTE 2/1/25... dumbass, it doesn't work because this line... ```=> |.3||F.2|.1T .1->F in antecedent``` 
            //      should have caused the proof to stop there, because .1 should have been blacklisted.
            // |||T.1|.2.3||.1.2|.1.3 => |T||.1.2|.1.3
            //      test .1->F in subsequent
            //      => |||T.1|.2.3||F.2|F.3 
            //      => |||T.1|.2.3||F.2T
            //      => |||T.1|.2.3|TT
            //      => |||T.1|.2.3F
            //      => T .1 is wildcard
            // => |||TT|.2.3||.1.2|.1.3 .1->T in antecedent
            // => ||F|.2.3||.1.2|.1.3 
            // => |T||.1.2|.1.3 
            // Answer: replacing just uno term instance during wildcard substitution is not logically correct.
            // It works when going the other direction because there's only uno matching term instance.  
            // Wow, I sure did waste a gd lot of time implementing and testing reduction mapping for nothing.
            // Note 1/15/25: Nah dog, turns out that reduction mapping is very useful and required
            // Note 2/1/25: Nope, wrong again dipshit.
            //  This test is just demonstrating that ALL the terms in a formula must be made irrelevant,
            //  not just somehow removed from the formula, but made irrelevant.  
            //  So, if we keep reducing a formula until all instances of the test term are made irrelevant
            //  and nuno are rearranged by a transform, then there's no need to keep track of which uno are reduced.  
            //  Tracking 'compelling terms' makes tracking Mapping unnecessary,  
            //  and this test is just demonstrating that Mapping doesnt always work.  
            //  
            {
                {

                }
                {
                    var reductionSteps = new[] 
                    {
                        "|||TF|.2.3||.1.2|.1.3",
                        "||T|.2.3||.1.2|.1.3",
                        "||T|.2.3||.1F|.1.3",
                        "||T|.2.3|T|.1.3",
                        "|.1|T|.2.3"
                    };
                    var reductionFormulas = reductionSteps
                        .Select(async s => await Lucid.GetMostlyCanonicalRecordAsync(s))
                        .ToArray();
                    await Lucid.SaveChangesAsync();
                    Task.WaitAll(reductionFormulas);
                    var truthTables = reductionFormulas.Select(f => TruthTable.GetTruthTable(f.Result.Formula).ToString()).Distinct().ToArray();
                    Assert.IsTrue(truthTables.Length == 1);
                }
                {
                    var reductionSteps = new[]
                    {
                        "|||T.1|.2.3||.1.2|.1.3",
                        "|||TT|.2.3||.1.2|.1.3",
                    };
                    var reductionFormulas = reductionSteps
                        .Select(async s => await Lucid.GetMostlyCanonicalRecordAsync(s))
                        .ToArray();
                    Task.WaitAll(reductionFormulas);
                    var truthTables = reductionFormulas.Select(f => TruthTable.GetTruthTable(f.Result.Formula).ToString()).Distinct().ToArray();
                    Assert.IsTrue(truthTables.Length == 1);
                }

                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|||T.1|.2.3||.1.2|.1.3");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|T||.1.2|.1.3");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }

            // |T||.1|T.2|.3|T.1 => ||.1.2||T.1|T.3
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|T||.1|T.2|.3|T.1");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("||.1.2||T.1|T.3");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }

        }
        [TestMethod]
        public async Task BasicNandReductionTests()
        {

            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|TT");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("F");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|.1T");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|T.1");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|.2.1");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|.1.2");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|T|T.1");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync(".1");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }
            await SimplestDeiterationTake1();
            await SimplestDeiterationTake2();
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|.2|.1T");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|.2|T.1");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }
            await SlightlyDeepReorderingReduction();
            await SlightlyDeepWildcardReduction();

            await SimpleWildcardSwappingExample();

            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("||.1|T.2|.2|.1T");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("||.1|T.2|.2|T.1");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("||T.2||T.3|.1F");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|.3|T.2");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }
            // ||.2.3||T.3|.1.2, is canonical
            // proof is that, for the common subterms .2 and .3, no wildcards exist.
            // test .2ant -> T
            //  => ||T.3||T.3|.1.2
            //  => ||T.3||TF|.1.2
            //  => ||T.3|T|.1.2 canonical
            // test .2ant -> F
            //  => ||F.3||T.3|.1.2
            //  => |T||T.3|.1.2 canonical
            // test .2seq -> T
            //  => ||.2.3||T.3|.1T
            //  => ||.2.3||T.1|T.3 canonical
            // test .2seq -> F
            //  => ||.2.3||T.3|.1F
            //  => ||.2.3||T.3T
            //  => ||.2.3.3
            //  => |.3|.2.3
            //  => |.3|T.2 canonical
            // test .3ant -> T
            //  => ||.2T||T.3|.1.2
            //  => ||.2T||T.3|.1F
            //  => ||.2T||T.3T
            //  => ||.2T.3
            //  => |.3|.2T canonical
            // test .3ant -> F
            //  => ||.2F||T.3|.1.2
            //  => |T||T.3|.1.2 canonical
            // test .3seq -> T
            //  => ||.2.3||TT|.1.2
            //  => ||.2.3|F|.1.2
            //  => ||.2.3T
            //  => |T|.2.3 canonical
            // test .3seq -> F
            //  => ||.2.3||F.3|.1.2
            //  => ||.2.3|T|.1.2
            //  => |.1|T|.2|T.3 canonical
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("||.2.3||T.3|.1.2");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("||.2.3||T.3|.1.2");
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }



            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|.1||.2.3||T.3|.1.2");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|.1||.2.3||T.2|T.3");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }

            await SlightlyDeeperWildcardReduction();

            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|.2|T|.1.3");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|.1|T|.2.3");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }

            await ComputedCofactorDeiteration();

            // |||T.2|T.3|.3|T||T.1|T.2
            {
                //var nonCanonicalFormula = (Expressions.Nand)StartingReRite.GetMostlyCanonicalRecordAsync("|||T.2|T.3|.3|T||T.1|T.2");
                //var nonCanonicalFormula = (Expressions.Nand)StartingReRite.GetMostlyCanonicalRecordAsync("|||T.2|T.3|T|T||T.1|T.2");
                //var nonCanonicalFormula = (Expressions.Nand)StartingReRite.GetMostlyCanonicalRecordAsync("|||T.2|T.3||T.1|T.2");
                //var nonCanonicalFormula = (Expressions.Nand)StartingReRite.GetMostlyCanonicalRecordAsync("|||T.2|T.3||T.1|T.2");
                //var nonCanonicalFormula = (Expressions.Nand)StartingReRite.GetMostlyCanonicalRecordAsync("|T||T.2||T.3|T.1");
                var nonCanonicalFormula = await Lucid.GetMostlyCanonicalRecordAsync("|T||.1.3|T.2");
                var nonCanonicalTT = TruthTable.GetTruthTable(nonCanonicalFormula.Formula).ToString();
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|T||T.2|.1.3");
                var canonicalTT = TruthTable.GetTruthTable(canonicalRecord.Formula).ToString();
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalFormula);
                Assert.AreEqual(nonCanonicalTT, canonicalTT);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }
            {
                // SELECT f.*, c.Text FROM FormulaRecords f
                // join(SELECT * FROM FormulaRecords WHERE CreateCompletionMarker = 1) c on c.TruthValue = f.TruthValue
                // Where f.Id = 5478 yields...
                // 5478    |||.1.2|.1.3|.3|T||T.1|T.2  19  EAEA    0    94  |T||.1.2|.1.3
                // Note: you cant tell from the above, but IsSubsumedBySchema == '', and it shouldn't be blank
                // Note: weirdly, |T||T.1|T.2 is canonical
                // |||.1.2|.1.3|.3|T||T.1|T.2
                //      test .1 => F in antecedent
                //      => |||F.2|F.3|.3|T||T.1|T.2
                //      => ||TT|.3|T||T.1|T.2
                //      => |F|.3|T||T.1|T.2 ;.1 is a wildcard
                //      => T
                // yields result that is independent of .1, therefore set .1 => T in subsequent...
                // => |||.1.2|.1.3|.3|T||TT|T.2
                // => |||.1.2|.1.3|.3|T|F|T.2
                // => |||.1.2|.1.3|.3|TT
                //                            => |||.1.2|.1.3|.3F
                //                            => |||.1.2|.1.3T
                //                            => |T||.1.2|.1.3
                //
                // error: |||.1.2|T.3|.3|T||T.1|T.2 is not a valid reduction for |||.1.2|.1.3|.3|T||T.1|T.2
                // test .1->F in subsequent
                // => |||.1.2|.1.3|.3|T||TF|T.2
                // => |||.1.2|.1.3|.3|T|T|T.2
                // => |||.1.2|.1.3|.3|T.2
                //      test => |||.1.2|.1.3|.3|T.2 ;wildcard .2->F 

                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|||.1.2|.1.3|.3|T||T.1|T.2");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|T||.1.2|.1.3");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }

            // 315	||.1.2|.3|.1|T.2	11	1B1B	0		167	||.1.2|.3|T.1
            // setting .2 => T in antecedent => ||.1.T|.3|.1|T.2
            //                             => ||.1.T|.3|F|T.2
            //                             => ||.1.T|.3T
            // yields result that is independent of .2, therefore set .2 => F in subsequent...
            // ||.1.2|.3|.1|T.2 => ||.1.2|.3|.1|TF
            //                  => ||.1.2|.3|.1T
            //                  => ||.1.2|.3|T.1
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("||.1.2|.3|.1|T.2");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("||.1.2|.3|T.1");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }

            // ||.1|.2.3|.2.3
            // ||.2.3|.1|.2.3
            //  => ||.2.3|T.1, since  |T||.1.2|.2 => |T||T.1|.2
            //  => ||T.1|.2.3
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("||.2.3|.1|.2.3");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("||T.1|.2.3");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }

            // |T||.1|.2.3|.2.3
            //  => |T||T.1|.2.3, since  |T||.1.2|.2 => |T||T.1|.2
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|T||.1|.2.3|.2.3");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|T||T.1|.2.3");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }

            // ||.1.2|.3|T.2 is not a valid reduction for ||.1.2|.3|.1.2
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("||.1.2|.3|.1.2");
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreNotEqual(reducedRecord, await Lucid.GetMostlyCanonicalRecordAsync("||.1.2|.3|T.2"));
            }
            // error: ||.1.2|.2.3 is not a valid reduction for ||.1.2|T||.1|.2.3|.2.3
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("||.1.2|T||.1|.2.3|.2.3");
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreNotEqual(reducedRecord, await Lucid.GetMostlyCanonicalRecordAsync("||.1.2|.2.3"));
            }

            // error...  |||T.1|T.3||T.2|.1.3 is not a valid reduction for |||T.1|T.3||.1.3|.2.3
            // The issue is that the proof listener errunoously identifies the last instance of .3 as a wildcard.
            //  |||T.1|T.3||.1.3|.2.3
            //      => |||T.1|TF||.1.3|.2.3, .3 -> F in antecedent
            //      => |||T.1T||.1.3|.2.3
            //      => |.1||.1.3|.2.3
            //      => |.1||T.3|.2.3
            //      => |.1||T.3|.2F
            //          <<< the following step identifies .2 as a wildcard,
            //          <<< the proof listener errunoously identifies this step as a reduction target for .3
            //      => |.1||T.3T
            //      => |.1.3
            // Another issue is that |||T.1|T.3||.1.3|.2.3 should be reducible via 'normal' nand reduction....
            //  test .1ant -> T
            //  => |||TT|T.3||.1.3|.2.3
            //  => ||F|T.3||.1.3|.2.3 wildcard @ position 8
            //  therefore
            //  => |||T.1|T.3||F.3|.2.3 
            //  => |||T.1|T.3|T|.2.3 
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|||T.1|T.3||.1.3|.2.3");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|T||.1.3|.2.3");
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreNotEqual(reducedRecord, await Lucid.GetMostlyCanonicalRecordAsync("|||T.1|T.3||T.2|.1.3"));
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }

            // error... |T|.1.3 is not a valid reduction for ||.1.3||.2.3||.1.2|.1.3
            // ||.1.3||.2.3||.1.2|.1.3 should be reducible via 'normal' nand reduction....
            //  .1 -> T in antecedent yields...
            //      => ||T.3||.2.3||.1.2|.1.3
            //      => ||T.3||.2F||.1.2|.1F
            //      => ||T.3||.2F||.1.2T, and therefore .1 is a wildcard, targetPosition == 12 (the .1 in |.13)
            //      if we dont stop the proof at this point...
            //      => ||T.3|T||.1.2T
            //  therefore .1 -> F in subsequent
            //      => ||.1.3||.2.3||.1.2|F.3
            //      => ||.1.3||.2.3|T|.1.2
            //      => ||.1.3|.1|T||.2.3.2, since |.2|T|.1.3 => |.1|T|.2.3 
            //      => ||.1.3|.1|T|.2|.2.3
            //      => ||.1.3|.1|T|.2|T.3
            //      => |T|.1||.2|T.3|T.3 , since ||.1.2|.1|T.3 => |T|.1|.3|T.2
            //      => |T|.1||.2T|T.3 
            //      => |T|.1||T.2|T.3 
            //      => |T|T||.1.2|.1.3, since |a||Tb|Tc -> |T||ab|ac 
            //      => ||.1.2|.1.3
            //  ..or..
            //  .3 -> T in antecedent yields...
            //      => ||.1T||.2.3||.1.2|.1.3
            //      => ||.1T||.2.3||F.2|F.3
            //      => ||.1T||.2.3|TT
            //      => ||.1T||.2.3F
            //      => ||.1TT
            //      => .1
            //  therefore .3 -> F in subsequent
            //      => ||.1.3||.2F||.1.2|.1F
            //      => ||.1.3|T||.1.2T
            //      => ||.1.3|T|T|.1.2
            //      => ||.1.3|.1.2
            //      => ||.1.2|.1.3
            // also error... Debug.Fail failed with '||.1.3||.2.3||F.2|.1.3 is not a valid reduction for ||.1.3||.2.3||.1.2|.1.3
            //  This happens because the 'wildcard finder' decides that the .1 in |.1.2 is a wildcard,
            //  when in fact it should be the .1 in the last instance of |.1.3

            {
                var nonCanonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("||.1.3||.2.3||.1.2|.1.3");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("||.1.2|.1.3");
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalRecord);
                Assert.AreNotEqual(reducedRecord.Formula, Formula.GetOrParse("|T|.1.3"));
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }

            // |T||T.1||T.2|T.3 => ||.2|T.1|.3|T.1
            //  test |a|bc => |a|bc -> |T||a|Tb|a|Tc -> * on subsequent
            //      => |T|||T.1|T|T.2||T.1|T|T.3 where a= |T.1 b= |T.2 c= |T.3
            //      => |T|||T.1|T|T.2||T.1.3 
            //      => |T|||T.1|T|T.2|.3|T.1
            //      => |T|||T.1.2|.3|T.1
            //      => |T||.2|T.1|.3|T.1
            //      => |T||T.1||T.2|T.3,  ||ba|ca -> |T|a||Tb|Tc -> *
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|T||T.1||T.2|T.3");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("||.2|T.1|.3|T.1");
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }


            // error... ||.1|T.2|.3|T.2 is not a valid reduction for |.1||T.2||T.1|T.3
            //  => |.1||T.2||TT|T.3
            //  => |.1||T.2|F|T.3
            //  => |.1||T.2T
            //  => |.1.2
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|.1||T.2||T.1|T.3");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|.1.2");
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreNotEqual(reducedRecord, await Lucid.GetMostlyCanonicalRecordAsync("||.1|T.2|.3|T.2"));
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }
            // ||.2|.1.3|.3|T.2 => ||.2|T.3|.3|.1.2
            // Is not reducible by NRA...
            //  test .2ant => F 
            //  => ||F|.1.3|.3|T.2
            //  => |T|.3|T.2, canonical
            //  test .2ant => T
            //  => ||T|.1.3|.3|T.2
            //  => ||T|.1.3|T|T.2
            //  => ||T|.1.3.2, canonical
            //  test .2seq => F 
            //  => ||.2|.1.3|.3|TF
            //  => ||.2|.1.3|T.3
            //  => ||.2|.1F|T.3
            //  => ||T.2|T.3, canonical
            //  test .2seq => T 
            //  => ||.2|.1.3|.3|T.T
            //  => ||.2|.1.3|.3F
            //  => |T|.2|.1.3, canonical
            //  test .3ant => F 
            //  => ||.2|.1F|.3|T.2
            //  => ||T.2|.3|T.2
            //  => ||T.2|.3|TF
            //  => ||T.2|T.3, canonical
            //  test .3ant => T
            //  => ||.2|T.1|.3|T.2, canonical
            //  test .3seq => F 
            //  => ||.2|.1.3|F|T.2
            //  => ||.2|.1.3T, canonical
            //  test .3seq => T
            //  => ||.2|.1.3|T|T.2
            //  => ||.2|.1.3.2
            //  => |.2|.2|.1.3
            //  => |.2|T|.1.3, canonical
            // This is the first rule that I ever took the time to actually prove
            // cannot be implemented via wildcard analysis.
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("||.2|.1.3|.3|T.2");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("||.2|T.3|.3|.1.2");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }

            // ||.1.2||.1.3|.2|T.1 => ||.1.2||T.2|.1.3
            // This formula should be reducible using wildcard analysis
            //  test .1 -> T in f.A
            //      => ||T.2||.1.3|.2|T.1
            //      => ||T.2||.1.3|F|T.1, wildcard @ 12
            //      => ||T.2||.1.3T
            //      => ||T.2|T|.1.3
            //  replace wildcard @ 12 -> F
            //  => ||.1.2||.1.3|.2|TF
            //  => ||.1.2||.1.3|T.2
            //  => ||.1.2||T.2|.1.3
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("||.1.2||.1.3|.2|T.1");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("||.1.2||T.2|.1.3");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }

            // bug: ||.2|T.3||.1.2|.1.3 - is not reducible via wildcard analysis but should be.
            // Proof...
            //  test .2->T in f.A
            //      => ||T|T.3||.1.2|.1.3
            //      => |.3||.1.2|.1.3
            //      => |.3||.1.2|.1T
            //      => |.3||F.2|.1T, wildcard found
            //      => |.3|T|.1T
            //      => |.3.1
            //      => |.1.3
            //  therefore rewrite as....
            //  => ||.2|T.3||.1F|.1.3
            //  => ||.2|T.3|T|.1.3
            //  test .3 -> F in f.S
            //      => ||.2|T.3|T|.1F
            //      => ||.2|T.3|TT
            //      => ||.2|T.3F, wildcard found
            //      => T
            // therefore rewrite as....
            // => ||.2|TT|T|.1.3
            // => ||.2F|T|.1.3
            // => |T|T|.1.3
            // => |.1.3
            // 
            //  test .2->F in f.A, ||.2|T.3||.1.2|.1.3
            //      => ||F|T.3||.1.2|.1.3
            //      => |T||.1.2|.1.3, verified canonical
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("||.2|T.3||.1.2|.1.3");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|.1.3");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }

            // bug: |||.1.2|.1.3|.1|T|.2|T.3 - is not reducible via wildcard analysis but should be.
            // Proof...
            //  test .2->T in f.S
            //      => |||.1.2|.1.3|.1|T|T|T.3
            //      => |||.1.2|.1.3|.1|T.3
            //      test .3->F in f.S
            //          => |||.1.2|.1.3|.1|TF
            //          => |||.1.2|.1.3|.1T
            //          => |||F.2|F.3|.1T wildcard
            //          => ||TT|.1T 
            //          => |F|.1T 
            //          => T
            //      therefore .3->T in f.A...
            //      => |||.1.2|.1T|.1|T.3
            //      => |||F.2|.1T|.1|T.3 wildcard
            //      => ||T|.1T|.1|T.3
            //      => |.1|.1|T.3
            //      => |.1|T|T.3
            //      => |.1.3
            //  therefore rewrite as....
            //  => |||.1F|.1.3|.1|T|.2|T.3
            //  => ||T|.1.3|.1|T|.2|T.3
            //  test .3->F in f.A
            //      => ||T|.1F|.1|T|.2|T.3
            //      => ||T|.1F|.1|T|.2|T.3
            //      => ||TT|.1|T|.2|T.3
            //      => |F|.1|T|.2|T.3 wildcard
            //      => T
            //  therefore rewrite as....
            //  => ||T|.1.3|.1|T|.2|TT
            //  => ||T|.1.3|.1|T|.2F
            //  => ||T|.1.3|.1|TT
            //  => ||T|.1.3|.1F
            //  => ||T|.1.3T
            //  => |T|T|.1.3
            //  => |.1.3
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|||.1.2|.1.3|.1|T|.2|T.3");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|.1.3");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }

            // |.1||.2|T.3|.3|.1.2 should be reducible via wildcard analysis
            // proof...
            // test .1 -> F in f.A...
            //  => |F||.2|T.3|.3|.1.2 wildcard
            // therefore
            // => |.1||.2|T.3|.3|T.2 
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|.1||.2|T.3|.3|.1.2"); // id=484
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("||.2.3||.1.2|.1.3"); //id=483
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }

            // |T||.1.2||T.1|T.2 => ||.1|T.2|.2|T.1
            // Should be reducible by rewriting using rule |T|a||Tb|Tc -> ||ab||ac 
            // proof...
            //  => ||.1|.1.2||.2|.1.2
            //  => ||.1|T.2||.2|.1T
            //  => ||.1|T.2||.2|T.1
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|T||.1.2||T.1|T.2");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("||.1|T.2|.2|T.1");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }

            await ReduceFormulaWithManyTargetsUNOWildcard();


            // |||T.2|T.3||.2.3|.1|TT is not a valid reduction for |||T.2|T.3||.2.3|.1|T.2
            //  test .2->F in antecedent
            //  => |||TF|T.3||.2.3|.1|T.2
            //  => ||T|T.3||.2.3|.1|T.2
            //  => |.3||.2.3|.1|T.2
            //  => |.3||.2T|.1|T.2
            //  => |.3||T.2|.1|T.2
            //  => |.3||T.2|.1|TF !!!this is NOT a wildcard because the target term should be replaced with the OPPOSITE of the test value 
            //  => |.3||T.2|.1T
            // In order to to be able to check the value of the replacement against the value of the test value the test value 
            // had to be added to the ReductionTargetFinder proof tracer class.
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|||T.2|T.3||.2.3|.1|T.2");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|T||.1.3|.2.3");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }


            // tossed an error
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("||.2.3||.1.2|.3|T.1");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("||.1.3||.1.2|.3|T.2");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }


            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|T||.1|T.2|.1|T.3");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|.1|.2.3");
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }
            // |.3||.1|T.2|.2|.1.T => ||.1.2||.1.3|.2.3
            // => |T||.3|T|.1|T.2|.3|T|.2|.1T |a|bc -> |T||a|Tb|a|Tc  a= .3 b= |.1|T.2 c= |.2|.1T
            // => |T||.3|T|.1|T.2|.2|T|.3|.1T 
            // => |T| |.1|T|.3|T.2 |.2|T|.3|.1T 
            {
                var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|.3||.1|T.2|.2|T.1");
                var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("||.1.2||.1.3|.2.3"); // verified canonical
                Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
                var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
                Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
            }

            await ReduceFormulaWithDeepProof();

        }

        // |||T.1|T.3||.1.3|.2.3 => |T||.1.3|.2.3
        //
        // --------- 5/25/25
        // since rhs[3<-F] =>* F then lhs[.3<-T]
        //      |||T.1|TT||.1.3|.2.3
        //      |||T.1F||.1.3|.2.3
        //      |T||.1.3|.2.3
        // Note that cofactor used to reduce ||13|23 can be computed
        // from the cofactors for |13 and |23
        //
        // ///////// 12/1/2024
        // DebugAssertException: 'an instance of the subterm should have been found
        // Caused by the wildcard tracer returning wildcard position of 5 instead of 4
        //  => test .1seq -> T
        //      => |||T.1|T.3||T.3|.2.3
        //      => |||T.1|T.3||T.3|.2F
        //      => |||T.1|T.3||T.3T
        //      => |||T.1|T.3.3
        //      => |.3||T.1|T.3
        //      => |.3||T.1|TT
        //      => |.3||T.1F wildcard @ 5
        //      => |.3T
        //      => |T.3
        //  => replace wildcard @ 4 -> F
        //  => |||TF|T.3||.1.3|.2.3
        //  => ||T|T.3||.1.3|.2.3
        //  => |.3||.1.3|.2.3
        //  => |.3||.1T|.2T
        //  => |.3||T.1|T.2
        [TestMethod]
        public async Task ComputedCofactorDeiteration()
        {
            var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|||T.1|T.3||.1.3|.2.3");
            var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|T||.1.3|.2.3");
            Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
            var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
            Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
        }


        // ||.1|.2.3||T.1.3 =>* ||.1|T.3||.1.2.3
        // This is an example of swapping mutually irrelevant terms.
        //
        // ########### UPDATE 4/28/2025
        //
        // The secret to minimizing this expression is to understand that
        // the concept of *irrelevance* is what drives minimization.
        // In this expression the T and the 2 can be swapped because when 2 == F then
        // the T is proof irrelevant and may therefore be replaced with 2.
        // And the reverse is true, when the last 2 in ||.1|.2.3|.3|2.1 == F then 
        // the first 2 is irrelevant and may therefore be replaced with T.
        //
        // That is....
        // 
        // ||.1|.2.3|.3|T.1 => ||.1|.2.3|.3|2.1, proof....
        //      ||.1|F.3|.3|T.1 ; test lhs [2<-F]
        //      ||.1T|.3|T.1    ; empty-cut elimination
        //      ||T.1|.3|T.1    ; ordering
        //      ||T.1|.3T       ; deiteration, removed T identified as wildcard
        //      ||T.1|T.3       ; ordering
        // ||.1|.2.3|.3|T.1     ; ||.1|.2.3|.3|T.1[T<-.2] by previous test proof
        //
        // and....
        // ||.1|.2.3|.3|.1.2 => ||.1|T.3|.3|.1.2 => , proof....
        //      ||.1|.2.3|.3|.1F    ; test rhs
        //      ||.1|.2.3|.3T       ; empty-cut elimination
        //      ||.1|.2F|.3T        ; deiteration
        //      ||.1T|.3T           ; empty-cut elimination, removed .2 identified as wildcard
        //      ||T.1|T.3           ; ordering
        // ||.1|.2.3|.3|.1T         ; ||.1|.2.3|.3|.1.2[.2<-T] by previous test proof
        //
        // Note that both cofactors have the same conclusion,  
        // and that swapping the cofactor terms (.2 and T) produces a reduction.  
        // The 'test' proofs above are how I think about 'relevance theory' and how to minimize
        // expressions by discovering relevance relationships between terms.  
        // 
        // However, I don't know how to prove complexity limits using these test proofs.  
        // Instead, LE uses cofactors because I can prove complexity limits when using cofactors.
        // Cofactors capture the essence of 'test proofs' by capturing the relevant parts of such proofs...
        //  - the tested expression
        //  - the test value
        //  - the test term
        //  - the conclusion
        // The basic rule for 'cofactor swapping' is...  
        //  If
        //      E[A<-F] =>* X and
        //      E[B<-F] =>* X
        //      where E is a mostly canonical expression (both the left and right sides are canonical but E is not)
        //  Then
        //      The expression E[A<->B] is equivalent to E.
        //      That is, we can swap A with B without changing E's truth function.
        //  Because
        //      When one or both of A and B are false 
        //      swapping the positions of A and B not will not change E's truth function 
        //      because E always reduces to X when one or both of A and B are false.
        // 
        //      When A and B are both true then
        //      swapping the positions of A and B not change E's truth function 
        //      because A and B will have the same value.
        //
        //  Also, we can constrain this rule to only instances of E where A and B exist in different sides of an expression.  
        //  Because if A and B were on the same side of E then that side would be reducible, 
        //  which they can't because E is mostly canonical.  
        //
        //  Here's another way to show that .2 and T are swappable using cofactors....
        //      ||.1|.2.3||T.1.3[.2<-F] =>* 
        //          ||.1|F.3||T.1.3
        //          ||.1T||T.1.3
        //          ||T.1|T.3
        //      ||.1|.2.3||T.1.3[T<-F] =>* 
        //          ||.1|.2.3||F.1.3
        //          ||.1|.2.3|T.3
        //          ||.1|.2F|T.3    
        //          ||.1T|T.3    
        //          ||T.1|T.3    
        //
        //  LE can look at the cofactors of its sides to detect these cofactors.
        // (LHS RHS)[A<-F] =>* X
        // (LHS RHS)[B<-F] =>* X
        //  The relevant cofactors are...  
        //      LHS[A<-F] =>* X
        //      RHS[B<-F] =>* Y
        //      RHS[X<-T] =>* Y
        //  The relevant cofactors are...  
        //      LHS[A<-F] =>* X
        //      RHS[B<-F] =>* Y
        //      LHS[Y<-T] =>* X
        //      RHS[X<-T] =>* Y
        //  Given the previous cofactors...
        //  - when A<-F then LHS =>* X, RHS =>* Y, and E =>* (X Y), and
        //  - when B<-F then E =>* (X Y)

        //
        // For this expression the relevant cofactors are...
        //      |.1|.2.3[.2<-F] =>* |T.1
        //      ||T.1.3[T<-F] =>* |T.3
        //      |.1|.2.3[|T.3<-T] =>* ???
        //          look for |.1|.2.3[.3<-F] =>* |T.1 instead
        //          because |T.3[.3<-F] =>* T
        //      ||T.1.3[|T.1<-T] =>* |T.3
        // Note that minimizing this expression illustrates some kind of rule about, or property of, cofactor subterms.
        // That is, that two cofactors...
        //  E[A<-M] =>* X, and
        //  E[B<-N] =>* X
        // ...are equivalent when A[B<-N] =>* M or B[A<-M] =>* N
        // 
        // ((1 (T 2))((T 1) 2)) 
        // test A = 2, B = T
        //      LHS[A<-F] =>* X: (1 (T F)) =>* (T 1)
        //      RHS[B<-F] =>* Y: ((F 1) 2) =>* (T 2)
        //      LHS[Y<-T] =>* X: (1 T)     =>* (T 1)
        //      RHS[X<-T] =>* Y: (T 2)     =>* (T 2)
        //  
        //
        [TestMethod]
        public async Task SlightlyDeeperWildcardReduction()
        {
            var trueId = await Lucid.GetConstantExpressionIdAsync(true);
            var falseId = await Lucid.GetConstantExpressionIdAsync(false);
            {
                var lhs = await Lucid.GetMostlyCanonicalRecordAsync("|.1|.2.3");
                var notOne = await Lucid.GetMostlyCanonicalRecordAsync("|T.1");
                var notThree = await Lucid.GetMostlyCanonicalRecordAsync("|T.3");
                var rhs = await Lucid.GetMostlyCanonicalRecordAsync("||T.1.3");
                var two = await Lucid.GetMostlyCanonicalRecordAsync(".2");
                var three = await Lucid.GetMostlyCanonicalRecordAsync(".3");

                //  ||T.1.3[T<-F] =>* |T.3              ; RHS[C<-F] =>* Y     
                var rCofactors = Lucid.Cofactors
                    .Where(_ => _.ExpressionId == rhs.Id 
                            && _.ReplacementId == falseId 
                            && _.SubtermId == trueId)
                    .ToArray();
                Assert.IsTrue(rCofactors.Any());

                //  |.1|.2.3[.2<-F] =>* |T.1,           ; LHS[A<-F] =>* X     
                var lCofactors = Lucid.Cofactors
                    .Where(_ => _.ExpressionId == lhs.Id
                            && _.ReplacementId == falseId
                            && _.SubtermId == two.Id
                            && _.ConclusionId == notOne.Id)
                    .ToArray();
                Assert.IsTrue(rCofactors.Any());

                //  ||T.1.3[|T.1<-T] =>* |T.3          ; RHS[X<-T] =>* Y
                rCofactors = Lucid.Cofactors
                    .Where(_ => _.ExpressionId == rhs.Id
                            && _.ReplacementId == trueId
                            && _.SubtermId == notOne.Id
                            && _.ConclusionId == notThree.Id)
                    .ToArray();
                Assert.IsTrue(rCofactors.Any());



                //      |.1|.2.3[|T.3<-T] =>* ???
                //          look for |.1|.2.3[.3<-F] =>* |T.1 instead
                //          because |T.3[.3<-F] =>* T
                lCofactors = Lucid.Cofactors
                    .Where(_ => _.ExpressionId == notThree.Id
                            && _.ReplacementId == falseId
                            && _.SubtermId == three.Id
                            && _.ConclusionId == trueId)
                    .ToArray();
                Assert.IsTrue(lCofactors.Any());
                lCofactors = Lucid.Cofactors
                    .Where(_ => _.ExpressionId == lhs.Id
                            && _.ReplacementId == falseId
                            && _.SubtermId == three.Id
                            && _.ConclusionId == notOne.Id)
                    .ToArray();
                Assert.IsTrue(lCofactors.Any());

            }
            var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("||.1|.2.3||T.1.3");
            var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("||.1|T.3||.1.2.3");
            Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
            var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
            Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
        }

        [TestMethod]
        public async Task SlightlyDeepReorderingReduction()
        {
            {
                var trueId = await Lucid.GetConstantExpressionIdAsync(true);
                var subRecord = await Lucid.GetMostlyCanonicalRecordAsync("|.1T");
                var fgfCofactors = await Lucid.GetFGroundingFCofactorsAsync(subRecord.Id);
                var IsTrueFgfCofactor = fgfCofactors.Where(_ => _.SubtermId == trueId).Any();
                Assert.IsFalse(IsTrueFgfCofactor);
            }
            {
                var trueId = await Lucid.GetConstantExpressionIdAsync(true);
                var subRecord = await Lucid.GetMostlyCanonicalRecordAsync("|.3|.1T");
                var fgfCofactors = await Lucid.GetFGroundingFCofactorsAsync(subRecord.Id);
                var IsTrueFgfCofactor = fgfCofactors.Where(_ => _.SubtermId == trueId).Any();
                Assert.IsFalse(IsTrueFgfCofactor);
            }

            var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|.2|.3|.1T");
            var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|.2|.3|T.1");

            Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
            var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
            Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(reducedRecord.Formula).ToString());
            Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
        }

        [TestMethod]
        public async Task SimplestDeiterationTake1()
        {
            var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|.1.1");
            var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|T.1");
            Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
            var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
            Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
        }

        [TestMethod]
        public async Task SimplestDeiterationTake2()
        {
            var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|.1|T.1");
            var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("T");
            Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
            var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
            Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
        }

        [TestMethod]
        public async Task SimplestDeiterationTake3()
        {
            var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|.1|.1.2");
            var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|.1|T.2");
            Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
            var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
            Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
        }

        // ||.1|.2.3||.1.2|.3|T.1 => |.3|.1|T.2
        //
        // Solution using HTRR (Hyper Transitive Reduction Rule)...
        // |.1.2 == .1->~.2,  |.1|T.2 == .1->.2
        // BIG
        // 
        // 
        //  Idea...
        //  Currently RR uses *groundings* to figure out new reductions.
        //  A grounding is a statement that says...
        //      > Assigning {termValue} to all the instances of {term} listed in {nameof(Positions)}
        //      > compels {formula} to have the value {formulaValue}
        //  > NOTE: a *grounding* is a generalization of the concept of *prime implicant* in the literature.
        //  > Because when {term} has {termValue}, the formula has value {formulaValue}
        //  Instead, its better to use the concept of 'irrelevance'.
        //  And a grounding is a statement that says...
        //      > Assigning {termValue} to all the instances of {term} on the {side} side of the formula
        //      > makes all the instances of {term} in the other side of the formula irrelevant.

        //  
        //
        // ||.1|.2.3||.1.2|.3|T.1 
        //  => ||.1|.2.3 |T|.1|T.2||.1.2|.3|T.1 
        //  
        //
        // ||.1|.2.3||.1.2|.3|T.1 : |T|.1.2|.3|T.1
        // => ||.1|.2.3||F.2|.3|TF : left(.1 => T, .2 => F) therefore right(.1=F)
        // => ||.1|.2.3|T|.3T 
        // => ||.1|.2.3.3
        // => |.3|.1|T.2
        //
        // ||.1|.2.3||.1.2|.3|T.1 : 
        // => ||T|.2.3||.1.2|.3|T.1 : right(.1 => F, .3 => F) therefore left(.1=T)
        // => ||T|T.3||.1.2|.3|T.1 : right(.2 => F, .3 => F) therefore left(.2=T)
        // => |.3||.1.2|.3|T.1 
        // => |.3||.1.2|T|T.1 
        // => |.3||.1.2.1
        // => |.3|.1|T.2
        //
        //  error...  ||.1|.2.3||F.2|.3|T.1 is not a valid reduction for ||.1|.2.3||.1.2|.3|T.1 (1480)
        //  Here's what Reduce does...
        //  test .1->T in antecedent
        //      => ||T|.2.3||.1.2|.3|T.1
        //          test .2->F in antecedent 
        //          => ||T|F.3||.1.2|.3|T.1
        //          => ||TT||.1.2|.3|T.1
        //          => |F||.1.2|.3|T.1 wildcard
        //          => T
        //      => ||T|.2.3||.1T|.3|T.1 .2->T in subsequent
        //  $   => ||T|.2.3||.1T|.3|TF !!!!!replacing .1 with F is also a wildcard!!!!!! (note: F is the opposite of the test value) 
        //      => ||T|.2.3||.1T|.3T
        //      => ||T|.2.3||.1T|T.3
        //      => ||T|.2.3||.1T|TT
        //      => ||T|.2.3||.1TF wildcard
        //      => ||T|.2.3T wildcard
        //  => ||.1|.2.3||F.2|.3|TF
        //  => ||.1|.2.3|T|.3T
        //  => ||.1|.2.3.3
        //  => |.3|.1|.2.3
        //  => |.3|.1|T.2
        // The proof tracer should return the position of the first discovered wildcard.
        // However, the proof tracer did not previously recognize the wildcard at position 12 so it returned position 8 instead.
        // NOTE...
        // The proof tracer needs to be extended to recognize 'wildcard reductions' 
        // that replace a formula that contains an instance of the target test term with a constant.
        // Such reductions also identify wildcards.
        [TestMethod]
        public async Task ReduceFormulaWithManyTargetsUNOWildcard()
        {
            var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("||.1|.2.3||.1.2|.3|T.1");
            var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|.3|.1|T.2");
            var testFormula = await Lucid.GetMostlyCanonicalRecordAsync("||.1|.2.3||F.2|.3|TF");
            var testFormula2 = await Lucid.GetMostlyCanonicalRecordAsync("||.1|.2.3||.1.2|.3|TF");
            Assert.AreEqual(TruthTable.GetTruthTable(testFormula2.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
            Assert.AreEqual(TruthTable.GetTruthTable(testFormula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
            Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
            var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
            Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
        }

        ///
        // |.1||.2|T.3|.3|.1.2 should be reducible via wildcard analysis
        //  test .1->F in antecedent
        //      => |F||.2|T.3|.3|.1.2 wildcard
        //      => T
        //  wildcard in subsequent: .1->T 
        //  => |.1||.2|T.3|.3|T.2 
        // error... |||T.1|.2.3||.1.2|.3|T.2 is not a valid reduction for |||.1.2|.3|T.2||.2.3|.1|T.2 (3407)
        // |||T.1|.2.3||.1.2|.3|T.2 (3407) ->* |T||.1.2|.1.3) 
        // |||.1.2|.3|T.2||.2.3|.1|T.2 (6071) ->* |T||.1.3) 
        // Here's basically what the NRA is (currently) doing... 
        //  test |T.2 -> F in antecedent
        //      => |||.1.2|.3F||.2.3|.1|T.2
        //      => |||.1.2T||.2.3|.1|T.2
        //      => ||T|.1.2||.2.3|.1|T.2
        //      test .1 -> T in antecedent
        //          => ||T|T.2||.2.3|.1|T.2
        //          => |.2||.2.3|.1|T.2, cuz |T|T.1 => .1
        //          => |.2||T.3|.1|TT, cuz .2 is wildcard in seq when .2 -> F
        //          => |.2||T.3|.1F,  <- wildcard
        //      => ||T|.1.2||.2.3|F|T.2, <- wildcard
        // => |||.1.2|.3|T.2||.2.3|.1T, cuz |T.2 is sub wildcard 
        // => |||.1.2|.3|T.2||.2.3|T.1
        // => |||.1.2|.3|T.2||T.1|.2.3
        // The problem is that the NRA finds that |T.2 is a wildcard when its not.  
        // This happens because NRA fails to consider that |T.2 was modified while testing .1 -> T. (** cuz .2 is wildcard in seq when .2 -> F)
        // How to fix?...
        // 1)   This formula is not reducible via wildcard analysis, it requires a hard-coded ordering rule.
        //      The ordering rule is not yet implemented.
        //      If this rule were already implemented then this problem would go away because the NRA wouldn't even get
        //      as far as attempting wildcard analysis on this formula.
        //      But that's kinda cheating, it doesnt fix the problem but just avoids it.
        //      I guess what I would want is for the call to Reduce to fail gracefully, by returning an un-reduced formula.
        // 2)   Extend the NRA proof trace to track all reductions in a 'context', a context that is maintained
        //      across sub-reductions.
        //      And disallow reductions that modify a subterm that is the subject of a parent context.
        //      This would cause Reduce to fail gracefully.
        //      But it's a lot of work, and its complicated.
        //
        // Instead, I took a less labor-intensive fix as a shortcut.
        // This shortcut is NOT THE SAME as extending the NRA to track 'context's but its far less labor intensive.
        // The shortcut is to skip common terms that contain any other common terms as a subterm.
        // Using this scheme, |T.2 would be skipped as a common term because it contains another comment term, .2, within it.
        //
        // Note 1/16/25: I will need to go back and implement 2)... the contextual 'black-list', 
        //  because the previous fix described above doesn't work when doing 'demorgan reduction'.
        ///
        [TestMethod]
        public async Task ReduceFormulaWithDeepProof()
        {
            var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|||.1.2|.3|T.2||.2.3|.1|T.2");
            var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|.1.3");
            Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
            var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
            Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
        }

        /// <summary>
        /// |.1||T.2|T.3 => |T||.1.2|.1.3
        /// 
        /// NOTES...
        ///     Not a very important reduction since its ONLY valid when .1 has length == 1
        /// 
        ///     |.1||.1.2|.1.3 is a critical term.
        ///     It can be reduced two ways....
        ///         #1: |1||1.2|1.3 => |1||T.2|T.3, or
        ///         #2: |1||1.2|1.3 => |T||1.2|1.3
        ///     
        /// ##### 4/28/25
        /// Now easily solvable using cofactors.
        /// Since...
        ///     1[1<-F] =>* F, and 
        ///     ||T.2|T.3[T<-F] =>* F 
        /// then .1 in the lhs may be swapped for T in the rhs.
        /// 
        /// 
        /// ###### 1/24/25
        /// Previous attempt didn't work.  
        /// For now, this rule is just hardcoded, see NandReducerCommutativeRules.
        /// It might be necessary to implement a generalized form of this type of wildcard swapping 
        /// in order for the reduction algorithm to cover all the formulas in the base RR rule database
        /// (and complete coverage is required for the reduction algorithm completeness proof).  
        /// 
        /// ###### 11/30/24
        /// The new way...
        /// Reducible directly by wildcard swapping .1 <-> T
        /// => |T||.1.2|.1.3
        /// 
        /// The old way...
        /// Reducible by rewriting using rule |a|bc -> |T||a|Tb|a|Tc.
        /// => |T||.1|T|T.2|.1|T|T.3 
        /// => |T||.1|T|T.2|.1.3
        /// => |T||.1.2|.1.3
        /// </summary>
        [TestMethod]
        public async Task SimpleWildcardSwappingExample()
        {
            {
//                if (reducedFormula.CompareTo(startingNand) < 0)

                var noncanonical = await Lucid.GetMostlyCanonicalRecordAsync("|.3|T.2");
                var ordered = await Lucid.GetMostlyCanonicalRecordAsync("||T.2.3");
                Assert.AreEqual(ordered.Formula.Text, "||T.2.3");
                var canonical = await Lucid.GetCanonicalRecordAsync(noncanonical);
                Assert.AreEqual(ordered.Formula.Text, canonical.Formula.Text);
            }
            {
                var ifNotTwoThenThree = await Lucid.GetMostlyCanonicalRecordAsync("||T.2|T.3");
                var canonical = await Lucid.GetCanonicalRecordAsync(ifNotTwoThenThree);
                Assert.AreEqual(ifNotTwoThenThree.Formula.Text, canonical.Formula.Text);
            }
            var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|.1||T.2|T.3");
            Assert.AreEqual("|.1||T.2|T.3", nonCanonicalformula.Formula.Text);
            var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|T||.1.2|.1.3");
            Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
            var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
            Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
        }

        [TestMethod]
        public async Task SlightlyDeepWildcardReduction()
        {
            var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|.2|.3|.1.2");
            var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("|.2|.3|T.1");
            Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
            var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
            Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
        }


        /// <summary>
        /// |T||1|T.2|2|T.1 => ||1.2||T.1|T.2 : Some kind of DeMorgan's law for nand systems? 
        ///     That is, the negation of ||.1|T.2|.2|T.1 is ||.1.2||T.1|T.2, which is 'simpler'.
        ///     Note that this rule ALWAYS produces a simpler formula
        ///     
        /// 
        /// The reverse, ||.1|T.2|.2|T.1 => |T||.1.2||T.1|T.2, is not a rule because the right side is always longer.
        /// 
        /// This formula cannot be reduced using just the current term-value analysis algorithm and constant elimination rules.
        /// Also the two terms in this formula are a [critical pair](https://en.wikipedia.org/wiki/Critical_pair_(term_rewriting)).
        ///     cuz...
        ///         |T||.1|.1.2|.2|.1.2 ; can be reduced to...
        ///             => ||.1.2||T.1|T.2 ; using wildcard swapping |.1.2 <-> T
        ///         or => |T||.1|T.2|.2|T.1 ; by reducing descendants first
        ///         Therefore (||.1.2||T.1|T.2, |T||.1|T.2|.2|T.1) is a critical pair.
        /// Therefore, these two terms form a new rule.
        /// The [Knuth-Bendix](https://en.wikipedia.org/wiki/Knuth%E2%80%93Bendix_completion_algorithm) way of 
        /// extending the current system would be to add this production rule to the set of rules in our system.  
        /// 
        /// ########### 
        ///     note that the rhs has no obvious f-grounding f-cofactors for (T ((1 (T 2)) ((T 1) 2))) 
        ///      (1 (T 2))   [1<-T]  =>* 2
        ///      (1 (T 2))   [2<-F]  =>* (T 1)
        ///      (1 (T 2))   [2<-T]  =>* T
        ///      (1 (T 2))   [T<-F]  =>* (T 1)
        ///      ((T 1) 2)   [2<-T]  =>* 1
        ///      ((T 1) 2)   [1<-F]  =>* (T 2)
        ///      ((T 1) 2)   [1<-T]  =>* T
        ///      ((T 1) 2)   [T<-F]  =>* (T 2)
        ///      (T ((1 (T 2)) ((T 1) 2)))   [1<-T]  =>* 2
        ///      (T ((1 (T 2)) ((T 1) 2)))   [1<-F]  =>* (T 2)
        ///      (T ((1 (T 2)) ((T 1) 2)))   [2<-T]  =>* 1
        ///      (T ((1 (T 2)) ((T 1) 2)))   [2<-F]  =>* (T 1)
        /// 
        /// 
        /// Btw, the lhs above says "1->2 and 2->1", and the rhs says "1 == 2"
        /// The secret is to be able to recognize that this... 
        ///     (T ((1 (T 2)) ((T 1) 2))) ; note that the rhs has no obvious f-grounding f-cofactor
        /// is equivalent to this...
        ///     (T ((1 (1 2)) (2 (1 2)))) ; note that (1 2) is an easily, mechanically identifiable f-grounding f-cofactor
        /// 
        /// ########### UPDATE 6/10/2025
        /// Cofactors have been reworked to remove 'unified' cofactors and use equalities instead.
        /// So this expression is now solvable using cofactors like so...
        ///     Since (T ((1 (1 2)) ((1 2) 2))) == (T ((1 (T 2)) ((T 1) 2))) 
        ///         and are therefore equivalent
        ///     and since...   
        ///         (T ((1 (1 2)) ((1 2) 2)))[(1 2)<-F] =>* T, and 
        ///         (T ((1 (T 2)) ((T 1) 2)))[T<-F] =>* T, 
        ///     Then T and (1 2) are swappable, resulting in a simpler expression.
        /// Note...
        /// When the cofactors are computed for (1 (T 2)), a tgf-cofactor is computed for (1 (1 2))[(1 2)<-F].
        /// When cofactors are computed for ((1 (T 2)) ((T 1) 2)), an fgf-cofactor is computed from 
        /// the tgf-cofactors for (1 (1 2)) and (2 (1 2)).
        /// 
        /// ########### UPDATE 4/28/2025
        /// 
        /// Now easily solvable using cofactors.
        /// Since...   
        ///     (T ((1 (T 2)) ((T 1) 2)))[T<-F] =>* T, and 
        ///     (T ((1 (T 2)) ((T 1) 2)))[(1 2)<-F] =>* T
        /// Then T and (1 2) are swappable.
        /// 
        /// ### Notes
        /// This expression also requires 'cofactor unification' to minimize.
        /// After extending LE with 'cofactor unification' then LE computes the following tgf-cofactors...
        ///     (1 (T 2))[(1 2)<-F] =>* T
        ///     (2 (T 1))[(1 2)<-F] =>* T
        ///     ((1 (T 2)) ((T 1) 2))[(1 2)<-F] =>* F
        ///     (T ((1 (T 2)) ((T 1) 2)))[(1 2)<-F] =>* T
        /// in addition to....
        ///     (1 (T 2))[(T 2)<-F] =>* T
        ///     (2 (T 1))[(T 1)<-F] =>* T
        ///     T[T<-F] =>* F
        ///     (T ((1 (T 2)) ((T 1) 2)))[T<-F] =>* T
        /// 
        /// LE currently does this by adding a 'UnifiedSubtermId field to cofactor records.
        /// I dont like this, at some point I' just add new records, like above.  
        /// Programatically it's possible to discover that (1 (T 2))[(1 2)<-F] =>* T 
        /// is a 'unified' or 'derived' cofactor because of the existence of (1 (T 2))[T<-F] =>* (1 T), 
        /// which is the exact same cofactor except that subterm == (T 2) instead of (1 2).  
        /// Meaning, if (1 2) can be deiterated then (T 2) can be deiterated.  
        /// 
        /// Pretty sure, but not 100%, that its possible to prove that # of computed cofactors is still polynomial.  
        /// Theorem: There's at most Ne2 cofactors for any expression E, when N is the length of E
        /// 
        /// 
        /// ########### UPDATE 2/2/25
        /// I implemented 'wildcard swapping' and then it didn't work right, 
        /// because it needed the proof tree to be complete in order to work, duh.
        /// So I would need to expand the algorithm to always create complete proof trees.  
        /// While working on the design of such an enhancement I generalized the concepts that I've defined so far 
        /// (wildcard reduction and wildcard swapping) down to a single concept I'm calling 'term relevance'.  
        /// And I've invented a single algorithm that unifies wildcard reduction and wildcard swapping, 
        /// while also fixing wildcard swapping by always creating complete proofs.  
        /// I believe that I can show that complete proof trees have less than ....hmmm
        /// 
        /// Here's how this formula is reduced using the concept of 'relevance', 
        /// The most relevant terms in a formula are all the instances of a term that, 
        /// when all instances are replaced with a constant, 
        /// completely determine the value of a formula.  
        /// In other words, the most relevant terms can make all the other terms irrelevant, for a specific constant value, either T or F.  
        /// Example.... |.2|.1.  
        ///     > By setting .2 to F, .2 can *compel* the formula to be T.  
        ///     > .1 is irrelevant when .2 is F, .2 is relevant.
        /// Example.... |.2|.1.2.  .1 is totally irrelevant, .2 is totally relevant.
        /// The terms in a formula can be order by...
        /// - How many other terms must be assigned a value in order to assign a value to the formula.
        /// - How much replacing the term can reduce the formula.
        ///     > the more towards the head of the formula the better, 
        ///     > because replacing it will reduce the formula more.
        /// RR only tracks the most relevant terms.
        /// RR reduces formulas by repeatedly, when possible,...
        /// - making terms irrelevant by replacing them with constants. aka wildcard reduction.
        /// - making a term more relevant by reducing the number of term instances. aka wildcard swapping.
        /// 
        /// 
        /// 
        /// ########### UPDATE 1/26/25
        /// I now have better definitions of wildcard analysis and 'wildcard swapping'....
        /// RR refers to all instances of T in a formula as *wildcards*.  
        /// So called because they can often be replaced with multiple values without changing the truth table of the formula.  
        /// The production rules produced by the Knuth-Bendix process seem to work by doing wildcard reduction 
        /// until the formula is 'mostly canonical'.  
        /// Then 'wildcard swapping' can reduce a mostly-canonical formula to 
        /// a reduced, non-canonical formula,
        /// and thus 'restart' the reduction process.  
        /// Or not, in which case the formula is canonical.
        /// 
        /// This is wildcard reduction...
        ///     Wildcard reduction is a kind of search for terms that are mostly irrelevant, and replacing them with a constant value.
        ///         Let F be a formula where a term S appears in both sides of the formula
        ///         Let V (for test value) be a variable that has a constant value of T or F.
        ///         Let C (for test case) be the formula created by replacing all instances of S with V on uno side of S.  
        ///         Let P (for proof) be the proof that reduces C to its canonical form.
        ///         Then... all instances of S in C, that are inherited from F, and that are irrelevant to P, 
        ///         may be replaced with V?F:T to create a reduced formula R.
        ///         
        /// This is wildcard swapping....  
        ///     wildcard swapping is chiral, this form replaces many terms with a constant value...
        ///         Let F be a formula of the form |TS
        ///         Let V (for test value) be a variable that has a constant value of T or F.
        ///         Let C (for test case) be the formula created by replacing all instances of S in uno side of F with V.  
        ///         If C reduces to F then all instances of S in F may be replaced with T, and the leading T replaced with S.  
        ///         
        ///     The other form of swapping, where constants are replaced by terms, is only valid when 
        ///     S.Length == 1, and is covered by some hard-coded rules in NandReducerCommutativeRules.
        ///     
        /// For this sprint ...
        ///  - The FIRST reduction should always be the Lookup table.
        ///     > Most hardcoded rules can be moved to the Lookup table, like |F.1 => T
        ///  - The 2nd reduction should be wildcard 'reduction'
        ///  - The 3rd reduction should be wildcard 'expansion'
        ///  - Finally, wildcard swapping.
        ///  - start indexing ALL formulas in the Lookup 
        ///     > because the trie can be used to make unification will swapping *a lot* easier and much faster 
        ///     - FormulaRecord sill includes the Text column AND the Text column is indexed
        ///         > because looking up records by formula is required
        ///     > It used to be that a formula was reducible if it matched a path in the Lookup table.
        ///     > Now its necessary to find a match, *and then*, check if the matching formula is canonical
        ///  - need to add unification to wildcard analysis and wildcard swapping
        ///         > That is, a term can be replaced if any of its substitutions match the test term.  
        ///         > Substitutions in a formula F are discovered by following the mappings back to other formulas that reduce to F.  
        ///  + change the current implementation of wildcard analysis to match the description above (which it doesnt as of writing).
        ///  - implement wildcard swapping 
        ///  - extend wildcard analysis by implementing 'term blacklisting'.  See ReduceFormulaWithDeepProof.
        ///         > This will remove the need for using 'independent terms'.
        ///         
        /// |T||.1|T.2|.2|T.1 => ||.1.2||T.1|T.2 
        ///     1) |.1|.1.2 => |.1|T.2 : wildcard expansion 
        ///     2) |.2|.2.1 => |.2|T.1 : wildcard expansion
        ///     3) |.2.1 => |.1.2 : wildcard swapping
        ///     
        /// 
        /// ############# UPDATE 1/17/25
        /// Forget about my comments back on 11/20/24.  
        /// The RR rules did not become locally confluent by removing constants, as I predicted they would.  
        /// I wasted about a month on that. 
        /// BUT, the detour led to a revelation...  that information is lost when 'dead' terms are replaced a constant.  
        /// Information that RR could possibly use later to reduce a formula.  
        /// The correct solution to making RR locally confluent was not to stop using constants, 
        /// the correct solution was to stop replacing terms with constants.  
        /// Instead, I should have kept track of all the viable replacements for every term.  
        /// Doing so makes it possible to later 'deduce' what RR calls DeMorgan reductions by unifying terms that have more than uno possible value.  
        /// So, I need to come up with some way implementing this idea.
        /// 
        /// After careful consideration I've come up with an extension to wildcard-analysis that...  
        /// - is a pretty straight-forward extension of the current reduction algorithm  
        /// - gets RR past the 'demorgan barrier'
        /// - is provably correct
        /// - is provably polynomial
        /// 
        /// RR calls formulas where every term in the formula is canonical a 'mostly-canonical' formula'.
        /// Being mostly-canonical is like being mostly-dead, its not the same.  
        /// |T|ab, where a and b are canonical is an example of a mostly-canonical formula.  
        /// Wildcard analysis is only capable of reducing terms within a formula, not the formula itself, 
        /// so wildcard-analysis cannot reduce mostly-canonical formulas.  
        /// In other words, wildcard-analysis can reduce a formula to its 'mostly-canonical' form but no further.
        /// 
        /// I think that RR needs to implement some kind of 'wildcard-swapping'(described elsewhere) 
        /// to go from 'mostly-canonical' to 'all-canonical', or reduced and 'non-canonical' again.
        /// 
        /// 
        /// ############################ 11/20/24
        /// 
        /// 
        /// In order to be able to implement the rule |T||.1|T.2|.2|T.1 => ||.1.2||T.1|T.2, 
        /// the system was simplified by eliminating constants.
        /// Removing constants also removes the need for wildcard substitution and wildcard swapping.
        /// The advantage to getting rid of constants is that it removes the need for this particular production rule 
        /// while leaving everything else working the same.  That is...
        ///         |T||.1|.1.2|.2|.1.2 ; will be written as...
        ///         |||.1|.1.2|.2|.1.2||.1|.1.2|.2|.1.2 ; and will be reduced to...
        ///             => ||.1.2||.1|.1.2|.2|.1.2 ; using wildcard substitution |.1.2 <-> T 
        ///             => ||.1.2||T.1|T.2 ; using wildcard substitution |.1.2 <-> T
        /// Everything else will work the same.
        /// Problem solved.
        /// ### NOTE (1/7/25) : There's a contradiction in the above paragraph.
        /// ### I said that constants are to be removed, then used the wildcard substitution |.1.2 <-> T to reduce a formula.  
        /// ### duh doy.  
        /// 
        /// </summary>
        [TestMethod]
        public async Task SimpleWildcardSwapRequiringUnification()
        {

            //var falseId = await Lucid.GetConstantExpressionIdAsync(false);
            //var subsequentRecord = await Lucid.GetMostlyCanonicalRecordAsync("||.1|T.2|.2|T.1"); 
            //Lucid.Cofactors.Where(_ => _.ExpressionId == subsequentRecord.Id && _.ConclusionId == falseId);

            {
                var trueId = await Lucid.GetConstantExpressionIdAsync(true);
                var falseId = await Lucid.GetConstantExpressionIdAsync(false);
                var nandOneTwo = await GetMostlyCanonicalRecordAsync("|.1.2");
                var ifOneThenTwo = await GetMostlyCanonicalRecordAsync("|.1|T.2");


                // (1 2) should be a tgf-cofactor of some iteration of (1 (T 2))
                {
                    var cofactors = await Lucid.Expressions.Where(_ => _.Id == ifOneThenTwo.Id || _.NextReductionId == ifOneThenTwo.Id || _.CanonicalId == ifOneThenTwo.Id)
                        .Join(Lucid.Cofactors, _ => _.Id, _ => _.ExpressionId, (e, c) => c)
                        .Where(_ => _.SubtermId == nandOneTwo.Id)
                        .Where(_ => _.ReplacementId == falseId)
                        .Where(_ => _.ConclusionId == trueId)
                        .ToArrayAsync();
                    Assert.IsTrue(cofactors.Any());
                }

                // replacing T with F in |.1|T.2 does NOT reduce the expression to T
                {
                    var invalidCofactors = await Lucid.Cofactors
                        .Where(_ => _.ExpressionId == ifOneThenTwo.Id
                                    && _.SubtermId == trueId
                                    && _.ReplacementId == falseId
                                    && _.ConclusionId == trueId)
                        .ToArrayAsync();
                    Assert.IsFalse(invalidCofactors.Any());
                }

                // (1 2) should be a tgf-cofactor of some iteration of ((T 1) 2)
                {
                    var ifNotOneThenNotTwo = await GetMostlyCanonicalRecordAsync("||T.1.2");
                    var cofactors = await Lucid.Expressions.Where(_ => _.Id == ifNotOneThenNotTwo.Id || _.NextReductionId == ifNotOneThenNotTwo.Id || _.CanonicalId == ifNotOneThenNotTwo.Id)
                        .Join(Lucid.Cofactors, _ => _.Id, _ => _.ExpressionId, (e, c) => c)
                        .Where(_ => _.SubtermId == nandOneTwo.Id)
                        .Where(_ => _.ReplacementId == falseId)
                        .Where(_ => _.ConclusionId == trueId)
                        .ToArrayAsync();
                    Assert.IsTrue(cofactors.Any());
                }


                // (1 2) should be a fgf-cofactor of ((1 (T 2)) ((T 1) 2))
                {
                    var subRecord = await GetMostlyCanonicalRecordAsync("||.1|T.2||T.1.2");
                    var canonicalSubRecord = await Lucid.GetCanonicalRecordAsync(subRecord);
                    Assert.AreEqual(subRecord, canonicalSubRecord);
                    Assert.AreEqual(TruthTable.GetTruthTable(subRecord.Formula).ToString(), TruthTable.GetTruthTable(canonicalSubRecord.Formula).ToString());

                    var cofactors = await Lucid.Expressions.Where(_ => _.Id == subRecord.Id || _.NextReductionId == subRecord.Id || _.CanonicalId == subRecord.Id)
                        .Join(Lucid.Cofactors, _ => _.Id, _ => _.ExpressionId, (e, c) => c)
                        .Where(_ => _.SubtermId == nandOneTwo.Id)
                        .Where(_ => _.ReplacementId == falseId)
                        .Where(_ => _.ConclusionId == falseId)
                        .ToArrayAsync();
                    Assert.IsTrue(cofactors.Any());
                }
            }


            var nonCanonicalformula = await GetMostlyCanonicalRecordAsync("|T||.1|T.2||T.1.2"); // id=456
            var canonicalRecord = await GetMostlyCanonicalRecordAsync("||.1.2||T.1|T.2");
            Assert.AreEqual(
                TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(),
                TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
            var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);


            Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
        }

        /// <summary>
        /// This problem is used in [Binary Implication Hypergraphs for the Representation and Simplification of Propositional Formulae, Francès de Mas](pay per view) 
        /// as an example of a simple problem that cant be simplified by any currently known automated simplification method.  
        ///     Transforms...
        ///         A || B => [[TA] || [TB]], 
        ///         A && B => [T[A && B]]
        ///         A -> B => [[TA] -> B]
        ///         ~A => [T~A]
        ///         A<->B  =>  [[AB]<->[[TA][TB]]]
        ///     ---------------
        ///     ( 
        ///         ((A∨B) → (C ↔A))  
        ///         ∧  ((A ↔B) → ((¬A∨B) ∧ C))   
        ///         ∧   
        ///             (  ((A∧B)∨(¬C))  ∧ (B →C) )) 
        ///     ) 
        ///     → (C → B)        
        ///     ---------------
        /// Here's the same thing in RR notation where ¬A∨B => |A|TB, 
        ///     [[T
        ///         [T[
        ///             [T[
        ///                 [[T [[TA]||[TB]] ]→ [[CA]<->[[TC][TA]]] ]
        ///             ∧  
        ///                 [[T 
        ///                     [[AB]<->[[TA][TB]]] 
        ///                 ]→ 
        ///                     [T[ 
        ///                         [[T[T~A]]||[TB]] 
        ///                     && 
        ///                         C
        ///                     ]] 
        ///                 ]
        ///             ]]
        ///         ∧   
        ///             [T[
        ///                 [[T
        ///                     [T[A && B]]]
        ///                 ∨
        ///                     [T~C]
        ///                 ]]
        ///             ∧
        ///                 [[TB]->C]
        ///             ]]
        ///         ]]   
        ///     ]→   
        ///         [[TC] → B]        
        ///     ]
        ///     ---------------
        ///     [[T                             /*IF*/
        ///         [T[                         /*AND*/
        ///             [T[                     /*AND*/
        ///                 [[T [[TA]||[TB]] ] [[CA][[TC][TA]]] ] /*IF A or B THEN C eq A*/
        ///                 [[T [[AB][[TA][TB]]] ] [T[ [[T[TA]][TB]] C ]] ] /* IF A eq B THEN ((NOT A OR B) AND C)
        ///             ]]                      /*ENDAND*/
        ///             [T[                     /*AND*/
        ///                 [[T[AB]] [T[TC]]]   /*A and B or NOT C*/
        ///                 [[TB]C]             /*B->C*/
        ///             ]]                      /*ENDAND*/
        ///         ]]                          /*ENDAND*/
        ///     ]                               /*THEN*/
        ///         [[TC]B]                     /*C->B*/
        ///     ]                               /*ENDIF*/
        ///     ---------------
        ///     Reduce....
        ///                 [T[TC]] => C
        ///             [[[T[AB]] C] [[TB]C]] => [C [[T[AB]][TB]]] ; reduce to 'shorter' formula that comes first in expression order
        ///             [[T[AB]][TB]] => T
        ///             [T[TC]] => C
        ///             [T[TA]] => A
        ///         remove C in...        
        ///         [T[                         
        ///             [T[                     
        ///                 [[T [[TA]||[TB]] ] [[CA][[TC][TA]]] ] 
        ///                 [[T [[AB][[TA][TB]]] ] [T[ [A[TB]] C ]] ] 
        ///             ]]                      
        ///             C                      
        ///         ]]                          
        ///         to get...
        ///         [T[                         
        ///             [[T [[AB][[TA][TB]]] ] [T[ [A[TB]] T ]] ] 
        ///             C                      
        ///         ]]                          
        ///         [ [T[[AB][[TA][TB]]]] [A[TB]] ] => [[A [TB]] [B [TA]]]
        ///     ====>
        ///     [
        ///         [                         
        ///             [[A [TB]] [B [TA]]]
        ///             C                      
        ///         ]                               
        ///         [[TC]B]                     
        ///     ]                               
        ///     ====>
        ///     [
        ///         [                         
        ///             [[A [CB]] [B [CA]]]
        ///             C                      
        ///         ]                               
        ///         [[TC]B]                     
        ///     ]                               
        ///     ====>
        ///     [
        ///         [                         
        ///             [T[A[TB]]]
        ///             C                      
        ///         ]                               
        ///         [[TC]B]                     
        ///     ]                               
        ///     
        ///     ---------------
        ///     To solve, lets assume that the two top-most sub-expressions are canonical, as would be the case in RR.  
        ///     RR would go about performing wildcard analysis for the common variables B and C, and stopping when a reduction opportunity is discovered.  
        ///     Note that RR is using *abductive reasoning*, basically making an educated guess.  
        ///     Using BIHs is not abductive reasoning, it's deductive reasoning, and it's much more efficient.  
        ///     Using BIHs 
        ///     
        /// </summary>
        [TestMethod]
        public async Task BIHSchoolProblem()
        {
            var nonCanonicalformula = await Lucid.GetMostlyCanonicalRecordAsync("|T||.1|T.2|.2|T.1"); // id=456
            var canonicalRecord = await Lucid.GetMostlyCanonicalRecordAsync("||.1.2||T.1|T.2");
            Assert.AreEqual(TruthTable.GetTruthTable(nonCanonicalformula.Formula).ToString(), TruthTable.GetTruthTable(canonicalRecord.Formula).ToString());
            var reducedRecord = await Lucid.GetCanonicalRecordAsync(nonCanonicalformula);
            Assert.AreEqual(canonicalRecord.Formula, reducedRecord.Formula);
        }
    }
}
