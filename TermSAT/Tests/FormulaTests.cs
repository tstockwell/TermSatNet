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
using System.Diagnostics;
using TermSAT.Common;

namespace TermSAT.Tests
{
    [TestClass]
    public class FormulaTests
    {


        [TestMethod]
        public void TestPrettyPrinter()
        {
            Assert.AreEqual("*.1.2", PrettyFormula.ToFormulaString("(1->2)"));
            Assert.AreEqual("*.1-.2", PrettyFormula.ToFormulaString("(1->~2)"));
            Assert.AreEqual("*.1-.2", PrettyFormula.ToFormulaString("(1 -> ~2)"));
            Assert.AreEqual("*.1T", PrettyFormula.ToFormulaString("(1 -> T)"));
            Assert.AreEqual("*.1*.1-.2", PrettyFormula.ToFormulaString("(1 -> (1 -> ~2))"));

            Assert.AreEqual("(1->2)", PrettyFormula.ToPrettyString("*.1.2"));
            Assert.AreEqual("(1->~2)", PrettyFormula.ToPrettyString("*.1-.2"));
            Assert.AreEqual("(1->~2)", PrettyFormula.ToPrettyString("*.1-.2"));
            Assert.AreEqual("(1->T)", PrettyFormula.ToPrettyString("*.1T"));
            Assert.AreEqual("(1->(1->~2))", PrettyFormula.ToPrettyString("*.1*.1-.2"));
        }

        [TestMethod]
        public void TestFormulaVariables()
        {
            Assert.AreEqual(3, "**.1.2.3".ToFormula().AllVariables.Count);
            Assert.AreEqual(3, "**.3.2.1".ToFormula().AllVariables.Count);
            Assert.AreEqual(3, "**.1.2*.3.1".ToFormula().AllVariables.Count);
            Assert.IsTrue("**.1.2*.3.1".ToFormula().AllVariables.Contains(Variable.ONE));
            Assert.IsTrue("**.1.2*.3.1".ToFormula().AllVariables.Contains(Variable.TWO));
            Assert.IsTrue("**.1.2*.3.1".ToFormula().AllVariables.Contains(Variable.THREE));
        }


        [TestMethod]
        public void TestFormulaConstruction()
        {
            Assert.AreEqual(".1", Variable.NewVariable(1).ToString());
            Assert.AreEqual("*.1.1", "*.1.1".ToFormula().ToString());

            var text = "***.1.2.3.4";
            Formula formula1 = text.ToFormula();
            Assert.AreEqual(7, formula1.Length);
            Assert.AreEqual(formula1.ToString(), text);

            text = "*" + text + text;
            Formula formula2 = Implication.NewImplication(formula1, formula1);
            Assert.AreEqual(15, formula2.Length);
            Assert.AreEqual(formula2.ToString(), text);

            text = "-" + text;
            Formula formula3 = Negation.NewNegation(formula2);
            Assert.AreEqual(16, formula3.Length);
            Assert.AreEqual(formula3.ToString(), text);

            Formula formula4 = Variable.NewVariable(23);
            Assert.AreEqual(formula4.ToString(), ".23");

        }

        [TestMethod]
        public void TestFormulaCaching()
        {

            /* At one time this test would fail easily.
             * Getting a decent cache implementation working in .NET was difficult
             */
            {
                for (var i = 10000; 0 < i--;)
                {
                    Implication formula1 = "***.1.2.3.4";
                    Implication formula2= Implication.NewImplication(formula1, formula1);
                    WeakCacheFlag.Value= formula1.GetHashCode();
                    for (var j= 10000; 0 < j--;)
                    {
                        var x= Implication.NewImplication(formula1, formula1);
                        Assert.AreEqual(formula2, x);
                    }
                    WeakCacheFlag.Value= -1;
                }
            }


            // gotta test all formulas types, since each has thier own cache
            {
                Assert.AreEqual("T".ToFormula().GetHashCode(), "T".ToFormula().GetHashCode());
                Assert.AreEqual(".1".ToFormula().GetHashCode(), ".1".ToFormula().GetHashCode());
                Assert.AreEqual("-.1".ToFormula().GetHashCode(), "-.1".ToFormula().GetHashCode());
                Assert.AreEqual("*.1.1".ToFormula().GetHashCode(), "*.1.1".ToFormula().GetHashCode());

                Implication formula1 = "***.1.2.3.4";
                Implication rule1 = "*.1.2";
                Assert.AreEqual(rule1.GetHashCode(), ((Implication)formula1.Antecedent).Antecedent.GetHashCode());
                Implication formula2 = Implication.NewImplication(formula1, formula1);
                Negation formula3 = Negation.NewNegation(formula1);

                var substitutions = new Dictionary<Variable, Formula> { { ".1", formula1 }, { ".2", formula1 } };
                Formula substitutionI = rule1.CreateSubstitutionInstance(substitutions);
                Assert.AreEqual(formula2.GetHashCode(), substitutionI.GetHashCode());

            }

            {
                for (var i = 1000; 0 < i--;)
                {
                    Implication formula1 = "***.1.2.3.4";
                    Implication formula2 = Implication.NewImplication(formula1, formula1);
                    var formula2Subformulas= formula2.ToSequence();

                    Implication rule1 = "*.1.2";
                    InstanceRecognizer recognizer = new InstanceRecognizer() { rule1 };

                    var match = recognizer.FindFirstGeneralization(formula2);
                    Assert.AreEqual(match.Generalization, rule1);
                    Assert.AreEqual(match.Substitutions[Variable.ONE], formula1);
                    Assert.AreEqual(match.Substitutions[Variable.TWO], formula1);

                    var substitution = match.Generalization.CreateSubstitutionInstance(match.Substitutions);
                    var substitutionSubformulas = substitution.ToSequence();

                    if (!formula2.Equals(substitution))
                    {
                        if (formula2.GetHashCode() != substitution.GetHashCode())
                        {
                            // test variables ****.1.2.3.4***.1.2.3.4
                            Assert.AreEqual(Variable.ONE.GetHashCode(), substitutionSubformulas[4].GetHashCode());
                            Assert.AreEqual(Variable.ONE.GetHashCode(), substitutionSubformulas[11].GetHashCode());
                            Assert.AreEqual(Variable.ONE.GetHashCode(), formula2Subformulas[4].GetHashCode());
                            Assert.AreEqual(Variable.ONE.GetHashCode(), formula2Subformulas[11].GetHashCode());
                            Assert.AreEqual(Variable.TWO.GetHashCode(), substitutionSubformulas[5].GetHashCode());
                            Assert.AreEqual(Variable.TWO.GetHashCode(), substitutionSubformulas[12].GetHashCode());
                            Assert.AreEqual(Variable.TWO.GetHashCode(), formula2Subformulas[5].GetHashCode());
                            Assert.AreEqual(Variable.TWO.GetHashCode(), formula2Subformulas[12].GetHashCode());
                            Assert.AreEqual(Variable.THREE.GetHashCode(), substitutionSubformulas[6].GetHashCode());
                            Assert.AreEqual(Variable.THREE.GetHashCode(), substitutionSubformulas[13].GetHashCode());
                            Assert.AreEqual(Variable.THREE.GetHashCode(), formula2Subformulas[6].GetHashCode());
                            Assert.AreEqual(Variable.THREE.GetHashCode(), formula2Subformulas[13].GetHashCode());
                            var four= Variable.NewVariable(4);
                            Assert.AreEqual(four.GetHashCode(), substitutionSubformulas[7].GetHashCode());
                            Assert.AreEqual(four.GetHashCode(), substitutionSubformulas[14].GetHashCode());
                            Assert.AreEqual(four.GetHashCode(), formula2Subformulas[7].GetHashCode());
                            Assert.AreEqual(four.GetHashCode(), formula2Subformulas[14].GetHashCode());

                            // test innermost implications
                            Assert.AreEqual(formula2Subformulas[3].GetHashCode(), substitutionSubformulas[3].GetHashCode());
                            Assert.AreEqual(formula2Subformulas[10].GetHashCode(), substitutionSubformulas[10].GetHashCode());
                            Assert.AreEqual(formula2Subformulas[3].GetHashCode(), substitutionSubformulas[10].GetHashCode());

                            // test next outer implications
                            Assert.AreEqual(formula2Subformulas[2].GetHashCode(), substitutionSubformulas[2].GetHashCode());
                            Assert.AreEqual(formula2Subformulas[9].GetHashCode(), substitutionSubformulas[9].GetHashCode());
                            Assert.AreEqual(formula2Subformulas[2].GetHashCode(), substitutionSubformulas[9].GetHashCode());

                            // test next outer implications
                            Assert.AreEqual(formula2Subformulas[1].GetHashCode(), substitutionSubformulas[1].GetHashCode());
                            Assert.AreEqual(formula2Subformulas[8].GetHashCode(), substitutionSubformulas[8].GetHashCode());
                            Assert.AreEqual(formula2Subformulas[1].GetHashCode(), substitutionSubformulas[8].GetHashCode());

                            // test next outer implications
                            Assert.AreEqual(formula2Subformulas[0].GetHashCode(), substitutionSubformulas[0].GetHashCode());
                        }
                        Assert.Fail("We have two different instances of the same formuals, formulas have not been properly cached");
                    }
                }
            }

        }

        [TestMethod]
        public void TestEvaluation()
        {
            Variable one = ".1";
            Variable two = ".2";
            Formula formula1 = "*.1.2";
            Formula formula2 = "-*.1.2";
            for (int a = 0; a <= 1; a++)
            {
                for (int b = 0; b <= 1; b++)
                {
                    var valuation = new Dictionary<Variable, bool>
                    {
                        { one, (a == 1 ? true : false) },
                        { two, (b == 1 ? true : false) }
                    };
                    var value1 = formula1.Evaluate(valuation);
                    Assert.AreEqual(a != 1 || b != 0, value1, "formula evaluation failed");
                    var value2 = formula2.Evaluate(valuation);
                    Assert.AreEqual(!value1, value2, "formula evaluation failed");
                }
            }
        }

        [TestMethod]
        public void TestSubstitutions()
        {
            Formula formula1 = "***.1.2.3.4";

            var substitutions = new Dictionary<Variable, Formula>
            {
                { ".1", ".5" },
                { ".2", ".6" },
                { ".3", ".7" },
                { ".4", ".8" }
            };
            Formula instance = formula1.CreateSubstitutionInstance(substitutions);

            Assert.AreEqual("***.5.6.7.8", instance);
        }

        [TestMethod]
        public void TestInstanceRecognizer()
        {
            SubstitutionInstance match;
            ICollection<SubstitutionInstance> matches;
            Formula substitution;

            Formula formula1 = "***.1.2.3.4";
            Formula formula2 = Implication.NewImplication(formula1, formula1);

            Formula rule1 = "*.1.2";
            InstanceRecognizer recognizer = new InstanceRecognizer() { rule1 };


            Assert.AreEqual(1, recognizer.FindAllGeneralizations(formula1).Count);
            Assert.AreEqual(1, recognizer.FindAllGeneralizations(formula2).Count);

            match = recognizer.FindFirstGeneralization(formula2);
            Assert.IsNotNull(match.Substitutions);
            Assert.AreEqual(2, match.Substitutions.Count);

            substitution = rule1.CreateSubstitutionInstance(match.Substitutions);
            Assert.AreEqual(formula2.GetHashCode(), substitution.GetHashCode(), "We have two different instances of the same formuals, formulas have not been properly cached");
            Assert.AreEqual(formula2, substitution);


            match = recognizer.FindFirstGeneralization(formula2);
            Assert.IsNotNull(match.Substitutions);
            Assert.AreEqual(2, match.Substitutions.Count);

            recognizer.Add("*.1.1");
            Assert.AreEqual(2, recognizer.FindAllGeneralizations(formula2).Count);

            InstanceRecognizer recognizer2 = new InstanceRecognizer { "**.1.2*.3.4" };
            Assert.AreEqual(1, recognizer2.FindAllGeneralizations(formula2).Count);

            recognizer.Add("**.1.2*.3.4");
            Assert.AreEqual(3, recognizer.FindAllGeneralizations(formula2).Count);
            recognizer.Add("**.1.2*.3.2");
            Assert.AreEqual(4, recognizer.FindAllGeneralizations(formula2).Count);
            recognizer.Add("**.1.2*.1.4");
            Assert.AreEqual(5, recognizer.FindAllGeneralizations(formula2).Count);
            recognizer.Add("**.1.2*.1.2");
            matches = recognizer.FindAllGeneralizations(formula2);
            Assert.AreEqual(6, matches.Count);
            foreach (var m in matches)
            {
                Assert.AreEqual(formula2, m.Generalization.CreateSubstitutionInstance(m.Substitutions));
            }


            Formula formula3 = Negation.NewNegation(formula2);
            recognizer.Add("-.1");
            Assert.AreEqual(1, recognizer.FindAllGeneralizations(formula3).Count);

            recognizer = new InstanceRecognizer { "*.1T" };
            Assert.AreEqual(1, recognizer.FindAllGeneralizations("*.1T").Count);

            recognizer = new InstanceRecognizer { "*.1.1" };
            Assert.AreEqual(0, recognizer.FindAllGeneralizations("*.1.2").Count);

            recognizer = new InstanceRecognizer { "-T" };
            Assert.AreEqual(0, recognizer.FindAllGeneralizations("-F").Count);

            recognizer = new InstanceRecognizer { "*.1-.1" };
            Assert.AreEqual(1, recognizer.FindAllGeneralizations("**.2.1-*.2.1").Count);
        }

    }

}

