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

namespace TermSAT.Tests
{
    [TestClass]
    public class UnificationTests
    {
        [TestMethod]
        async public void testUnification()
        {
            Formula one = Formula.CreateFormula("*1.T");
            Formula two = Formula.CreateFormula("*T2.");
            var unification = await Formula.Unify(one, two);
            Assert.IsNotNull(unification, "A unifying substitution should have been found");
            Formula expectedFormula = Formula.CreateFormula("*TT");
            Formula unifyingInstance = await one.CreateSubstitutionInstance(unification);
            Assert.AreEqual(expectedFormula, unifyingInstance, "Incorrect unification");
            unifyingInstance = await two.CreateSubstitutionInstance(unification);
            Assert.AreEqual(expectedFormula, unifyingInstance, "Incorrect unification");

            one = Formula.CreateFormula("*1.2.");
            two = Formula.CreateFormula("*-3.3.");
            expectedFormula = Formula.CreateFormula("*-3.3.");
            unification = await Formula.Unify(one, two);
            Assert.IsNotNull(unification, "A unifying substitution should have been found");
            unifyingInstance = await one.CreateSubstitutionInstance(unification);
            Assert.AreEqual(expectedFormula, unifyingInstance, "Incorrect unification");
            unifyingInstance = await two.CreateSubstitutionInstance(unification);
            Assert.AreEqual(expectedFormula, unifyingInstance, "Incorrect unification");

            one = Formula.CreateFormula("**1.2.*1.3.");
            two = Formula.CreateFormula("**4.*5.6.6.");
            expectedFormula = Formula.CreateFormula("**4.*5.*4.3.*4.3.");
            unification = await Formula.Unify(one, two);
            Assert.IsNotNull(unification, "A unifying substitution should have been found");
                   
            unifyingInstance = await one.CreateSubstitutionInstance(unification);
            Assert.AreEqual(expectedFormula, unifyingInstance, "Incorrect unification");
            unifyingInstance = await two.CreateSubstitutionInstance(unification);
            Assert.AreEqual(expectedFormula, unifyingInstance, "Incorrect unification");

            // this example used to cause Formula.createFormula to lock up
            one = Formula.CreateFormula("*2.*1.2.");
            two = Formula.CreateFormula("*-*-*2.*1.2.*1.-*2.*1.2.*1.-*2.*1.2.");
            unification = await Formula.Unify(one, two);
            Assert.IsNull(unification, "A unifying substitution should not have been found");

            // this example used to cause Formula.createFormula to lock up
            one = Formula.CreateFormula("***1.2.-*2.1.3.");
            two = Formula.CreateFormula("****1.2.-*2.1.3.-*-*3.1.2.");
            unification = await Formula.Unify(one, two);
            Assert.IsNull(unification, "A unifying substitution should not have been found");

            one = Formula.CreateFormula("T");
            two = Formula.CreateFormula("****1.2.-*2.1.3.-*-*3.1.2.");
            unification = await Formula.Unify(one, two);
            Assert.IsNull(unification, "A unifying substitution should not have been found");

            // this example is from Figure 10 in
            // http://comjnl.oxfordjournals.org/content/34/1/2.full.pdf
            one = Formula.CreateFormula("**1.2.3.");
            two = Formula.CreateFormula("*-1.1.");
            unification = await Formula.Unify(one, two);
            Assert.IsNull(unification, "A unifying substitution should not have been found");
        }

    }

}

