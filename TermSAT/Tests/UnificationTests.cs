/*******************************************************************************
 *     termsat SAT solver
 *     Copyright (C) 2019 Ted Stockwell <emorning@yahoo.com>
 * 
 *     This program is free software: you can redistribute it and/or modify
 *     it under the terms of the GNU Affero General Public License as
 *     published by the Free Software Foundation, either version 3 of the
 *     License, or (at your option) any later version.
 * 
 *     This program is distributed in the hope that it will be useful,
 *     but WITHOUT ANY WARRANTY; without even the implied warranty of
 *     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *     GNU Affero General Public License for more details.
 * 
 *     You should have received a copy of the GNU Affero General Public License
 *     along with this program.  If not, see <http://www.gnu.org/licenses/>.
 ******************************************************************************/
using TermSAT.Formulas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace TermSAT.Tests
{
    [TestClass]
    public class UnificationTests
    {
        [TestMethod]
        public void TestUnification()
        {
            Formula one = "*1.T";
            Formula two = "*T2.";
            var unification = Formula.Unify(one, two);
            Assert.IsNotNull(unification, "A unifying substitution should have been found");
            Formula expectedFormula = "*TT";
            Formula unifyingInstance = one.CreateSubstitutionInstance(unification);
            Assert.AreEqual(expectedFormula, unifyingInstance, "Incorrect unification");
            unifyingInstance = two.CreateSubstitutionInstance(unification);
            Assert.AreEqual(expectedFormula, unifyingInstance, "Incorrect unification");

            one = "*1.2.";
            two = "*-3.3.";
            expectedFormula = "*-3.3.";
            unification = Formula.Unify(one, two);
            Assert.IsNotNull(unification, "A unifying substitution should have been found");
            unifyingInstance = one.CreateSubstitutionInstance(unification);
            Assert.AreEqual(expectedFormula, unifyingInstance, "Incorrect unification");
            unifyingInstance = two.CreateSubstitutionInstance(unification);
            Assert.AreEqual(expectedFormula, unifyingInstance, "Incorrect unification");

            one = "**1.2.*1.3.";
            two = "**4.*5.6.6.";
            expectedFormula = "**4.*5.*4.3.*4.3.";
            unification = Formula.Unify(one, two);
            Assert.IsNotNull(unification, "A unifying substitution should have been found");
                   
            unifyingInstance = one.CreateSubstitutionInstance(unification);
            Assert.AreEqual(expectedFormula, unifyingInstance, "Incorrect unification");
            unifyingInstance = two.CreateSubstitutionInstance(unification);
            Assert.AreEqual(expectedFormula, unifyingInstance, "Incorrect unification");

            // this example used to cause Formula.createFormula to lock up
            one = "*2.*1.2.";
            two = "*-*-*2.*1.2.*1.-*2.*1.2.*1.-*2.*1.2.";
            unification = Formula.Unify(one, two);
            Assert.IsNull(unification, "A unifying substitution should not have been found");

            // this example used to cause Formula.createFormula to lock up
            one = "***1.2.-*2.1.3.";
            two = "****1.2.-*2.1.3.-*-*3.1.2.";
            unification = Formula.Unify(one, two);
            Assert.IsNull(unification, "A unifying substitution should not have been found");

            one = "T";
            two = "****1.2.-*2.1.3.-*-*3.1.2.";
            unification = Formula.Unify(one, two);
            Assert.IsNull(unification, "A unifying substitution should not have been found");

            // this example is from Figure 10 in
            // http://comjnl.oxfordjournals.org/content/34/1/2.full.pdf
            one = "**1.2.3.";
            two = "*-1.1.";
            unification = Formula.Unify(one, two);
            Assert.IsNull(unification, "A unifying substitution should not have been found");
        }

    }

}

