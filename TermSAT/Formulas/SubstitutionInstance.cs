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
        private Formula substitutionInstance= null;
        public Formula Generalization { get; }

        public IDictionary<Variable, Formula> Substitutions { get; }

        public SubstitutionInstance(Formula generalization, IDictionary<Variable, Formula> substitutions)
        {
            Generalization = generalization;
            Substitutions = (substitutions == null) ? 
                ImmutableDictionary<Variable, Formula>.Empty : 
                substitutions.ToImmutableDictionary();
        }

        public Formula Instance 
        {
            get
            {
                if (substitutionInstance == null)
                {
                    substitutionInstance= Generalization.CreateSubstitutionInstance(Substitutions);
                }
                return substitutionInstance;
            }
        }
    }


    public partial class Formula
    {
        /**
         * Creates a new formula by making the given substitutions for the 
         * variables in the given formula.  
         */
        abstract public Formula CreateSubstitutionInstance(IDictionary<Variable, Formula> substitutions);

    }

    public partial class Constant : Formula
    {
        override public Formula CreateSubstitutionInstance(IDictionary<Variable, Formula> substitutions) => this;
    }

    public partial class Variable
    {
        override public Formula CreateSubstitutionInstance(IDictionary<Variable, Formula> substitutions)
        {
            if (!substitutions.TryGetValue(this, out Formula f))
                f= this;
            return f;
        }
    }

    public partial class Negation
    {
        override public Formula CreateSubstitutionInstance(IDictionary<Variable, Formula> substitutions)
        {
            Formula child = this.Child;
            Formula f = child.CreateSubstitutionInstance(substitutions);
            if (f == child)
                return this;
            return Negation.NewNegation(f);
        }
    }

    public partial class Implication
    {
        override public Formula CreateSubstitutionInstance(IDictionary<Variable, Formula> substitutions)
        {
            var newAntecedent = Antecedent.CreateSubstitutionInstance(substitutions);
            var newConsequent = Consequent.CreateSubstitutionInstance(substitutions);
            if (!newAntecedent.Equals(Antecedent) || !newConsequent.Equals(Consequent))
                return Implication.NewImplication(newAntecedent, newConsequent);
            return this;
        }
    }


}
