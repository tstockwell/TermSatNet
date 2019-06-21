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
        public static Symbol Variable = new Symbol('.');
        public static Symbol Negation = new Symbol('-');
        public static Symbol Implication = new Symbol('*');

        public static Symbol True = new Symbol('T');
        public static Symbol False = new Symbol('F');

        private string Text { get; }

        private Symbol(char c) : base(c) { }

        public static bool IsNegation(char c) => c == Negation.Value;
        public static bool IsImplication(char c) => c == Implication.Value;
        public static bool IsVariable(char c) => c == Variable.Value;
        public static bool IsFalse(char c) => c == False.Value;
        public static bool IsTrue(char c) => c == True.Value;


        public static bool IsNegation(string c) => c.StartsWith(Negation.Value);
        public static bool IsImplication(string c) => c.StartsWith(Implication.Value);
        public static bool IsVariable(string c) => c.StartsWith(Variable.Value);
        public static bool IsTrue(string c) => c.StartsWith(True.Value);
        public static bool IsFalse(string c) => c.StartsWith(False.Value);
        public static bool IsConstant(string c) => IsTrue(c) || IsFalse(c);

        public static bool IsSymbol(char c) => IsTrue(c) || IsFalse(c) || IsVariable(c) || IsNegation(c) || IsImplication(c);
    }
}

