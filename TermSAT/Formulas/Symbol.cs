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

using System;
using TermSAT.Common;

namespace TermSAT.Formulas
{
    public class Symbol : EnumType<Symbol, char>
    {
        public static Symbol True = new Symbol('T');
        public static Symbol False = new Symbol('F');
        public static Symbol Negation = new Symbol('-');
        public static Symbol Implication = new Symbol('*');
        public static Symbol Variable = new Symbol('.');


        private string Text { get; }

        private Symbol(char c) : base(c) { }


        public static bool isNegation(char c)
        {
            return c == Negation.Value;
        }

        public static bool isImplication(char c)
        {
            return c == Implication.Value;
        }

        public static bool isVariable(char c)
        {
            return c == Variable.Value;
        }

        public static bool isTrue(char c)
        {
            return c == True.Value;
        }

        public static bool isFalse(char c)
        {
            return c == False.Value;
        }
        public static bool isConstant(char c)
        {
            return isTrue(c) || isFalse(c);
        }


        public static bool isNegation(string c)
        {
            return c.StartsWith(Negation.Value);
        }

        public static bool isImplication(string c)
        {
            return c.StartsWith(Implication.Value);
        }

        public static bool isVariable(string c)
        {
            return c.StartsWith(Variable.Value);
        }

        public static bool isTrue(string c)
        {
            return c.StartsWith(True.Value);
        }

        public static bool isFalse(string c)
        {
            return c.StartsWith(False.Value);
        }
        public static bool isConstant(string c)
        {
            return isTrue(c) || isFalse(c);
        }

        public static bool isSymbol(char c)
        {
            return isTrue(c) || isFalse(c) || isVariable(c) || isNegation(c) || isImplication(c);
        }
    }
}

