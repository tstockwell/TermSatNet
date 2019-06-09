/*******************************************************************************
 *     termsat SAT solver
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
namespace TermSAT.Formulas
{

    /**
     * A utility for converting 'pretty' formula text into this system's 
     * normal form, and vice versa.
     * 
     * @author Ted Stockwell
     */
    public class PrettyFormula
    {
        private static class PrettyParser
        {
            int _position = 0;
            string _formula;
            PrettyParser(string formula)
            {
                _formula = formula;
                _formula = formula.replaceAll(" ", "");
            }

            string getFormulaText()
            {
                char c = _formula.charAt(_position);
                if (c == '(')
                {
                    _position++;
                    string antecedent = getFormulaText();
                    if (!_formula.substring(_position).startsWith("->"))
                        throw new RuntimeException("Expected -> at position " + _position);
                    _position += 2;
                    string consequent = getFormulaText();
                    return Operator.Implication.getFormulaText() + antecedent + consequent;
                }
                else if (c == '~')
                {
                    _position++;
                    string antecedent = getFormulaText();
                    return Operator.Negation.getFormulaText() + antecedent;
                }
                else if (Character.isDigit(c))
                {
                    int i = _position;
                    while (Character.isDigit(_formula.charAt(++_position))) { }
                    return _formula.substring(i, _position) + Symbol.Variable.getFormulaText();
                }
                else if (c == 'T')
                {
                    return Symbol.True.getFormulaText();
                }
                else if (c == 'F')
                {
                    return Symbol.False.getFormulaText();
                }
                throw new RuntimeException("Unexpected character '" + c + "' at position " + _position);
            }

        }

        public static string getFormulaText(string prettyText)
        {
            return new PrettyParser(prettyText).getFormulaText();
        }

        public static string getPrettyText(string formula)
        {
            return getPrettyText(Formula.CreateFormula(formula));
        }

        public static string getPrettyText(Formula formula)
        {
            if (formula is Constant)
			return formula.tostring();
            if (formula is Variable)
			return formula.tostring().replaceAll("\\.", "");
            if (formula is Negation)
			return "~" + getPrettyText(((Negation)formula).getChild());
            if (formula is Implication) {
                Implication i = (Implication)formula;
                return "(" + getPrettyText(i.getAntecedent()) + "->" + getPrettyText(i.getConsequent()) + ")";
            }
            throw new RuntimeException("Unknown formula:" + formula);
        }

    }

}


