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
            Assert.Equals("*#1#2", PrettyFormula.getFormulaText("(1->2)"));
            Assert.Equals("*#1-#2", PrettyFormula.getFormulaText("(1->~2)"));
            Assert.Equals("*#1-#2", PrettyFormula.getFormulaText("(1 -> ~2)"));
            Assert.Equals("*#1T", PrettyFormula.getFormulaText("(1 -> T)"));
            Assert.Equals("*#1*#1-#2.", PrettyFormula.getFormulaText("(1 -> (1 -> ~2))"));

            Assert.Equals("(1->(1->~2))", PrettyFormula.getPrettyText("*1.*1.-2."));
        }

        public void testFormulaConstruction()
        {
            Assert.Equals("#1", Variable.newVariable(1).ToString());

            Assert.Equals("*#1#1", Formula.CreateFormula("*#1#1").ToString());

            var text = "***#1#2#3#4";
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
            Assert.Equals(formula4.ToString(), "#23");

        }

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
                    var value1 = formula1.evaluate(valuation);
                    Assert.AreEqual(a != 1 || b != 0, value1, "formula evaluation failed");
                    var value2 = formula2.evaluate(valuation);
                    Assert.AreEqual(!value1, value2, "formula evaluation failed");
                }
            }
        }

        public void testSubstitutions()
        {
            String text = "***.1.2.3.4";
            Formula formula1 = Formula.CreateFormula(text);

            var substitutions = new Dictionary<Variable, Formula>();
            substitutions.Add(Variable.newVariable(1), Variable.newVariable(5));
            substitutions.Add(Variable.newVariable(2), Variable.newVariable(6));
            substitutions.Add(Variable.newVariable(3), Variable.newVariable(7));
            substitutions.Add(Variable.newVariable(4), Variable.newVariable(8));
            Formula instance = Formula.createInstance(formula1, substitutions);

            Assert.Equals("***.5.6.7.8", instance.ToString());

        }

        public void testInstanceRecognizer()
        {
            String text = "***1.2.3.4.";
            Formula formula1 = Formula.CreateFormula(text);
            Formula formula2 = Implication.newImplication(formula1, formula1);

            InstanceRecognizer recognizer = new InstanceRecognizer();
            Formula rule1 = Formula.CreateFormula("*1.2.");
            recognizer.addFormula(rule1);
            Assert.Equals(1, recognizer.findAllMatches(rule1).size());
            Assert.Equals(1, recognizer.findAllMatches(formula1).size());
            Assert.Equals(1, recognizer.findAllMatches(formula2).size());

            SubstitutionInstance match = recognizer.findFirstMatch(formula2);
            Assert.IsNotNull(match.Substitutions);
            Assert.Equals(2, match.Substitutions.Count);

            recognizer.addFormula(Formula.CreateFormula("*1.1."));
            Assert.Equals(2, recognizer.findAllMatches(formula2).size());

            InstanceRecognizer recognizer2 = new InstanceRecognizer();
            recognizer2.addFormula(Formula.CreateFormula("**1.2.*3.4."));
            Assert.Equals(1, recognizer2.findAllMatches(formula2).size());

            recognizer.addFormula(Formula.CreateFormula("**1.2.*3.4."));
            Assert.Equals(3, recognizer.findAllMatches(formula2).size());
            recognizer.addFormula(Formula.CreateFormula("**1.2.*3.2."));
            Assert.Equals(4, recognizer.findAllMatches(formula2).size());
            recognizer.addFormula(Formula.CreateFormula("**1.2.*1.4."));
            Assert.Equals(5, recognizer.findAllMatches(formula2).size());
            recognizer.addFormula(Formula.CreateFormula("**1.2.*1.2."));
            Assert.Equals(6, recognizer.findAllMatches(formula2).size());

            Formula formula3 = Negation.newNegation(formula2);
            recognizer.addFormula(Formula.CreateFormula("-1."));
            Assert.Equals(1, recognizer.findAllMatches(formula3).size());

            recognizer = new InstanceRecognizer();
            recognizer.addFormula(Formula.CreateFormula("*1.T"));
            Assert.Equals(1, recognizer
                    .findAllMatches(Formula.CreateFormula("*1.T")).size());

            recognizer = new InstanceRecognizer();
            recognizer.addFormula(Formula.CreateFormula("*1.1."));
            Assert.Equals(0,
                    recognizer.findAllMatches(Formula.CreateFormula("*1.2."))
                            .size());

        }

    }

}

