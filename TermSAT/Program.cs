using System.Threading.Tasks;
using System.CommandLine;
using TermSAT.RuleDatabase;
using System.Diagnostics;
using TermSAT.NandReduction;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace TermSAT
{
    public class TermSATTraceListener : TraceListener
    {
        Task continuation = Task.CompletedTask;
        TextWriter TextWriter {  get; }
        public TermSATTraceListener(TextWriter textWriter)
        {
            TextWriter = textWriter;
        }
        public override void Write(string message)
        {
            continuation.ContinueWith(async _ => await TextWriter.WriteAsync(message));
        }

        public override void WriteLine(string message)
        {
            continuation.ContinueWith(async _ => await TextWriter.WriteLineAsync(message));
        }
    }
    public static class Program
    {
        static async Task<int> Main(string[] args)
        {
            Trace.Listeners.Add(new TermSATTraceListener(System.Console.Out));

            var rootCommand = new RootCommand("TermSAT");

            //AddRuleGeneration3Command(rootCommand);
            //AddOrderingTestCommand(rootCommand);
            //AddSchemeEquivalenceCommand(rootCommand);
            AddNandRuleGeneration3Command(rootCommand);
            AddDiscoverRulesSubsumedBySchemeCommand(rootCommand);

            return await rootCommand.InvokeAsync(args);
        }

        private static void AddNandRuleGeneration3Command(RootCommand rootCommand)
        {
            var schemeEquivalenceTest = new Command("nand-rule-generation-3");
            schemeEquivalenceTest.SetHandler(async () =>
            {
                //ServiceProvider serviceProvider = new ServiceCollection()
                //    .AddLogging((loggingBuilder) => loggingBuilder
                //        .SetMinimumLevel(LogLevel.Trace)
                //        .AddConsole())
                //    .BuildServiceProvider();
                //Trace.Listeners.Add(new TextWriterTraceListener("nand-rule-generation-3-trace.log"));

                await TermSAT.NandReduction.Scripts.RunNandRuleGenerator("nand-rules-3.db", "nand-rules-index.db");
                //var database = new FormulaDatabase("nand-rules-3.db");
                //TermSAT.NandReduction.Scripts_RuleGenerator_KnuthBendix.RunNandRuleGenerator(database);
            });
            rootCommand.AddCommand(schemeEquivalenceTest);
        }
        private static void AddDiscoverRulesSubsumedBySchemeCommand(RootCommand rootCommand)
        {
            var schemeEquivalenceTest = new Command("nand-subsumed-rules");
            schemeEquivalenceTest.SetHandler(async () =>
            {
                Trace.Listeners.Add(new TextWriterTraceListener("nand-subsumed-rules.log"));
                await NandReduction.Scripts.DiscoverRulesSubsumedBySchemeAsync("nand-rules-3.db");
            });
            rootCommand.AddCommand(schemeEquivalenceTest);
        }

        //private static void AddRuleGeneration3Command(RootCommand rootCommand)
        //{
        //    var schemeEquivalenceTest = new Command("rule-generation-3");
        //    schemeEquivalenceTest.SetHandler(() =>
        //    {
        //        Trace.Listeners.Add(new TextWriterTraceListener("rules-trace-3.log"));

        //        // recreate the database
        //        using (var ctx = RuleDatabaseContext.GetDatabaseContext("rules-3.db"))
        //        {
        //            ctx.Database.EnsureDeleted();
        //            ctx.Database.EnsureCreated();
        //        }

        //        var ruleDb = new FormulaDatabase("rules-3.db");
        //        var generator = new FormulaGenerator(ruleDb, 3);
        //        new RuleGenerator(ruleDb, generator).Run();
        //    });
        //    rootCommand.AddCommand(schemeEquivalenceTest);
        //}


        //private static void AddOrderingTestCommand(RootCommand rootCommand)
        //{
        //    var cmd = new Command("ordering-test");
        //    cmd.SetHandler(() =>
        //    {
        //        Trace.Listeners.Add(new TextWriterTraceListener("ordering-test.log"));

        //        var database = new FormulaDatabase("rules-3.db");
        //        SchemeReducer.Scripts.OrderingTest(database);
        //    });
        //    rootCommand.AddCommand(cmd);
        //}

        //private static void AddSchemeEquivalenceCommand(RootCommand rootCommand)
        //{
        //    var schemeEquivalenceTest = new Command("scheme-equivalence-test");
        //    schemeEquivalenceTest.SetHandler(() =>
        //    {
        //        Trace.Listeners.Add(new TextWriterTraceListener("scheme-equivalence-test.log"));

        //        var database = new FormulaDatabase("rules-3.db");
        //        SchemeReducer.Scripts.BasicSchemeEquivalence(database);
        //    });
        //    rootCommand.AddCommand(schemeEquivalenceTest);
        //}
    }

    public static class Options
    {

    }
}
