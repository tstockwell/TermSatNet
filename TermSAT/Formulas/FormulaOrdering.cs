using System;
using System.Collections;
using System.Collections.Generic;

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
        public static FlatTerm AsFlatTerm(this Formula formula) => FlatTerm.GetFlatTerm(formula);

        /**
         * Creates a new formula from a DFS ordering and some changes
         */
        public static Formula ToFormula(this FlatTerm sequence, IDictionary<int, Formula> replacements)
        {
            Formula formula;

            Stack<Formula> stack = new Stack<Formula>();
            for (int i = sequence.Length; 0 < i--;)
            {
                if (!replacements.TryGetValue(i, out Formula subformula))
                    subformula = sequence[i];
                if (subformula is Negation)
                {
                    Formula f = stack.Pop();
                    stack.Push(Negation.NewNegation(f));
                }
                else if (subformula is Implication)
                {
                    Formula antecendent = stack.Pop();
                    Formula consequent = stack.Pop();
                    stack.Push(Implication.NewImplication(antecendent, consequent));
                }
                else if (subformula is Variable)
                {
                    stack.Push(subformula);
                }
                else if (subformula == Constant.TRUE)
                {
                    stack.Push(Constant.TRUE);
                }
                else if (subformula == Constant.FALSE)
                {
                    stack.Push(Constant.FALSE);
                }
                else
                    throw new Exception("wtf");
            }

            if (stack.Count != 1)
                throw new Exception("hmm, looks like an invalid formula DFS ordering");

            formula = stack.Pop();
            return formula;
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


    /**
     * This class enumerates all of a Formula's subformulas in the same order as they 
     * are written.  Basically that means that implication antecedents come before consequents.
     * The first formula returned is the enumerated formula itself.
     */
    public class FormulaDFSEnumerator : IEnumerator<Formula>
    {
        private readonly Formula formula;
        private readonly Stack<Formula> stack = new Stack<Formula>();

        public Formula Current { get; private set; }

        object IEnumerator.Current { get { return Current; } }

        public FormulaDFSEnumerator(Formula formula)
        {
            this.formula = formula;
        }

        public void Dispose() { /* do nothing */ }

        public bool MoveNext()
        {
            if (Current == null)
            {
                Current = formula;
                return true;
            }
            if (Current is Constant || Current is Variable)
            {
                if (stack.Count <= 0)
                    return false;
                Current = stack.Pop();
                return true;
            }
            if (Current is Negation)
            {
                Current = (Current as Negation).Child;
                return true;
            }
            if (Current is Nand)
            {
                var nand = Current as Nand;
                stack.Push(nand.Subsequent);
                Current = nand.Antecedent;
                return true;
            }
            if (Current is Implication)
            {
                stack.Push((Current as Implication).Consequent);
                Current = (Current as Implication).Antecedent;
                return true;
            }

            throw new Exception($"unknown formula type: {Current.GetType().FullName}");

        }

        public void Reset()
        {
            this.Current = null;
        }
    }



}


