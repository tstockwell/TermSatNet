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
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TermSAT.Formulas;

namespace TermSAT.RuleDatabase
{


    /**
     * An API for accessing the rule database.
     * The rule database is an enumeration of all formulas with [VARIABLE_COUNT] 
     * variables, up to a length determined by the RuleGenerator.
     * The database used to create a set of rewrite rules.
     * 
     * The rule database is a single table named FORMULA with the following columns...
     * 		FORMULA 	- a textual representation of the formula
     * 		LENGTH 		- the length of the textual representation
     * 		TRUTHVALUE 	- the truth value
     * 		CANONICAL 	- indicates that the formula is one of the shortest formulas 
     * 					  with the associated truth value
     * 		ID 			- a number assigned to the formula  
     * 
     * 
     * @author Ted Stockwell
     *
     */
    public class FormulaDatabase : IDisposable
    {

        string ConnectionString { get; set; }
        SqliteConnection Connection {  get; set; }


        /// <param name="datasource">path to a file, or ':memory:' to create a memory-based db</param>
        public FormulaDatabase(string datasource)
        {
            //ConnectionString = "DataSource=" + datasource + ";Pooling=True;Max Pool Size=100;";
            ConnectionString = "DataSource=" + datasource;
            Connection = new SqliteConnection(ConnectionString);
            Connection.Open();
            using (var ctx = GetDatabaseContext())
            {
                ctx.Database.EnsureCreated();
            }
        }


        public RuleDatabaseContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder()
                .UseSqlite(Connection)
                .Options;

            return new RuleDatabaseContext(options);
        }

        public void Clear()
        {
            using (var ctx = GetDatabaseContext())
            {
                ctx.Database.ExecuteSqlRaw("DELETE FROM FormulaRecords");
            }
        }

        public Formula GetLastGeneratedFormula()
        {
            using (var ctx = GetDatabaseContext())
            {
                var record = ctx.FormulaRecords.AsNoTracking()
                    .OrderByDescending(f => f.Id)
                    .FirstOrDefault();

                var formula = record != null ? Formula.Parse(record.Text) : null;
                return formula;
            }
        }

        public List<Formula> GetCanonicalFormulas(TruthTable truthTable)
        {
            using (var ctx = GetDatabaseContext())
            {
                var records = ctx.FormulaRecords.AsNoTracking()
                    .Where(f => f.TruthValue == truthTable.ToString() && f.IsCanonical == true)
                    .OrderBy(f => f.Id)
                    .ToList();
                var formulas = records.Select(r => Formula.Parse(r.Text)).ToList();
                return formulas;
            }
        }
        public List<FormulaRecord> GetAllFormulaRecords()
        {
            using (var ctx = GetDatabaseContext())
            {
                var records = ctx.FormulaRecords.AsNoTracking()
                    .OrderBy(f => f.Id)
                    .ToList();
                return records;
            }
        }

        public List<Formula> GetNonCanonicalFormulas(TruthTable truthTable)
        {
            using (var ctx = GetDatabaseContext())
            {
                var records = ctx.FormulaRecords.AsNoTracking()
                    .Where(f => f.TruthValue == truthTable.ToString() && f.IsCanonical == false)
                    .OrderBy(f => f.Id)
                    .ToList();
                var formulas = records.Select(r => Formula.Parse(r.Text)).ToList();
                return formulas;
            }
        }


        public int GetLengthOfLongestCanonicalFormula()
        {
            using (var ctx = GetDatabaseContext())
            {
                var formula = ctx.FormulaRecords.AsNoTracking()
                    .Where(f => f.IsCanonical == true)
                    .OrderByDescending(f => f.Length)
                    .FirstOrDefault();

                if (formula == null)
                    return 0;

                return formula.Length;
            }
        }

        /**
         * Formulas longer than this length are guaranteed to be reducible with rules,
         * generated from previous formulas. 
         * Therefore processing can stop when formulas get this long.
         */
        public int LengthOfLongestPossibleNonReducibleFormula()
        {
            int maxLength = GetLengthOfLongestCanonicalFormula();
            if (maxLength <= 0) // we don't know the length of longest formula yet
                return int.MaxValue;
            return maxLength * 2 + 1;
        }


        public List<Formula> FindCanonicalFormulasByLength(int size)
        {
            using (var ctx = GetDatabaseContext())
            {
                var records = ctx.FormulaRecords.AsNoTracking()
                    .Where(f => f.Length == size && f.IsCanonical == true)
                    .OrderBy(f => f.Id)
                    .ToList();
                var formulas = records.Select(r => r.Text.ToFormula()).ToList();
                return formulas;
            }
        }

        /// <summary>
        /// Note, using 0 for id (a field in the primary key) causes the Sqlite driver to use the Sqlite auto increment value
        /// </summary>
        public void AddFormula(Formula formula, bool isCanonical) => AddFormula(0, formula, isCanonical);

        public void AddFormula(int id, Formula formula, bool isCanonical)
        {
            using (var ctx = GetDatabaseContext())
            {
                var record = new FormulaRecord(id, formula, isCanonical);

                ctx.FormulaRecords.Add(record);
                ctx.SaveChanges();
                ctx.Clear();
            }
        }
        public async Task IsSubsumedBySchemeAsync(Formula formula, string value)
        {
            using (var ctx = GetDatabaseContext())
            {
                var record = await ctx.FormulaRecords
                    .Where(f => f.Text.Equals(formula.ToString()))
                    .FirstOrDefaultAsync();
                if (record == null)
                {
                    throw new Exception($"formula not found in database:{formula}");
                }

                record.IsSubsumedByScheme = value;

                await ctx.SaveChangesAsync();
                ctx.Clear();
            }
        }

        public List<Formula> GetAllNonCanonicalFormulas(int maxLength)
        {
            using (var ctx = GetDatabaseContext())
            {
                var records = ctx.FormulaRecords.AsNoTracking()
                    .Where(f => f.Length <= maxLength && f.IsCanonical == false)
                    .OrderBy(f => f.Id)
                    .ToList();
                var formulas = records.Select(r => r.Text.ToFormula()).ToList();
                return formulas;
            }
        }
        public List<Formula> GetAllNonCanonicalFormulas()
        {
            using (var ctx = GetDatabaseContext())
            {
                var records = ctx.FormulaRecords.AsNoTracking()
                    .Where(f => f.IsCanonical == false)
                    .OrderBy(f => f.Id)
                    .ToList();
                var formulas = records.Select(r => r.Text.ToFormula()).ToList();
                return formulas;
            }
        }
        public List<Formula> GetAllCanonicalFormulas()
        {
            using (var ctx = GetDatabaseContext())
            {
                var records = ctx.FormulaRecords.AsNoTracking()
                    .Where(f => f.IsCanonical == true)
                    .OrderBy(f => f.Id)
                    .ToList();
                var formulas = records.Select(r => r.Text.ToFormula()).ToList();
                return formulas;
            }
        }
        public List<Formula> GetAllCanonicalFormulasInLexicalOrder()
        {
            using (var ctx = GetDatabaseContext())
            {
                var records = ctx.FormulaRecords.AsNoTracking()
                    .Where(f => f.IsCanonical == true)
                    .OrderBy(f => f.Text)
                    .ToList();
                var formulas = records.Select(r => r.Text.ToFormula()).ToList();
                return formulas;
            }
        }
        public List<TruthTable> GetAllTruthTables()
        {
            using (var ctx = GetDatabaseContext())
            {
                var truthValues = ctx.FormulaRecords.AsNoTracking()
                    .OrderBy(f => f.TruthValue)
                    .Select(f => f.TruthValue)
                    .Distinct()
                    .ToList();
                var truthTables = truthValues.Select(v => TruthTable.NewTruthTable(v)).ToList();
                return truthTables;
            }
        }

        public int GetLengthOfCanonicalFormulas(TruthTable truthTable)
        {
            using (var ctx = GetDatabaseContext())
            {
                var formula = ctx.FormulaRecords.AsNoTracking()
                    .Where(f => f.IsCanonical == true && f.TruthValue == truthTable.ToString())
                    .OrderBy(f => f.Id)
                    .FirstOrDefault();

                if (formula == null)
                    return 0;

                return formula.Length;
            }
        }

        /**
         * Finds the canonical form of the given formula.
         */
        public Formula FindCanonicalFormula(Formula formula)
        {
            using (var ctx = GetDatabaseContext())
            {
                return ctx.FindCanonicalFormula(TruthTable.NewTruthTable(formula));
            }
        }

        public int CountNonCanonicalFormulas()
        {
            using (var ctx = GetDatabaseContext())
            {
                var count = ctx.FormulaRecords.AsNoTracking()
                    .Where(f => f.IsCanonical == false)
                    .Count();
                return count;
            }
        }

        public int CountCanonicalFormulas()
        {
            using (var ctx = GetDatabaseContext())
            {
                var count = ctx.FormulaRecords.AsNoTracking()
                    .Where(f => f.IsCanonical == true)
                    .Count();
                return count;
            }
        }


        public long CountCanonicalTruthTables()
        {
            using (var ctx = GetDatabaseContext())
            {
                var count = ctx.FormulaRecords.AsNoTracking()
                    .Where(f => f.IsCanonical == true)
                    .Select(f => f.TruthValue)
                    .Distinct()
                    .Count();
                return count;
            }
        }

        public List<Formula> GetAllFormulas(TruthTable truthTable)
        {
            using (var ctx = GetDatabaseContext())
            {
                var records = ctx.FormulaRecords.AsNoTracking()
                    .Where(f => f.TruthValue == truthTable.ToString())
                    .ToList();
                var formulas = records.Select(r => r.Text.ToFormula()).ToList();
                return formulas;
            }
        }

        public void Dispose()
        {
            // do nothing
        }
    }

}

