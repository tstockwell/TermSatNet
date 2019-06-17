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
using System.Collections.Generic;
using System.Collections.Immutable;

namespace TermSAT.Formulas
{

    /**
     * A formula and a list of substitutions that will transform the given formula into a 
     * substitution instance of another formula.
     * 
     * Terminology used in TermSAT... 
     * ...the result of applying a substitution to a formula, F, is called a substitution instance of formula F.
     * ...if T is the formula that is the result of applying a substitution to formula F then F is called a 
     *  generalization of formula T
     * 
     * @author ted stockwell
     */
    public class SubstitutionInstance
    {
        public Formula Generalization { get; }

        public IDictionary<Variable, Formula> Substitutions { get; }

        public SubstitutionInstance(Formula generalization, IDictionary<Variable, Formula> substitutions)
        {
            Generalization = generalization;

            if (substitutions == null)
            {
                substitutions = new Dictionary<Variable, Formula>(0).ToImmutableDictionary();
            }
            else
                substitutions = substitutions.ToImmutableDictionary();
        }
    }

}
