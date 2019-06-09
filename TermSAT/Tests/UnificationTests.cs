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

        public void testUnification()
        {
            Formula one = Formula.CreateFormula("*1.T");
            Formula two = Formula.CreateFormula("*T2.");
            Map<Variable, Formula> unification = Formula.unify(one, two);
            assertNotNull("A unifying substitution should have been found",
                    unification);
            Formula expectedFormula = Formula.CreateFormula("*TT");
            Formula unifyingInstance = Formula.createInstance(one, unification);
            Assert.Equals("Incorrect unification", expectedFormula, unifyingInstance);
            unifyingInstance = Formula.createInstance(two, unification);
            Assert.Equals("Incorrect unification", expectedFormula, unifyingInstance);

            one = Formula.CreateFormula("*1.2.");
            two = Formula.CreateFormula("*-3.3.");
            expectedFormula = Formula.CreateFormula("*-3.3.");
            unification = Formula.unify(one, two);
            assertNotNull("A unifying substitution should have been found",
                    unification);
            unifyingInstance = Formula.createInstance(one, unification);
            Assert.Equals("Incorrect unification", expectedFormula, unifyingInstance);
            unifyingInstance = Formula.createInstance(two, unification);
            Assert.Equals("Incorrect unification", expectedFormula, unifyingInstance);

            one = Formula.CreateFormula("**1.2.*1.3.");
            two = Formula.CreateFormula("**4.*5.6.6.");
            expectedFormula = Formula.CreateFormula("**4.*5.*4.3.*4.3.");
            unification = Formula.unify(one, two);
            assertNotNull("A unifying substitution should have been found",
                    unification);
            unifyingInstance = Formula.createInstance(one, unification);
            Assert.Equals("Incorrect unification", expectedFormula, unifyingInstance);
            unifyingInstance = Formula.createInstance(two, unification);
            Assert.Equals("Incorrect unification", expectedFormula, unifyingInstance);

            // this example used to cause Formula.createFormula to lock up
            one = Formula.CreateFormula("*2.*1.2.");
            two = Formula.CreateFormula("*-*-*2.*1.2.*1.-*2.*1.2.*1.-*2.*1.2.");
            unification = Formula.unify(one, two);
            assertNull("A unifying substitution should not have been found",
                    unification);

            // this example used to cause Formula.createFormula to lock up
            one = Formula.CreateFormula("***1.2.-*2.1.3.");
            two = Formula.CreateFormula("****1.2.-*2.1.3.-*-*3.1.2.");
            unification = Formula.unify(one, two);
            assertNull("A unifying substitution should not have been found",
                    unification);

            one = Formula.CreateFormula("T");
            two = Formula.CreateFormula("****1.2.-*2.1.3.-*-*3.1.2.");
            unification = Formula.unify(one, two);
            assertNull("A unifying substitution should not have been found",
                    unification);

            // this example is from Figure 10 in
            // http://comjnl.oxfordjournals.org/content/34/1/2.full.pdf
            one = Formula.CreateFormula("**1.2.3.");
            two = Formula.CreateFormula("*-1.1.");
            unification = Formula.unify(one, two);
            assertNull("A unifying substitution should not have been found",
                    unification);
        }

    }

}

