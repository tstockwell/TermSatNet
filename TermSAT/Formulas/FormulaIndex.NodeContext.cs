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
using System.Linq;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;
using static TermSAT.Formulas.FormulaIndex;

namespace TermSAT.Formulas;

public static partial class FormulaIndex
{
    public class NodeContext : DbContext
    {
        public NodeContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<Node> Nodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Node>().Property(f => f.Id).IsRequired().ValueGeneratedOnAdd();
            modelBuilder.Entity<Node>().Property(f => f.Parent).IsRequired();
            modelBuilder.Entity<Node>().Property(f => f.Key).IsRequired();
            modelBuilder.Entity<Node>().Property(f => f.Value).IsRequired();

            modelBuilder.Entity<Node>().HasKey(f => f.Id);
            modelBuilder.Entity<Node>().HasIndex(f => f.Parent);
            modelBuilder.Entity<Node>().HasIndex(_ => new { _.Parent, _.Key});

            modelBuilder.Entity<Node>(f => f.ToTable(nameof(NodeContext.Nodes)));
        }

        public override int SaveChanges()
        {
            var result = base.SaveChanges();
            return result;
        }

        /// <summary>
        /// I think that ef still tracks after an add/ssavechanges.
        /// So, periodically detach any entries.
        /// </summary>
        public void Clear() => this.ChangeTracker.Clear();

    }
}

