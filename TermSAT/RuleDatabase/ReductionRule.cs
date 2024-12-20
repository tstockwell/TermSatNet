/*******************************************************************************
 * termsat SAT solver
 *     Copyright (C) 2010 Ted Stockwell <emorning@yahoo.com>
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
using System.Threading.Tasks;
using TermSAT.Formulas;

namespace TermSAT.RuleDatabase
{
    public class ReductionRule
    {
        public Formula Formula {  get; private set; }
        public Formula Reduction {  get; private set; }

        public ReductionRule(Formula formula, Formula reduction)
        {
            this.Formula = formula;
            this.Reduction = reduction;
        }

        override public string ToString()
        {
            return Formula.ToString() + " ==> " + Reduction.ToString();
        }
    }


	public static class ReductionRuleUtilities
    {

        /**
         * Returns a formula that is reduced by applying the given rule.
         * This method examines subterms of the given formula.
         * This method only applies the reduction rule once, it doesn't apply it as many times as 
         * possible.
         * @return A reduced formula.  Returns null if the given formula can't be reduced. 
         */
        public static async Task<Formula> ReduceUsingRule(this Formula formula, ReductionRule rule)
        {
            Formula reducedFormula;

            /// variable and constants can't be reduced
            if (formula is Variable || formula is Constant)
                return null;

            //
            // reduce subformulas before reducing this formula
            //
            if (formula is Negation negation)
            {
                Formula negated = negation.Child;
                Formula n = await ReduceUsingRule(negated, rule);
                if (n != null)
                {
                    reducedFormula = Negation.NewNegation(n);
                    return reducedFormula;
                }
            }
            else if (formula is Implication implication)
            {
                Formula antecent = implication.Antecedent;
                Formula consequent = implication.Consequent;
                Formula a = await ReduceUsingRule(antecent, rule);
                if (a != null)
                {
                    reducedFormula = Implication.NewImplication(a, consequent);
                    return reducedFormula;
                }
                Formula c = await ReduceUsingRule(consequent, rule);
                if (c != null)
                {
                    reducedFormula = Implication.NewImplication(antecent, c);
                    return reducedFormula;
                }
            }


            // check if given formula is a substitution instance of the reduction rules formula
            InstanceRecognizer instanceRecognizer = new InstanceRecognizer { rule.Formula };
            var matches = instanceRecognizer.FindGeneralizationNodes(formula, 1);
            if (matches == null)
                return null;

            // if formula and rule formula match then create reduced formula
            var info = matches[0];
            reducedFormula = rule.Reduction.CreateSubstitutionInstance(info.Substitutions);
            return reducedFormula;
        }



    }

}
