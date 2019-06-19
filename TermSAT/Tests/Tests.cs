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
    public class FormulaTests
    {


        [TestMethod]
        public void testPrettyPrinter()
        {
            Assert.Equals("*.1.2", PrettyFormula.ToFormulaString("(1->2)"));
            Assert.Equals("*.1-.2", PrettyFormula.ToFormulaString("(1->~2)"));
            Assert.Equals("*.1-.2", PrettyFormula.ToFormulaString("(1 -> ~2)"));
            Assert.Equals("*.1T", PrettyFormula.ToFormulaString("(1 -> T)"));
            Assert.Equals("*.1*.1-.2.", PrettyFormula.ToFormulaString("(1 -> (1 -> ~2))"));

            Assert.Equals("(1->(1->~2))", PrettyFormula.ToPrettyString("*1.*1.-2."));
        }

        [TestMethod]
        public void testFormulaConstruction()
        {
            Assert.Equals(".1", Variable.newVariable(1).ToString());

            Assert.Equals("*.1.1", Formula.CreateFormula("*.1.1").ToString());

            var text = "***.1.2.3.4";
            Formula formula1 = Formula.CreateFormula(text);
            Assert.Equals(formula1.Length, text.Length);
            Assert.Equals(formula1.ToString(), text);

            text = "*" + text + text;
            Formula formula2 = Implication.newImplication(formula1, formula1);
            Assert.Equals(formula2.Length, text.Length);
            Assert.Equals(formula2.ToString(), text);

            text = "-" + text;
            Formula formula3 = Negation.newNegation(formula2);
            Assert.Equals(formula3.Length, text.Length);
            Assert.Equals(formula3.ToString(), text);

            Formula formula4 = Variable.newVariable(23);
            Assert.Equals(formula4.ToString(), ".23");

        }

        [TestMethod]
        public void testEvaluation()
        {
            Variable one = Variable.newVariable(1);
            Variable two = Variable.newVariable(2);
            Formula formula1 = Implication.newImplication(one, two);
            Formula formula2 = Negation.newNegation(formula1);
            for (int a = 0; a <= 1; a++)
            {
                for (int b = 0; b <= 1; b++)
                {
                    var valuation = new Dictionary<Variable, Boolean>();
                    valuation.Add(one, (a == 1 ? true : false));
                    valuation.Add(two, (b == 1 ? true : false));
                    var value1 = formula1.Evaluate(valuation);
                    Assert.AreEqual(a != 1 || b != 0, value1, "formula evaluation failed");
                    var value2 = formula2.Evaluate(valuation);
                    Assert.AreEqual(!value1, value2, "formula evaluation failed");
                }
            }
        }

        [TestMethod]
        async public void testSubstitutions()
        {
            String text = "***.1.2.3.4";
            Formula formula1 = Formula.CreateFormula(text);

            var substitutions = new Dictionary<Variable, Formula>();
            substitutions.Add(Variable.newVariable(1), Variable.newVariable(5));
            substitutions.Add(Variable.newVariable(2), Variable.newVariable(6));
            substitutions.Add(Variable.newVariable(3), Variable.newVariable(7));
            substitutions.Add(Variable.newVariable(4), Variable.newVariable(8));
            Formula instance = await formula1.CreateSubstitutionInstance(substitutions);

            Assert.Equals("***.5.6.7.8", instance.ToString());

        }

        [TestMethod]
        public void testInstanceRecognizer()
        {
            String text = "***1.2.3.4.";
            Formula formula1 = Formula.CreateFormula(text);
            Formula formula2 = Implication.newImplication(formula1, formula1);

            InstanceRecognizer recognizer = new InstanceRecognizer();
            Formula rule1 = Formula.CreateFormula("*1.2.");
            recognizer.Add(rule1);
            Assert.Equals(1, recognizer.findAllGeneralizations(rule1).Count);
            Assert.Equals(1, recognizer.findAllGeneralizations(formula1).Count);
            Assert.Equals(1, recognizer.findAllGeneralizations(formula2).Count);

            SubstitutionInstance match = recognizer.findFirstGeneralization(formula2);
            Assert.IsNotNull(match.Substitutions);
            Assert.Equals(2, match.Substitutions.Count);

            recognizer.Add(Formula.CreateFormula("*1.1."));
            Assert.Equals(2, recognizer.findAllGeneralizations(formula2).Count);

            InstanceRecognizer recognizer2 = new InstanceRecognizer();
            recognizer2.Add(Formula.CreateFormula("**1.2.*3.4."));
            Assert.Equals(1, recognizer2.findAllGeneralizations(formula2).Count);

            recognizer.Add(Formula.CreateFormula("**1.2.*3.4."));
            Assert.Equals(3, recognizer.findAllGeneralizations(formula2).Count);
            recognizer.Add(Formula.CreateFormula("**1.2.*3.2."));
            Assert.Equals(4, recognizer.findAllGeneralizations(formula2).Count);
            recognizer.Add(Formula.CreateFormula("**1.2.*1.4."));
            Assert.Equals(5, recognizer.findAllGeneralizations(formula2).Count);
            recognizer.Add(Formula.CreateFormula("**1.2.*1.2."));
            Assert.Equals(6, recognizer.findAllGeneralizations(formula2).Count);

            Formula formula3 = Negation.newNegation(formula2);
            recognizer.Add(Formula.CreateFormula("-1."));
            Assert.Equals(1, recognizer.findAllGeneralizations(formula3).Count);

            recognizer = new InstanceRecognizer();
            recognizer.Add(Formula.CreateFormula("*1.T"));
            Assert.Equals(1, recognizer
                    .findAllGeneralizations(Formula.CreateFormula("*1.T")).Count);

            recognizer = new InstanceRecognizer();
            recognizer.Add(Formula.CreateFormula("*1.1."));
            Assert.Equals(0,
                    recognizer.findAllGeneralizations(Formula.CreateFormula("*1.2."))
                            .Count);
        }

    }

}

