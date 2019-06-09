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
using System;
using System.Collections;
using System.Collections.Generic;
using TermSAT.Common;
using TermSAT.Formulas;

namespace TermSAT.RuleDatabase
{


    /// <summary>
    /// Represents a formula's truth table.
    /// A truth table enumerates the values of a boolean formula over all possible assignments of variables.
    /// Since the number of possibles assignments grows exponentially as the number of variables increases, it's 
    /// not really feasible to implement truth tables for fomulas with any possible number of variables.
    /// Truth tables are only used by TermSAT to generate reduction rules for formulas with just a few variables.
    /// This implementation uses a bit array internally and cannot support truth tables for more than 31 variables.
    /// 
    /// Truth tables are designed to make it possible for the RuleGenerator to lookup canonical formulas by truth table 
    /// in a database, therefore the ToString method returns all the values in a TruthTable encoded in a string, which 
    /// can be used in a SQL query AND is somewhat human readable.  
    /// 
    /// Truth Tables are immutable, and cached, of course.
    /// 
    /// 
    /// </summary>
    /// <author>ted stockwell</author>
    public class TruthTable
    {

        static WeakCache<string, TruthTable> __cache = new WeakCache<string, TruthTable>();
        static WeakCache<Formula, TruthTable> __formulaCache = new WeakCache<Formula, TruthTable>();

        private BitArray values = new BitArray(2 ^ (2 ^ RuleDatabase.VARIABLE_COUNT));

        private TruthTable(BitArray values)
        {
            this.values = values;
        }
        private TruthTable(string valueText) : this(valueText.ToBitArray()) {  }
        private TruthTable(Formula formula) : this(ToBitArray(formula)) { }

        public static TruthTable newTruthTable(string valueText)
        {
            return __cache.GetOrCreateValue(valueText, (y) => new TruthTable(valueText));
        }
        public static TruthTable newTruthTable(Formula formula)
        {
            return __formulaCache.GetOrCreateValue(formula, (y) => new TruthTable(formula));
        }

        /// <summary>
        /// Create a bit array that represents a formula's truth table
        /// </summary>
        public static BitArray ToBitArray(Formula formula)
        {
            var truthTable = new BitArray(2 ^ (2 ^ RuleDatabase.VARIABLE_COUNT));

            for (int i= 0; i < truthTable.Length; i++)
            {
                var valuation = new Dictionary<Variable, bool>();

                for (int b= 1; b <= RuleDatabase.VARIABLE_COUNT; b++)
                {
                    var variable = Variable.newVariable(b);
                    var mask = 1 << (b - 1);
                    var iVariableValue= i & ~(mask);
                    valuation.Add(variable, iVariableValue != 0);
                }

                var formulaValue= formula.evaluate(valuation);
                truthTable.Set(i, formulaValue);
            }
            return truthTable;
        }


        override public bool Equals(Object obj)
        {
            if (obj is TruthTable)
                return ((TruthTable)obj).values == this.values;
            if (obj is Formula)
                return ToBitArray(obj as Formula) == this.values;
            if (obj is string)
                return this.ToString() == (obj as string);
            return false;
        }

        public override string ToString()
        {
            return values.ToHexadecimalString();
        }

    }

}
