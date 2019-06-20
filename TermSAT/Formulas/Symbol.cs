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
    /// <summary>
    /// 
    /// Note that the characters used to represent formulas as strings were 
    /// specifically chosen so that the string representations of a formula naturally 
    /// sort in the same order as formulas themselves.  
    /// That is, formulas that are just a constant come before variables, and variables 
    /// before negations, and so on.
    /// So the characters chosen to represent these naturally sort in the same order.
    /// Also, since implications with simpler antecents are simpler than other implications 
    /// of the same length, polish notation is used since it will causes formula strings 
    /// to naturally sort in the same order as the formulas. 
    /// 
    /// 
    /// </summary>
    public class Symbol : EnumType<Symbol, char>
    {
        public static Symbol Variable = new Symbol('.');
        public static Symbol Negation = new Symbol('-');
        public static Symbol Implication = new Symbol('*');

        public static Symbol True = new Symbol('T');
        public static Symbol False = new Symbol('F');

        private string Text { get; }

        private Symbol(char c) : base(c) { }


        public static bool IsNegation(char c)
        {
            return c == Negation.Value;
        }

        public static bool IsImplication(char c)
        {
            return c == Implication.Value;
        }

        public static bool IsVariable(char c)
        {
            return c == Variable.Value;
        }

        public static bool IsFalse(char c)
        {
            return c == False.Value;
        }

        public static bool IsTrue(char c)
        {
            return c == True.Value;
        }


        public static bool IsNegation(string c)
        {
            return c.StartsWith(Negation.Value);
        }

        public static bool IsImplication(string c)
        {
            return c.StartsWith(Implication.Value);
        }

        public static bool IsVariable(string c)
        {
            return c.StartsWith(Variable.Value);
        }

        public static bool IsTrue(string c)
        {
            return c.StartsWith(True.Value);
        }

        public static bool IsFalse(string c)
        {
            return c.StartsWith(False.Value);
        }
        public static bool IsConstant(string c)
        {
            return IsTrue(c) || IsFalse(c);
        }

        public static bool IsSymbol(char c)
        {
            return IsTrue(c) || IsFalse(c) || IsVariable(c) || IsNegation(c) || IsImplication(c);
        }
    }
}

