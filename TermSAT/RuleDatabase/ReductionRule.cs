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
using TermSAT.Formulas;

namespace TernSat.RuleDatabase
{
    public class ReductionRule
    {
        public Formula formula;
        public Formula reduction;

        public ReductionRule(Formula formula, Formula reduction)
        {
            this.formula = formula;
            this.reduction = reduction;
        }

        override public string ToString()
        {
            return PrettyFormula.getPrettyText(formula) + " ==> " + PrettyFormula.getPrettyText(reduction);
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
        static public Formula reduceUsingRule(this Formula formula, ReductionRule rule)
        {
            Formula reducedFormula;

            /// variable and constants can't be reduced
            if (formula is Variable || formula is Constant)
                return null;

            //
            // reduce subformulas before reducing this formula
            //
            if (formula is Negation)
            {
                Formula negated = ((Negation)formula).getChild();
                Formula n = reduceUsingRule(negated, rule);
                if (n != null)
                {
                    reducedFormula = Formula.createNegation(n);
                    return reducedFormula;
                }
            }
            else if (formula is Implication)
            {
                Implication implication = (Implication)formula;
                Formula antecent = implication.getAntecedent();
                Formula consequent = implication.getConsequent();
                Formula a = reduceUsingRule(antecent, rule);
                if (a != null)
                {
                    reducedFormula = Formula.createImplication(a, consequent);
                    return reducedFormula;
                }
                Formula c = reduceUsingRule(consequent, rule);
                if (c != null)
                {
                    reducedFormula = Formula.createImplication(antecent, c);
                    return reducedFormula;
                }
            }


            // check if given formula is a substitution instance of the reduction rules formula
			var substitutionInstance
            InstanceRecognizer instanceRecognizer = new InstanceRecognizer();
            instanceRecognizer.addFormula(rule.formula);
            List<NodeInfo> matches = instanceRecognizer.findMatchingNodes(formula, 1);
            if (matches == null)
                return null;

            // if formula and rule formula match then create reduced formula
            NodeInfo info = matches.get(0);
            reducedFormula = Formula.createInstance(rule.reduction, info.substitutions);
            return reducedFormula;
        }



    }

}
