using System.Threading.Tasks;
using System.CommandLine;
using TermSAT.RuleDatabase;
using System.Diagnostics;
using TermSAT.NandReduction;

namespace TermSAT
{
    public static class Program
    {
        static async Task<int> Main(string[] args)
        {
            Trace.Listeners.Add(new TextWriterTraceListener(System.Console.Out));

            var rootCommand = new RootCommand("TermSAT");

            AddRuleGeneration3Command(rootCommand);
            AddOrderingTestCommand(rootCommand);
            AddSchemeEquivalenceCommand(rootCommand);
            AddNandRuleGeneration3Command(rootCommand);
            AddDiscoverRulesSubsumedBySchemeCommand(rootCommand);

            return await rootCommand.InvokeAsync(args);
        }

        private static void AddNandRuleGeneration3Command(RootCommand rootCommand)
        {
            var schemeEquivalenceTest = new Command("nand-rule-generation-3");
            schemeEquivalenceTest.SetHandler(async () =>
            {
                Trace.Listeners.Add(new TextWriterTraceListener("nand-rule-generation-3-trace.log"));

                var database = new FormulaDatabase("nand-rules-3.db");
                NandReduction.Scripts.RunNandRuleGenerator(database);
                //var generator = new NandFormulaGenerator(database,3);
                //new RuleGenerator(database, generator).Run();
            });
            rootCommand.AddCommand(schemeEquivalenceTest);
        }
        private static void AddDiscoverRulesSubsumedBySchemeCommand(RootCommand rootCommand)
        {
            var schemeEquivalenceTest = new Command("nand-subsumed-rules");
            schemeEquivalenceTest.SetHandler(async () =>
            {
                Trace.Listeners.Add(new TextWriterTraceListener("nand-subsumed-rules.log"));

                var database = new FormulaDatabase("nand-rules-3.db");
                await NandReduction.Scripts.DiscoverRulesSubsumedBySchemeAsync(database);
            });
            rootCommand.AddCommand(schemeEquivalenceTest);
        }

        private static void AddRuleGeneration3Command(RootCommand rootCommand)
        {
            var schemeEquivalenceTest = new Command("rule-generation-3");
            schemeEquivalenceTest.SetHandler(() =>
            {
                Trace.Listeners.Add(new TextWriterTraceListener("rules-trace-3.log"));

                var database = new FormulaDatabase("rules-3.db");
                database.Clear();
                var generator = new FormulaGenerator(database, 3);
                new RuleGenerator(database, generator).Run();
            });
            rootCommand.AddCommand(schemeEquivalenceTest);
        }


        private static void AddOrderingTestCommand(RootCommand rootCommand)
        {
            var cmd = new Command("ordering-test");
            cmd.SetHandler(() =>
            {
                Trace.Listeners.Add(new TextWriterTraceListener("ordering-test.log"));

                var database = new FormulaDatabase("rules-3.db");
                SchemeReducer.Scripts.OrderingTest(database);
            });
            rootCommand.AddCommand(cmd);
        }

        private static void AddSchemeEquivalenceCommand(RootCommand rootCommand)
        {
            var schemeEquivalenceTest = new Command("scheme-equivalence-test");
            schemeEquivalenceTest.SetHandler(() =>
            {
                Trace.Listeners.Add(new TextWriterTraceListener("scheme-equivalence-test.log"));

                var database = new FormulaDatabase("rules-3.db");
                SchemeReducer.Scripts.BasicSchemeEquivalence(database);
            });
            rootCommand.AddCommand(schemeEquivalenceTest);
        }
    }

    public static class Options
    {

    }
}
