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
using TermSAT.Formulas;

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
        public class DatabaseReportOptions
        {
            public bool ShowReductionRules { get; set; } = false;
            public bool ShowNonCanonicalFormulas { get; set; } = false;
            public bool ShowCanonicalFormulasInLexicalOrder { get; set; } = true;
        }


        public FormulaDatabase Database { get; private set; }
        public DatabaseReportOptions Options { get; private set; }

        public DatabaseReport(FormulaDatabase database, DatabaseReportOptions options= null)
        {
            Database= database;
            Options = (options == null) ? new DatabaseReportOptions() : options;
        }

        public void Run()
        {
            /* 
             * Count # of rules
             */
            Trace.WriteLine("Total number of canonical formulas is " + Database.CountCanonicalFormulas());
            Trace.WriteLine("Total number of non-canonical formulas  is " + Database.CountNonCanonicalFormulas());

            var allTruthTables = Database.GetAllTruthTables();
            Trace.WriteLine("Have found formlas for " + allTruthTables.Count + " of " + TruthTable.MAX_TRUTH_TABLES + " truth tables");
            Trace.WriteLine("The length of the longest canonical formula is " + Database.GetLengthOfLongestCanonicalFormula());
            Trace.WriteLine("");


            /*
             * List lengths and # of canonical formulas
             */
            Trace.WriteLine("TRUTH VALUE     LENGTH    COUNT");
            Trace.WriteLine("                FORMULAS");
            Trace.WriteLine("-------------   ------   ------");
            foreach (var truthTable in allTruthTables)
            {
                var canonicalFormulas = Database.GetCanonicalFormulas(truthTable);

                if (canonicalFormulas.Count <= 0)
                {
                    Trace.WriteLine(truthTable.ToString() + "   *not yet determined*");
                }
                else
                {
                    Trace.WriteLine(
                        truthTable.ToString().PadRight(16) + 
                        canonicalFormulas[0].Length.ToString().PadRight(9) +
                        canonicalFormulas.Count.ToString());

                    foreach (var formula in canonicalFormulas)
                    {
                        Trace.WriteLine("          " + formula.ToString());
                    }
                }
                Trace.WriteLine("-------------   ------   ------");
            }

            if (Options.ShowNonCanonicalFormulas)
            {
                /*
                 * List lengths and # of canonical formulas
                 */
                Trace.WriteLine("TRUTH VALUE     COUNT");
                Trace.WriteLine("                ");
                Trace.WriteLine("-------------   ------");
                foreach (var truthTable in allTruthTables)
                {
                    var nonCanonicalFormulas = Database.GetNonCanonicalFormulas(truthTable);

                    Trace.WriteLine(
                        truthTable.ToString().PadRight(16) +
                        nonCanonicalFormulas.Count.ToString());

                    foreach (var formula in nonCanonicalFormulas)
                    {
                        Trace.WriteLine("          " + formula.ToString());
                    }
                    Trace.WriteLine("-------------   ------   ------");
                }
            }

            if (Options.ShowCanonicalFormulasInLexicalOrder)
            {
                Trace.WriteLine("");
                Trace.WriteLine("Canonical Formulas in Lexical Order");
                Trace.WriteLine("=====================================");
                Database.GetAllCanonicalFormulasInLexicalOrder().ForEach(f =>
                {
                    Trace.WriteLine(f);
                });
            }

            if (Options.ShowReductionRules)
            {
                Trace.WriteLine("");
                Trace.WriteLine("Reduction Rules");
                Trace.WriteLine("=====================================");
                Database.GetAllNonCanonicalFormulas().ForEach(f =>
                {
                    Trace.WriteLine(new ReductionRule(f, Database.FindCanonicalFormula(f)));
                });
            }
        }
    }

}
