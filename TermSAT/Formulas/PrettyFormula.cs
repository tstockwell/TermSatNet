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
     * Example:  
     * This formula in TermSAT normal form...
     *      *.1*.1-.2
     * ...is written like this in 'pretty' form...
     *      (1 -> (1 -> ~2))
     *  
     * 
     * @author Ted Stockwell
     */
    static public class PrettyFormula
    {
        public static string ToFormulaString(this string prettyText)
        {
            return new PrettyParser(prettyText).GetFormulaText();
        }

        public static string ToPrettyString(this string formulaText)
        {
            return ToPrettyString(formulaText.GetOrParse());
        }

        public static string ToPrettyString(this Formula formula)
        {
            if (formula is Constant)
                return formula.ToString();
            if (formula is Variable)
                return formula.ToString().Replace(".", "");
            if (formula is Negation)
                return "~" + ToPrettyString((formula as Negation).Child);
            if (formula is Implication)
            {
                Implication i = formula as Implication;
                return "(" + ToPrettyString(i.Antecedent) + "->" + ToPrettyString(i.Consequent) + ")";
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

        public string GetFormulaText()
        {
            char c = _formula[_position];
            if (c == '(')
            {
                _position++;
                string antecedent = GetFormulaText();
                if (!_formula.Substring(_position).StartsWith("->"))
                    throw new TermSatException("Expected -> at position " + _position);
                _position += 2;
                string consequent = GetFormulaText();
                return Symbol.Implication.ToString() + antecedent + consequent;
            }
            else if (c == '~')
            {
                _position++;
                string antecedent = GetFormulaText();
                return Symbol.Negation.ToString() + antecedent;
            }
            else if (Char.IsDigit(c))
            {
                int i = _position;
                while (Char.IsDigit(_formula[++_position])) { }
                return Symbol.Variable.ToString() + _formula.Substring(i, _position - i);
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


