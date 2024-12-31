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


        public void Dispose()
        {
            // do nothing
        }
    }

}

