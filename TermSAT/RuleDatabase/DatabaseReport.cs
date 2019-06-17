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
            Trace.WriteLine("Total number of canonical formulas is " + database.countCanonicalFormulas());
            Trace.WriteLine("Total number of non-canonical formulas  is " + database.countNonCanonicalFormulas());

            int tt = 0;
            for (int truthValue = 0; truthValue < TruthTable.MAX_TRUTH_TABLES; truthValue++)
            {
                TruthTable truthTable = TruthTable.newTruthTable(truthValue);
                if (0 < database.getLengthOfCanonicalFormulas(truthTable))
                    tt++;
            }
            Trace.WriteLine("Have found formlas for " + tt + " of " + TruthTable.MAX_TRUTH_TABLES + " truth tables");
            Trace.WriteLine("The length of the longest canonical formula is " + database.getLengthOfLongestCanonicalFormula());
            Trace.WriteLine("");


            /*
             * List lengths and # of canonical formulas
             */
            database.getLengthOfCanonicalFormulas(TruthTables.create(0));
            Trace.WriteLine("TRUTH VALUE     LENGTH    COUNT");
            Trace.WriteLine("                FORMULAS");
            Trace.WriteLine("-------------   ------   ------");
            for (int truthValue = 0; truthValue < TruthTable.MAX_TRUTH_TABLES; truthValue++)
            {
                TruthTable truthTable = TruthTables.create(truthValue);
                var canonicalFormulas = database.getCanonicalFormulas(truthTable);

                var t = "" + truthValue + "(" + truthTable + ")                 ";
                t = t.substring(0, 13);

                if (canonicalFormulas.Count <= 0)
                {
                    Trace.WriteLine(t + "   *not yet determined*");
                }
                else
                {
                    var l = "      " + canonicalFormulas.get(0).length();
                    l = l.substring(l.length() - 6);
                    var c = "      " + canonicalFormulas.Count;
                    c = c.substring(c.length() - 6);
                    Trace.WriteLine(t + "   " + l + "   " + c);
                    foreach (var formula in canonicalFormulas)
                    {
                        Trace.WriteLine("              " + formula.toString());
                    }
                }
                Trace.WriteLine("-------------   ------   ------");
            }

            Trace.WriteLine("");
            Trace.WriteLine("Canonical Formulas in Lexical Order");
            Trace.WriteLine("=====================================");
            foreach (var formula in database.getAllCanonicalFormulasInLexicalOrder())
            {
                Trace.WriteLine(formula);
            }

            if (showReductionRules)
            {
                Trace.WriteLine("");
                Trace.WriteLine("Reduction Rules");
                Trace.WriteLine("=====================================");
                long count = 0;
                foreach (var formula in database.getAllNonCanonicalFormulas())
                {
                    Trace.WriteLine(new ReductionRule(formula, database.findCanonicalFormula(formula)));
                    count++;
                }
            }


        }

    }

}
