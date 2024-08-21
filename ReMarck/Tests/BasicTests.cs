using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using TermMark.System;
using ReMarck.System;

namespace TermMark.Tests
{
    public class BasicTests
    {
        /// <summary>
        /// I wrote this test while designing the API.
        /// </summary>
        public void SingleReplaceRule()
        {
            var repository= TripletRepository.FromFormula("*.1*.2.1");

            var system= new MSystem(repository);
            system.ApplyReductionRules();

            var roots= repository.Roots.ToList();
            Assert.AreEqual(1, roots.Count);

            var reducedFormula= roots[0].ToFormulaString();
            Assert.AreEqual("T", reducedFormula);
        }
    }
}
