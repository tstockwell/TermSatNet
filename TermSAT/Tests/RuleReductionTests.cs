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
    public class RuleReductionTests
    {


        public void testRuleReduction()
        {
            RuleRepository rules = new RuleRepository();
            Solver solver = new Solver(rules);

            Formula formula = Formula.CreateFormula("*1.T");
            assertTrue(rules.containsKey("*1.T"));
            SubstitutionInstance match = rules.findFirstMatch(formula);
            assertNotNull(match);
            Formula reduced = rules.findReducedFormula(formula);
            Assert.Equals(Constant.TRUE, reduced);

            formula = Formula.CreateFormula("*6.T");
            match = rules.findFirstMatch(formula);
            assertNotNull(match);
            reduced = rules.findReducedFormula(formula);
            Assert.Equals(Constant.TRUE, reduced);

            formula = Formula.CreateFormula("*T2.");
            reduced = solver.reduce(formula);
            Assert.Equals(Formula.CreateFormula("2."), reduced);

            formula = Formula.CreateFormula("*F-2.");
            reduced = solver.reduce(formula);
            Assert.Equals(Formula.CreateFormula("T"), reduced);

            formula = Formula.CreateFormula("**T2.-*F-2.");
            reduced = solver.reduce(formula);
            Assert.Equals(Formula.CreateFormula("-2."), reduced);

            formula = Formula.CreateFormula("-**F2.-*2.F");
            reduced = solver.reduce(formula);
            Assert.Equals(Formula.CreateFormula("-2."), reduced);

            // prove that **-3.2.2. is equivalent to *3.2.
            formula = Formula.CreateFormula("**-3.2.2.");
            Formula target = Formula.CreateFormula("*3.2.");
            Assert.Equals(formula.getTruthTable(), target.getTruthTable());
            // now test that **-3.2.2. reduces to *3.2.
            reduced = solver.reduce(formula);
            Assert.Equals(target, reduced);


            formula = Formula.CreateFormula("*-*-3.2.3.");
            target = Formula.CreateFormula("*-2.3.");
            Assert.Equals(formula.getTruthTable(), target.getTruthTable());
            reduced = solver.reduce(formula);
            Assert.Equals(reduced.getTruthTable(), target.getTruthTable());

            formula = Formula.CreateFormula("*1.F");
            ReductionRule reductionRule = new ReductionRule(
                    Formula.CreateFormula("*FF"),
                    Formula.CreateFormula("T"));
            reduced = Formula.reduceUsingRule(formula, reductionRule);
            assertNull("Formula was reduced when it shouldn't be", reduced);

            // at one time there was a bug in the solver where calling the solver 
            // twice with the same formula did not return the same formula 
            formula = Formula.CreateFormula("*-1.F");
            Formula reduced1 = solver.reduce(formula);
            Formula reduced2 = solver.reduce(formula);
            Assert.Equals(reduced1, reduced2);


        }

        public void testSyntaxEquality()
        {
            Formula one = Formula.CreateFormula("-2.");
            Formula two = Formula.CreateFormula("-2.");
            assertTrue("Identical formulas not recognised",
                    Formula.syntacticallyEqual(one, two));

            one = Formula.CreateFormula("-2.");
            two = Formula.CreateFormula("-1.");
            assertTrue("Identical formulas not recognised",
                    Formula.syntacticallyEqual(one, two));

            one = Formula.CreateFormula("-2.");
            two = Formula.CreateFormula("-*1.3.");
            assertFalse("Non-identical formulas not recognised",
                    Formula.syntacticallyEqual(one, two));
        }


    }

}

