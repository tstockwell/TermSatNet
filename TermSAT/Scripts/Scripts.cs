using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using TermSAT.RuleDatabase;

namespace TermSAT.Scripts
{
    /// <summary>
    /// I think it's more convenient to run scripts and such from the test framework than 
    /// creating  a new project for each script.
    /// </summary>
    [TestClass, TestCategory("Scripts")]
    public class Scripts
    {
        readonly string DATABASE_PATH= "rules-"+TruthTable.VARIABLE_COUNT+".db";

        // do this to create memory-based db
        //readonly string DATABASE_PATH = ":memory:"; 

        [TestMethod]
        public void RunRuleGenerator()
        {
            var database = new FormulaDatabase(DATABASE_PATH);
            database.Clear();
            new RuleGenerator(database, new FormulaGenerator(database)).Run();
        }

        [TestMethod]
        public void RunRuleReport()
        {
            var database = new FormulaDatabase(DATABASE_PATH);
            var options = new DatabaseReport.DatabaseReportOptions();
            if (TruthTable.VARIABLE_COUNT <= 2)
                options.ShowNonCanonicalFormulas = true;
            new DatabaseReport(database, options).Run();
        }

        [TestMethod]
        public void RunRuleReport_ShowReductionRules()
        {
            var database = new FormulaDatabase(DATABASE_PATH);
            var options = new DatabaseReport.DatabaseReportOptions() { ShowReductionRules = true };
            new DatabaseReport(database, options).Run();
        }
    }
}
