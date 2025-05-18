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

        public IReadOnlyDictionary<Variable, Formula> Substitutions { get; }

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
        abstract public Formula CreateSubstitutionInstance(IReadOnlyDictionary<Variable, Formula> substitutions);

        /**
         * Creates a new formula by replacing all occurrences of the 'target' formula with the 'replacement' formula.
         */
        abstract public Formula ReplaceAll(Formula target, Formula replacement);

        /**
         * Creates a new formula by replacing the formula at the given position 
         */
        abstract public Formula ReplaceAt(int targetPosition, Formula replacement);
    }

    public partial class Constant : Formula
    {
        override public Formula CreateSubstitutionInstance(IReadOnlyDictionary<Variable, Formula> substitutions) => this;
        override public Formula ReplaceAll(Formula target, Formula replacement)
        {
            if (target.Equals(this))
            {
                return replacement;
            }
            return this;
        }
        override public Formula ReplaceAt(int targetPosition, Formula replacement)
        {
            if (0 == targetPosition)
            {
                return replacement;
            }
            return this;
        }
    }

    public partial class Variable
    {
        override public Formula CreateSubstitutionInstance(IReadOnlyDictionary<Variable, Formula> substitutions)
        {
            if (!substitutions.TryGetValue(this, out Formula f))
                f= this;
            return f;
        }
        override public Formula ReplaceAll(Formula target, Formula replacement)
        {
            if (target.Equals(this))
            {
                return replacement;
            }
            return this;
        }
        override public Formula ReplaceAt(int targetPosition, Formula replacement)
        {
            if (targetPosition <= 0)
            {
                return replacement;
            }
            return this;
        }
    }

    public partial class Negation
    {
        override public Formula CreateSubstitutionInstance(IReadOnlyDictionary<Variable, Formula> substitutions)
        {
            Formula child = this.Child;
            Formula f = Child.CreateSubstitutionInstance(substitutions);
            if (Child.Equals(f))
            {
                return this;
            }
            return Negation.NewNegation(f);
        }
        override public Formula ReplaceAll(Formula target, Formula replacement)
        {
            if (target.Equals(this))
            {
                return replacement;
            }

            var newChild = Child.ReplaceAll(target, replacement);
            if (!newChild.Equals(Child))
            {
                return Negation.NewNegation(newChild);
            }

            return this;
        }
        override public Formula ReplaceAt(int targetPosition, Formula replacement)
        {
            if (targetPosition <= 0)
            {
                return replacement;
            }
            else
            {
                var newChild = Child.ReplaceAt(targetPosition - 1, replacement);
                if (!newChild.Equals(Child))
                {
                    return Negation.NewNegation(newChild);
                }
            }

            return this;
        }
    }

    public partial class Implication
    {
        override public Formula CreateSubstitutionInstance(IReadOnlyDictionary<Variable, Formula> substitutions)
        {
            var newAntecedent = Antecedent.CreateSubstitutionInstance(substitutions);
            var newConsequent = Consequent.CreateSubstitutionInstance(substitutions);
            if (!newAntecedent.Equals(Antecedent) || !newConsequent.Equals(Consequent))
                return Implication.NewImplication(newAntecedent, newConsequent);
            return this;
        }
        override public Formula ReplaceAll(Formula target, Formula replacement)
        {
            if (target.Equals(this))
            {
                return replacement;
            }

            var newAntecedent = Antecedent.ReplaceAll(target, replacement);
            var newConsequent = Consequent.ReplaceAll(target, replacement);
            if (!newAntecedent.Equals(Antecedent) || !newConsequent.Equals(Consequent))
            {
                return Implication.NewImplication(newAntecedent, newConsequent);
            }

            return this;
        }
        override public Formula ReplaceAt(int targetPosition, Formula replacement)
        {
            if (targetPosition < Antecedent.Length)
            {
                var newAntecedent = Antecedent.ReplaceAt(targetPosition, replacement);
                if (!newAntecedent.Equals(Antecedent))
                {
                    return Implication.NewImplication(newAntecedent, Consequent);
                }
            }
            else
            {
                var newConsequent = Consequent.ReplaceAt(targetPosition - Antecedent.Length - 1, replacement);
                if (!newConsequent.Equals(Antecedent))
                {
                    return Implication.NewImplication(Antecedent, newConsequent);
                }
            }

            return this;
        }

    }


    public partial class Nand
    {
        override public Formula CreateSubstitutionInstance(IReadOnlyDictionary<Variable, Formula> substitutions)
        {
            var newAntecedent = Antecedent.CreateSubstitutionInstance(substitutions);
            var newConsequent = Subsequent.CreateSubstitutionInstance(substitutions);
            if (!newAntecedent.Equals(Antecedent) || !newConsequent.Equals(Subsequent))
                return Nand.NewNand(newAntecedent, newConsequent);
            return this;
        }
        override public Formula ReplaceAll(Formula target, Formula replacement)
        {
            if (target.Equals(this))
            {
                return replacement;
            }

            var newAntecedent = Antecedent.ReplaceAll(target, replacement);
            var newConsequent = Subsequent.ReplaceAll(target, replacement);
            if (!newAntecedent.Equals(Antecedent) || !newConsequent.Equals(Subsequent))
            {
                return Nand.NewNand(newAntecedent, newConsequent);
            }

            return this;
        }
        override public Formula ReplaceAt(int targetPosition, Formula replacement)
        {
            if (targetPosition < 0)
            {
                return this;
            }
            else if (targetPosition == 0)
            {
                return replacement;
            }
            else if (targetPosition <= Antecedent.Length)
            {
                var newAntecedent = Antecedent.ReplaceAt(targetPosition - 1, replacement);
                if (!newAntecedent.Equals(Antecedent))
                {
                    return Nand.NewNand(newAntecedent, Subsequent);
                }
            }
            else
            {
                var newConsequent = Subsequent.ReplaceAt(targetPosition - Antecedent.Length - 1, replacement);
                if (!newConsequent.Equals(Subsequent))
                {
                    return Nand.NewNand(Antecedent, newConsequent);
                }
            }

            return this;
        }

    }

}
