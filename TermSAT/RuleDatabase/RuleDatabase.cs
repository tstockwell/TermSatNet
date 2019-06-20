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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using TermSAT.Formulas;

namespace TermSAT.RuleDatabase
{

    public class FormulaRecord
    {
        [Key]
        public int Id { get; set; }
        public string Text { get; set; }
        public int Length { get; set; }
        public string TruthValue { get; set; }
        public bool IsCanonical { get; set; }
    }


    public class RuleDatabaseContext : DbContext
    {
        public DbSet<FormulaRecord> Formulas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FormulaRecord>().Property(f => f.Text).IsRequired();
            modelBuilder.Entity<FormulaRecord>().Property(f => f.Length).IsRequired();
            modelBuilder.Entity<FormulaRecord>().Property(f => f.TruthValue).IsRequired();

            modelBuilder.Entity<FormulaRecord>().HasKey(f => f.Id);
            modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.Text);
            modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.Length);
            modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.TruthValue);
            modelBuilder.Entity<FormulaRecord>().HasIndex(f => f.IsCanonical);

            modelBuilder.Entity<FormulaRecord>().Property(f => f.IsCanonical).HasDefaultValue(false);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=rules-" + TruthTable.VARIABLE_COUNT + ".db");
        }

        /// <summary>
        /// I think that ef still tracks after an add/ssavechanges.
        /// So, periodically detach any entries.
        /// </summary>
        public void DetachAllEntities()
        {
            var changedEntriesCopy = this.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in changedEntriesCopy)
                entry.State = EntityState.Detached;
        }
    }


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
    public class RuleDatabase
    {

        RuleDatabaseContext ruleContext;

        public RuleDatabase()
        {
            ruleContext = new RuleDatabaseContext();

            // no tracking
            ruleContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public Formula GetLastGeneratedFormula()
        {
            var record = this.ruleContext.Formulas
                .OrderByDescending(f => f.Id)
                .First();

            var formula = record != null ? Formula.Parse(record.Text) : null;
            return formula;
        }

        public List<Formula> GetCanonicalFormulas(TruthTable truthTable)
        {
            var records = ruleContext.Formulas
                .Where(f => f.TruthValue == truthTable.ToString() && f.IsCanonical == true)
                .OrderBy(f => f.Id)
                .ToList();
            var formulas = records.Select(r => Formula.Parse(r.Text)).ToList();
            return formulas;
        }


        public void Shutdown()
        {
            ruleContext.Dispose();
        }

        public int GetLengthOfLongestCanonicalFormula()
        {
            var formula = ruleContext.Formulas
                .Where(f => f.IsCanonical == true)
                .OrderByDescending(f => f.Length)
                .First();

            if (formula == null)
                return 0;

            return formula.Length;
        }

        /**
         * Formulas longer than this length are guaranteed to be reducable with rules,
         * generated from previous formulas. 
         * Therefore processing can stop when formulas get this long.
         */
        public int LengthOfLongestPossibleNonReducableFormula()
        {
            int maxLength = GetLengthOfLongestCanonicalFormula();
            if (maxLength <= 0) // we don't know the length of longest formula yet
                return int.MaxValue;
            return maxLength * 2 + 1;
        }


        public List<Formula> FindCanonicalFormulasByLength(int size)
        {
            var records = ruleContext.Formulas
                .Where(f => f.Length == size && f.IsCanonical == true)
                .OrderBy(f => f.Id)
                .ToList();
            var formulas = records.Select(r => r.Text.ToFormula()).ToList();
            return formulas;
        }

        public void AddFormula(Formula formula, bool isCanonical)
        {
            var record = new FormulaRecord
            {
                Text = formula.ToString(),
                IsCanonical = isCanonical,
                Length = formula.Length,
                TruthValue = TruthTable.NewTruthTable(formula).ToString()
            };

            ruleContext.Formulas.Add(record);
            ruleContext.SaveChanges();
        }

        public List<Formula> GetAllNonCanonicalFormulas(int maxLength)
        {
            var records = ruleContext.Formulas
                .Where(f => f.Length <= maxLength && f.IsCanonical == false)
                .ToList();
            var formulas = records.Select(r => r.Text.ToFormula()).ToList();
            return formulas;
        }
        public List<Formula> GetAllNonCanonicalFormulas()
        {
            var records = ruleContext.Formulas
                .Where(f => f.IsCanonical == false)
                .OrderBy(f => f.Length)
                .ThenBy(f => f.Text)
                .ToList();
            var formulas = records.Select(r => r.Text.ToFormula()).ToList();
            return formulas;
        }
        public List<Formula> GetAllCanonicalFormulasInLexicalOrder()
        {
            var records = ruleContext.Formulas
                .Where(f => f.IsCanonical == true)
                .OrderBy(f => f.Text)
                .ToList();
            var formulas = records.Select(r => r.Text.ToFormula()).ToList();
            return formulas;
        }
        public List<TruthTable> GetAllTruthTables()
        {
            var truthValues = ruleContext.Formulas
                .Where(f => f.IsCanonical == true)
                .OrderBy(f => f.Text)
                .Select(f => f.TruthValue)
                .Distinct()
                .ToList();
            var truthTables = truthValues.Select(v => TruthTable.NewTruthTable(v)).ToList();
            return truthTables;
        }

        public int GetLengthOfCanonicalFormulas(TruthTable truthTable)
        {
            var formula = ruleContext.Formulas
                .Where(f => f.IsCanonical == true && f.TruthValue == truthTable.ToString())
                .OrderBy(f => f.Id)
                .First();

            if (formula == null)
                return 0;

            return formula.Length;
        }

        /**
         * Finds the canonical form of the given formula.
         */
        public Formula FindCanonicalFormula(Formula formula)
        {
            var truthTableText = TruthTable.NewTruthTable(formula).ToString();

            var record = ruleContext.Formulas
                .AsNoTracking()
                .Where(f => f.IsCanonical == true && f.TruthValue == truthTableText)
                .OrderBy(f => f.Id)
                .First();

            if (record == null)
                return null;

            var canonicalFormula = Formula.Parse(record.Text);

            return canonicalFormula;
        }

        public int CountNonCanonicalFormulas()
        {
            var count = ruleContext.Formulas
                .Where(f => f.IsCanonical == false)
                .Count();
            return count;
        }

        public int CountCanonicalFormulas()
        {
            var count = ruleContext.Formulas
                .Where(f => f.IsCanonical == true)
                .Count();
            return count;
        }


        public long CountCanonicalTruthTables()
        {
            var count = ruleContext.Formulas
                .Where(f => f.IsCanonical == true)
                .Select(f => f.TruthValue)
                .Distinct()
                .Count();
            return count;
        }

        public List<Formula> GetAllFormulas(TruthTable truthTable)
        {
            var records = ruleContext.Formulas
                .Where(f => f.TruthValue == truthTable.ToString())
                .ToList();
            var formulas = records.Select(r => r.Text.ToFormula()).ToList();
            return formulas;
        }

    }

}

