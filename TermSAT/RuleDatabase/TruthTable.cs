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
using System.Runtime.CompilerServices;
using TermSAT.Common;
using TermSAT.Formulas;

namespace TermSAT.RuleDatabase
{


    /// <summary>
    /// Represents a formula's truth table.
    /// A truth table enumerates the values of a boolean formula over all possible assignments of variables.
    /// Since the number of possible assignments grows exponentially as the number of variables increases, it's 
    /// not really feasible to implement truth tables for formulas with any possible number of variables.
    /// Truth tables are only used by TermSAT to generate reduction rules for formulas with just a few variables.
    /// 
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

        public const int VARIABLE_COUNT= 4;
	    public const int MAX_TRUTH_VALUES= 1 << VARIABLE_COUNT;
	    public const long MAX_TRUTH_TABLES= 1 << MAX_TRUTH_VALUES;

        static WeakCache<string, TruthTable> __cache = new WeakCache<string, TruthTable>();
        static ConditionalWeakTable<Formula, TruthTable> __formulaCache = new ConditionalWeakTable<Formula, TruthTable>();

        private BitArray values = new BitArray(MAX_TRUTH_VALUES);

        private TruthTable(BitArray values)
        {
            this.values = values;
        }

        private TruthTable(string valueText) : this(valueText.ToBitArray()) {  }
        private TruthTable(Formula formula) : this(ToBitArray(formula)) { }

        public static TruthTable GetTruthTable(string valueText) { 
            __cache.GetValue(valueText, out TruthTable t, () => new TruthTable(valueText));
            return t;
        }

        public static TruthTable GetTruthTable(Formula formula) { 
            var t = __formulaCache.GetValue(formula, _ => new TruthTable(_));
            return t;
        }

        /// <summary>
        /// Create a bit array that represents a formula's truth table
        /// </summary>
        private static BitArray ToBitArray(Formula formula)
        {
            var bits = new BitArray(MAX_TRUTH_VALUES);

            for (int i= 0; i < bits.Length; i++)
            {
                BitArray a = new BitArray(new int[] { i });

                var valuation = new Dictionary<Variable, bool>();
                for (int b= 1; b <= VARIABLE_COUNT; b++)
                {
                    var variable = Variable.NewVariable(b);
                    valuation.Add(variable, a[b-1]);
                }

                var formulaValue= formula.Evaluate(valuation);
                bits.Set(i, formulaValue);
            }
            return bits;
        }

        public override string ToString() => values.ToHexadecimalString();

    }


    public static class TruthTableExtensions
    {
        public static TruthTable GetTruthTable(this Formula formula) =>
            TruthTable.GetTruthTable(formula);
    }
}
