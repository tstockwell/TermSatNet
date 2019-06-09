using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TermSAT.Common;

namespace TermSAT.Formulas
{
    /// <summary>
    /// This source module extends the Formula class with an API that represents Formulas as an array of all the 
    /// subformulas in the formula, ordered from the leftmost subformula (the forumula itself) to the rightmost 
    /// subformula (the last constant or variable that appears in the formula).
    /// Such an array is called a 'symbol sequence'.
    /// 
    /// A SymbolSequence is a representation of a Formula as a sequence of symbols, as opposed to a tree of objects.
    /// SymbolSequence are like strings, but not strings of characters, SymbolSequence are strings of the subformulas 
    /// of a formula as enumerated from right to left.
    /// 
    /// SymbolSequence are useful for constructing indexes of sets of formulas.
    /// The indexes built from SymbolSequence make it possible to quickly find substitution instances and unifications 
    /// of a given formula in a large set of formulas.
    /// 
    /// Example:
    /// Given the formula...
    ///     *a*-bc
    /// ...the associated SymbolSequence is
    ///     { *, a, *-bc, -b, c }
    ///     
    /// </summary>
    public interface SymbolSequence : IEnumerable<Formula>, IComparable<SymbolSequence>, IEquatable<SymbolSequence>
    {
        /// <summary>
        /// return the subformula that starts at a given position within this formula 
        /// </summary>
        /// <returns></returns>
        Formula FormulaAt(int position);

        /// <summary>
        /// returns the Formula from which the SymbolSequence is built
        /// </summary>
        Formula EnumeratedFormula {  get; }
    }


    public partial class Formula : SymbolSequence
    {
        public Formula EnumeratedFormula { get { return this; } }

        abstract public Formula FormulaAt(int position);

        /// <summary>
        /// The Formula.CompareTo method orderes formulas by thier 'simplicity'.
        /// SymbolSequences essentially order formulas lexically.
        /// </summary>
        public int CompareTo(SymbolSequence other)
        {
            return CompareTo(other as Formula);
        }

        public bool Equals(SymbolSequence other)
        {
            return EnumeratedFormula.Equals(other.EnumeratedFormula);
        }

        public IEnumerator<Formula> GetEnumerator()
        {
            return new FormulaEnumerator(EnumeratedFormula);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new FormulaEnumerator(EnumeratedFormula);
        }
    }

    public partial class Constant
    {
        override public Formula FormulaAt(int charPosition)
        {
            if (0 < charPosition)
                throw new TermSatException("invalid position:" + charPosition);
            return this;
        }
    }

    public partial class Variable
    {
        override public Formula FormulaAt(int charPosition)
        {
            if (charPosition != 0)
                throw new TermSatException("Invalid symbol position:" + charPosition + "in formula " + ToString());
            return this;
        }
    }

    public partial class Negation
    {
        override public Formula FormulaAt(int charPosition)
        {
            if (charPosition <= 0)
                return this;
            return Child.FormulaAt(charPosition - 1);
        }
    }

    public partial class Implication
    {
        override public Formula FormulaAt(int position)
        {
            if (position <= 0)
                return this;
            int a = Antecedent.Length;
            if (position <= a)
                return Antecedent.FormulaAt(position - 1);
            return Consequent.FormulaAt(position - a - 1);
        }
    }

    public class FormulaEnumerator : IEnumerator<Formula>
    {
        private Formula formula;
        private int startingPosition;
        int position = 0;

        public Formula Current { get; private set; }

        object IEnumerator.Current { get { return Current; } }

        public FormulaEnumerator(Formula f, int startingPosition = 0)
        {
            this.position = this.startingPosition = startingPosition;
        }

        public void Dispose() { /* do nothing */ }

        public bool MoveNext()
        {
            if (formula.Length <= position)
                return false;
            Current = formula.FormulaAt(position++);
            return true;
        }

        public void Reset()
        {
            this.position = this.startingPosition;
            this.Current = null;
        }
    }


}


