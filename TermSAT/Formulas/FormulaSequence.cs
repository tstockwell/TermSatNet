using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TermSAT.Common;

namespace TermSAT.Formulas
{

    /// <summary>
    /// 
    /// This source module extends the Formula class with an API that represents Formulas as an array of all the 
    /// subformulas in the formula, ordered from the leftmost subformula (the forumula itself) to the rightmost 
    /// subformula (the last constant or variable that appears in the formula).
    /// Such an array is called a 'formula sequence'.
    /// 
    /// A FormulaSequence is a representation of a Formula as a sequence of symbols, as opposed to a tree of objects.
    /// FormulaSequence are like strings, but not strings of characters, FormulaSequence are strings of the subformulas 
    /// of a formula as enumerated from right to left.
    /// 
    /// FormulaSequences are useful for constructing indexes of sets of formulas.
    /// The indexes built from FormulaSequence make it possible to quickly find substitution instances and unifications 
    /// of a given formula in a large set of formulas.
    /// 
    /// Example:
    /// Given the formula...
    ///     *a*-bc
    /// ...the associated FormulaSequence is a list of a the subformuals from right to left...
    ///     { *a*-bc, a, *-bc, -b, b, c }
    ///  The above list of formulas is exactly the same enumeration of formulas that would be obtained 
    ///  by navigating the original formula in a depth first enumeration (enumerating antecents before 
    ///  consequents).
    ///  Note that the original formula from which the sequence is derived can be reconstructed by 
    ///  examining the types of the subformulas and the variables in the sequence.
    ///  The types of the subformulas looks like this...
    ///     { implication, variable(a), implication, negation, variable(b), variable(c) }.
    ///  Note that by replacing subformulas with the symbol that represents thier type, we 
    ///  can reconstuct the original formula from which the sequence is derived...
    ///     { *, a, *, -, b, c }
    ///     
    ///  Thus, formula sequences are a kind of string that represents a formula.
    ///  
    ///  There are many indexing techniques used in SAT solvers that are based on various string 
    ///  indexing and matching techniques, and formula sequences give us a way to represent 
    ///  formulas as strings in a memory efficient way that takes advantage of the existing 
    ///  structure of formulas in TermSAT.
    ///  You might be thinking that implementing the IFormulaSequence API is a lot of work compared 
    ///  to just using string to represent formulas.  But I have now enough experience with SAT solvers 
    ///  to know that efficient use of memory is very important. 
    ///  One advantage of FormulaSequences is that, since they are just an interface added to Formulas 
    ///  and have no data of their own, they use no extra memory.
    ///
    /// </summary>
    public interface IFormulaSequence : ISequence<Formula> { }


    public partial class Formula : IFormulaSequence
    {
        abstract public Formula this[int index] { get; }

        public int CompareTo(ISequence<Formula> other)
        {
            throw new NotImplementedException();
        }

        public bool Equals(ISequence<Formula> other)
        {
            return this.Equals(other as Formula);
        }

        public IEnumerator<Formula> GetEnumerator()
        {
            return new FormulaEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new FormulaEnumerator(this);
        }
    }

    public partial class Constant
    {
        override public Formula this[int index]
        {
            get
            {
                if (index != 0)
                    throw new TermSatException("Invalid symbol position:" + index + "in formula " + ToString());
                return this;
            }
        }
    }

    public partial class Variable
    {
        override public Formula this[int index]
        {
            get
            {
                if (index != 0)
                    throw new TermSatException("Invalid symbol position:" + index + "in formula " + ToString());
                return this;
            }
        }
    }

    public partial class Negation
    {
        override public Formula this[int index]
        {
            get
            {
                if (index < 0)
                    throw new TermSatException("Invalid symbol position:" + index + "in formula " + ToString());
                if (index == 0)
                    return this;
                return Child[index - 1];
            }
        }
    }

    public partial class Implication
    {
        override public Formula this[int index]
        {
            get
            {
                if (index < 0)
                    throw new TermSatException("Invalid symbol position:" + index + "in formula " + ToString());
                if (index == 0)
                    return this;
                int a = Antecedent.Length;
                if (index <= a)
                    return Antecedent[index - 1];
                return Consequent[index - a - 1];
            }
        }
    }

    public class FormulaEnumerator : IEnumerator<Formula>
    {
        private Formula formula;
        private readonly int startingPosition;
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
            Current = formula[position++];
            return true;
        }

        public void Reset()
        {
            this.position = this.startingPosition;
            this.Current = null;
        }
    }


}


