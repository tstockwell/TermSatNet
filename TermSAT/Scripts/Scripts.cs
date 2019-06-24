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
        [TestMethod]
        public void RunRuleGenerator()
        {
            new RuleGenerator().Run();
        }
    }
}
