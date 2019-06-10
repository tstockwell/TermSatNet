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
using System;

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
                return formula.ToString();
            if (formula is Variable)
                return formula.ToString().Replace("\\.", "");
            if (formula is Negation)
                return "~" + getPrettyText((formula as Negation).Child);
            if (formula is Implication)
            {
                Implication i = formula as Implication;
                return "(" + getPrettyText(i.Antecedent) + "->" + getPrettyText(i.Consequent) + ")";
            }
            throw new TermSatException("Unknown formula:" + formula);
        }

    }

    class PrettyParser
    {
        int _position = 0;
        string _formula;
        public PrettyParser(string formula)
        {
            _formula = formula;
            _formula = formula.Replace(" ", "");
        }

        public string getFormulaText()
        {
            char c = _formula[_position];
            if (c == '(')
            {
                _position++;
                string antecedent = getFormulaText();
                if (!_formula.Substring(_position).StartsWith("->"))
                    throw new TermSatException("Expected -> at position " + _position);
                _position += 2;
                string consequent = getFormulaText();
                return Symbol.Implication.ToString() + antecedent + consequent;
            }
            else if (c == '~')
            {
                _position++;
                string antecedent = getFormulaText();
                return Symbol.Negation.ToString() + antecedent;
            }
            else if (Char.IsDigit(c))
            {
                int i = _position;
                while (Char.IsDigit(_formula[++_position])) { }
                return _formula.Substring(i, _position - i) + Symbol.Variable.ToString();
            }
            else if (c == 'T')
            {
                return Symbol.True.ToString();
            }
            else if (c == 'F')
            {
                return Symbol.False.ToString();
            }
            throw new TermSatException("Unexpected character '" + c + "' at position " + _position);
        }

    }


}


