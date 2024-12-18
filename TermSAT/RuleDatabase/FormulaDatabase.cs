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
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TermSAT.Formulas;

namespace TermSAT.RuleDatabase
{

    /// <summary>
    /// TODO:This class is legacy and should be removed.  It been replaced by RuleDatabaseContext.
    /// </summary>
    public class FormulaDatabase : IDisposable
    {
        string DataSource { get; set; }


        /// <param name="datasource">path to a file, or ':memory:' to create a memory-based db</param>
        public FormulaDatabase(string datasource)
        {
            //ConnectionString = "DataSource=" + datasource + ";Pooling=True;Max Pool Size=100;";
            DataSource = datasource;
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                ctx.Database.EnsureCreated();
            }
        }

        public void Clear()
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                ctx.Clear();
            }
        }

        public Formula GetLastGeneratedFormula()
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.GetLastGeneratedFormula();
            }
        }

        public List<Formula> GetCanonicalFormulas(TruthTable truthTable)
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.GetCanonicalFormulas(truthTable);
            }
        }

        public List<FormulaRecord> GetAllFormulaRecords()
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.GetAllFormulaRecords();
            }
        }

        public List<Formula> GetNonCanonicalFormulas(TruthTable truthTable)
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.GetNonCanonicalFormulas(truthTable);
            }
        }


        public int GetLengthOfLongestCanonicalFormula()
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.GetLengthOfLongestCanonicalFormula();
            }
        }

        /**
         * Formulas longer than this length are guaranteed to be reducible with rules,
         * generated from previous formulas. 
         * Therefore processing can stop when formulas get this long.
         */
        public int LengthOfLongestPossibleNonReducibleFormula()
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.LengthOfLongestPossibleNonReducibleFormula();
            }
        }


        public List<Formula> FindCanonicalFormulasByLength(int size)
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.FindCanonicalFormulasByLength(size);
            }
        }

        /// <summary>
        /// Note, using 0 for id (a field in the primary key) causes the Sqlite driver to use the Sqlite auto increment value
        /// </summary>
        public void AddFormula(Formula formula, bool isCanonical) => AddFormula(0, formula, isCanonical);

        public void AddFormula(int id, Formula formula, bool isCanonical)
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                ctx.AddFormula(id, formula, isCanonical);
            }
        }
        public async Task IsSubsumedBySchemeAsync(Formula formula, string value)
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                await ctx.IsSubsumedBySchemeAsync(formula, value);
            }
        }

        public List<Formula> GetAllNonCanonicalFormulas(int maxLength)
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.GetAllNonCanonicalFormulas(maxLength);
            }
        }

        public List<Formula> GetAllNonCanonicalFormulas()
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.GetAllNonCanonicalFormulas();
            }
        }

        public List<Formula> GetAllCanonicalFormulas()
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.GetAllCanonicalFormulas();
            }
        }

        public List<Formula> GetAllCanonicalFormulasInLexicalOrder()
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.GetAllCanonicalFormulasInLexicalOrder();
            }
        }

        public List<TruthTable> GetAllTruthTables()
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.GetAllTruthTables();
            }
        }

        public int GetLengthOfCanonicalFormulas(TruthTable truthTable)
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.GetLengthOfCanonicalFormulas(truthTable);
            }
        }

        /**
         * Finds the canonical form of the given formula.
         */
        public Formula FindCanonicalFormula(Formula formula)
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.FindCanonicalFormula(formula);
            }
        }

        public int CountNonCanonicalFormulas()
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.CountNonCanonicalFormulas();
            }
        }

        public int CountCanonicalFormulas()
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.CountCanonicalFormulas();
            }
        }


        public long CountCanonicalTruthTables()
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.CountCanonicalTruthTables();
            }
        }

        public List<Formula> GetAllFormulas(TruthTable truthTable)
        {
            using (var ctx = RuleDatabaseContext.GetDatabaseContext(DataSource))
            {
                return ctx.GetAllFormulas(truthTable);
            }
        }

        public void Dispose()
        {
            // do nothing
        }
    }

}

