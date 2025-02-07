using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TermSAT.Formulas
{


    /// <summary>
    ///   Defines an interface to a list of formulas in some order.
    /// </summary>
    public interface IFormulaOrdering : IEnumerable<Formula>
    {
        Formula this[int index] { get; }

        int Length { get; }
    }


    public static class FormulaSequenceExtensions
    {
        public static Formula[] AsFlatTerm(this Formula formula)
        {
            Formula[] flatterm = new Formula[formula.Length];
            int i = 0;
            var terms = new FormulaDFSEnumerator(formula);
            while (terms.MoveNext())
            {
                flatterm[i++] = terms.Current;
            }
            return flatterm;
        }

        public static Formula AsFormula(this IEnumerable<Formula> flatterm)
        {
            var builder = new StringBuilder();
            foreach (var term in flatterm)
            {
                if (term is Nand)
                {
                    builder.Append('|');

                }
                else
                {
                    builder.Append(term.ToString());
                }
            }

            var result = Formula.GetOrParse(builder.ToString());
            return result;
        }

        /**
         * Creates a new formula from a DFS ordering and some changes
         */
        public static Formula WithReplacements(this Formula[] flatterm, IDictionary<int, Formula> replacements)
        {
            var sequence = new List<Formula>(flatterm);
            foreach (var index in replacements.Keys.OrderDescending())
            {
                var formula = flatterm[index];
                var replacementFormula = replacements[index];
                sequence = sequence.GetRange(0, index)
                    .Concat(replacementFormula.AsFlatTerm())
                    .Concat(sequence.GetRange(index + formula.Length, sequence.Count - index - formula.Length))
                    .ToList();
            }

            var result = sequence.AsFormula();
            return result;
        }

    }

    public partial class Formula
    {
        abstract public Formula GetFormulaAtPosition(int index);
        abstract public int PositionOf(Formula subterm);
    }

    public partial class Constant
    {
        override public Formula GetFormulaAtPosition(int index)
        {
            if (index != 0)
                throw new TermSatException("Invalid symbol position:" + index + "in formula " + ToString());
            return this;
        }
        override public int PositionOf(Formula subterm)
        {
            if (this.Equals(subterm))
            {
                return 0;
            }
            return -1;
        }
    }

    public partial class Variable
    {
        override public Formula GetFormulaAtPosition(int index)
        {
            if (index != 0)
                throw new TermSatException("Invalid symbol position:" + index + "in formula " + ToString());
            return this;
        }
        override public int PositionOf(Formula subterm)
        {
            if (this.Equals(subterm))
            {
                return 0;
            }
            return -1;
        }
    }

    public partial class Negation : Formula
    {
        override public Formula GetFormulaAtPosition(int index)
        {
            if (index < 0)
                throw new TermSatException("Invalid symbol position:" + index + "in formula " + ToString());
            if (index == 0)
                return this;
            return Child.GetFormulaAtPosition(index - 1);
        }
        override public int PositionOf(Formula subterm)
        {
            if (this.Equals(subterm))
            {
                return 0;
            }

            int childPosition = Child.PositionOf(subterm);
            if (0 <= childPosition)
            {
                return childPosition + 1;
            }
            return -1;
        }
    }

    public partial class Implication
    {
        override public Formula GetFormulaAtPosition(int index)
        {
            if (index < 0)
                throw new TermSatException("Invalid symbol position:" + index + "in formula " + ToString());
            if (index == 0)
                return this;
            int a = Antecedent.Length;
            if (index <= a)
                return Antecedent.GetFormulaAtPosition(index - 1);
            return Consequent.GetFormulaAtPosition(index - a - 1);
        }
        override public int PositionOf(Formula subterm)
        {
            if (this.Equals(subterm))
            {
                return 0;
            }

            int antecedentPosition = Antecedent.PositionOf(subterm);
            if (0 <= antecedentPosition)
            {
                return antecedentPosition + 1;
            }

            int consequentPosition = Consequent.PositionOf(subterm);
            if (0 <= consequentPosition)
            {
                return consequentPosition + Antecedent.Length + 1;
            }

            return -1;
        }
    }

    public partial class Nand
    {
        override public Formula GetFormulaAtPosition(int index)
        {
            if (index < 0)
            {
                throw new TermSatException("invalid formula index:" + index + "in formula " + ToString());
            }
            if (index == 0)
            {
                return this;
            }
            if (!(index < Length))
            {
                throw new TermSatException($"index of {index} exceeds formula length of {Length} in formula {this}");
            }
            int a = Antecedent.Length;
            if (index <= a)
                return Antecedent.GetFormulaAtPosition(index - 1);
            return Subsequent.GetFormulaAtPosition(index - a - 1);
        }
        override public int PositionOf(Formula subterm)
        {
            if (this.Equals(subterm))
            {
                return 0;
            }

            if (this.Length <= subterm.Length)
            {
                return -1;
            }

            int antecedentPosition = Antecedent.PositionOf(subterm);
            if (0 <= antecedentPosition)
            {
                return antecedentPosition + 1;
            }

            int consequentPosition = Subsequent.PositionOf(subterm);
            if (0 <= consequentPosition)
            {
                return consequentPosition + Antecedent.Length + 1;
            }

            return -1;
        }
    }



}


