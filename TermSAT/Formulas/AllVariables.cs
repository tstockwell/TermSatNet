/*******************************************************************************
 *     termsat SAT solver
 *     Copyright (C) 2019 Ted Stockwell <emorning@yahoo.com>
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TermSAT.Common;

namespace TermSAT.Formulas
{
    /**
     * Adds the AllVariables property to formulas.
     * 
     * A note about the implementation...
     * 
     * Initially I implemented the Formula class as one monolithic class (over the years I've written TermSAT in BASICA, C++, Java, and C#).
     * And often caching was implemented by adding a reference to the Formula class to hold cached data.
     * 
     * There are a couple of problems with my previous implementations...
     * 
     *  ....there are some features that add to the memory consumed by each formula, but these features are only used in certain situations.
     *      For instance, a feature may only be used during rule generation but not will be used while solving formulas.
     *      It would be desirable for a feature to only have an impact on memory usage when the feature is actually being used.
     *      
     *  ...The Formula class (and related subclasses) got very large and hard to understand/maintain.
     *  
     *  For these reasons I have made the Formula class a partial class and have started breaking features out into 
     *  thier own source files.  This makes for a better separation of concerns and makes it easier to understand/maintain each feature.
     *  
     *  Also, instead of saving references to cached data in the Formula class (which will suck up a small amount of memory 
     *  for each and every formula regardless of whether the feature is used) a ConditionalWeakTable is used for caching, which is a way 
     *  of dynamically adding properties to objects.  
     *  By using the ConditionalWeakTable, memory use is only impacted when the AllVariables property is actually being used.
     *  
     * @author Ted Stockwell <emorning@yahoo.com>
     */
    abstract public partial class Formula 
    {
        /**
         * Returns an ordered list of the variable that occur in this formula.
         * The returned list may be empty.
         */
        abstract public IList<Variable> AllVariables { get; }

    }


    public partial class Constant 
    {
        public override IList<Variable> AllVariables { get => ImmutableList<Variable>.Empty; }
    }

    public partial class Variable : Formula
    {
        private static readonly ConditionalWeakTable<Variable, List<Variable>> __varListCache = new ConditionalWeakTable<Variable, List<Variable>>();

        override public IList<Variable> AllVariables
        {
            get
            {
                List<Variable> variables;
                if (__varListCache.TryGetValue(this, out variables))
                    return variables;

                lock (__varListCache)
                {
                    if (__varListCache.TryGetValue(this, out variables))
                        return variables;
                    variables= new List<Variable>() { this };
                    __varListCache.Add(this, variables);
                }

                return variables;
            }
        }
    }

    public partial class Negation 
    {
        public override IList<Variable> AllVariables { get => Child.AllVariables; }
    }

    public partial class Implication 
    {
        private static readonly ConditionalWeakTable<Implication, IList<Variable>> __varListCache = new ConditionalWeakTable<Implication, IList<Variable>>();

        public override IList<Variable> AllVariables
        {
            get
            {
                IList<Variable> variables;
                if (__varListCache.TryGetValue(this, out variables))
                    return variables;


                lock (__varListCache)
                {
                    // if a variable list is already cached for this formula then return it.
                    if (__varListCache.TryGetValue(this, out variables))
                        return variables;

                    // create a new list
                    var vars = new HashSet<Variable>();
                    vars.UnionWith(Antecedent.AllVariables);
                    vars.UnionWith(Consequent.AllVariables);
                    var sortedVars = new List<Variable>();
                    sortedVars.AddRange(vars);
                    sortedVars.Sort();

                    variables = sortedVars;
                    __varListCache.Add(this, variables);
                }

                return variables;
            }
        }
    }

    public partial class Nand
    {
        private static readonly ConditionalWeakTable<Nand, IList<Variable>> __varListCache = new ConditionalWeakTable<Nand, IList<Variable>>();

        public override IList<Variable> AllVariables
        {
            get
            {
                IList<Variable> variables;
                if (__varListCache.TryGetValue(this, out variables))
                    return variables;


                lock (__varListCache)
                {
                    // if a variable list is already cached for this formula then return it.
                    if (__varListCache.TryGetValue(this, out variables))
                        return variables;

                    // create a new list
                    variables = 
                        Antecedent.AllVariables.Concat(Subsequent.AllVariables).Select(_ => _.Number)
                        .Distinct()
                        .Order()
                        .Select(_ => Variable.NewVariable(_))
                        .ToList();

                    __varListCache.Add(this, variables);
                }

                return variables;
            }
        }
    }


}

