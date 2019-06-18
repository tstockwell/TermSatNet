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

using System.Diagnostics;
using TermSAT.RuleDatabase;

namespace TermSAT.RuleDatabase
{

    /**
     * Spits out a bunch of information about the formulas in the Rule Database.
     *  
     * @author Ted Stockwell
     *
     */
    public class DatabaseReport
    {

        private const string SHOW_REDUCTION_RULES = "-showReductionRules";

        public static void Main(string[] args)
        {
            bool showReductionRules = false;

            foreach (string arg in args)
                if (arg.Equals(SHOW_REDUCTION_RULES))
                    showReductionRules = true;

            var database = new RuleDatabase();

            /* 
             * Count # of rules
             */
            Trace.WriteLine("Total number of canonical formulas is " + database.CountCanonicalFormulas());
            Trace.WriteLine("Total number of non-canonical formulas  is " + database.CountNonCanonicalFormulas());

            int tt = 0;
            foreach (var truthTable in database.GetAllTruthTables())
            {
                if (0 < database.GetLengthOfCanonicalFormulas(truthTable))
                    tt++;
            }
            Trace.WriteLine("Have found formlas for " + tt + " of " + TruthTable.MAX_TRUTH_TABLES + " truth tables");
            Trace.WriteLine("The length of the longest canonical formula is " + database.GetLengthOfLongestCanonicalFormula());
            Trace.WriteLine("");


            /*
             * List lengths and # of canonical formulas
             */
            Trace.WriteLine("TRUTH VALUE     LENGTH    COUNT");
            Trace.WriteLine("                FORMULAS");
            Trace.WriteLine("-------------   ------   ------");
            foreach (var truthTable in database.GetAllTruthTables())
            {
                var canonicalFormulas = database.GetCanonicalFormulas(truthTable);

                var t = "(" + truthTable.ToString() + ")                 ";
                t = t.Substring(0, 13);

                if (canonicalFormulas.Count <= 0)
                {
                    Trace.WriteLine(t + "   *not yet determined*");
                }
                else
                {
                    var l = "      " + canonicalFormulas[0].Length;
                    l = l.Substring(l.Length - 6);
                    var c = "      " + canonicalFormulas.Count;
                    c = c.Substring(c.Length - 6);
                    Trace.WriteLine(t + "   " + l + "   " + c);
                    foreach (var formula in canonicalFormulas)
                    {
                        Trace.WriteLine("              " + formula);
                    }
                }
                Trace.WriteLine("-------------   ------   ------");
            }

            Trace.WriteLine("");
            Trace.WriteLine("Canonical Formulas in Lexical Order");
            Trace.WriteLine("=====================================");
            foreach (var formula in database.GetAllCanonicalFormulasInLexicalOrder())
            {
                Trace.WriteLine(formula);
            }

            if (showReductionRules)
            {
                Trace.WriteLine("");
                Trace.WriteLine("Reduction Rules");
                Trace.WriteLine("=====================================");
                long count = 0;
                foreach (var formula in database.GetAllNonCanonicalFormulas())
                {
                    Trace.WriteLine(new ReductionRule(formula, database.FindCanonicalFormula(formula)));
                    count++;
                }
            }


        }

    }

}
