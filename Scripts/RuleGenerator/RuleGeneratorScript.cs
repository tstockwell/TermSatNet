using System;
using System.Diagnostics;
using TermSAT.RuleDatabase;

namespace RuleGeneratorScript
{
    public class RuleGeneratorScript
    {
        static readonly string DATABASE_PATH = "rules-" + TruthTable.VARIABLE_COUNT + ".db";


        // do this to create memory-based db
        //readonly string DATABASE_PATH = ":memory:"; 


        public static void Main(string[] args)
        {
            throw new NotImplementedException();
            //Trace.Listeners.Add(new TextWriterTraceListener(System.Console.Out));
            //Trace.Listeners.Add(new TextWriterTraceListener("rules-trace-" + TruthTable.VARIABLE_COUNT + ".txt"));

            //var database = new FormulaDatabase(DATABASE_PATH);
            //database.Clear();
            //new RuleGenerator(database, new FormulaGenerator(database, TruthTable.VARIABLE_COUNT)).Run();
        }


    }

}
