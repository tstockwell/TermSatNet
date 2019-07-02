using System;
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

            var database = new FormulaDatabase(DATABASE_PATH);
            database.Clear();
            new RuleGenerator(database).Run();
        }


    }

}
