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
        private Formula[] formulaEnumeration;

        partial void Initialize()
        {
            formulaEnumeration= new Formula[Length];

            int i= 0;
            var e= new FormulaEnumerator(this);
            while (e.MoveNext())
                formulaEnumeration[i++]= e.Current;
        }

        public Formula this[int index]  { get => formulaEnumeration[index];  }

        public int CompareTo(ISequence<Formula> other) => throw new NotImplementedException();

        public bool Equals(ISequence<Formula> other) => this.Equals(other as Formula);

        public IEnumerator<Formula> GetEnumerator() => new FormulaEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => new FormulaEnumerator(this);
    }

    public class FormulaEnumerator : IEnumerator<Formula>
    {
        private readonly Formula formula;
        private readonly Stack<Formula> stack= new Stack<Formula>();

        public Formula Current { get; private set; }

        object IEnumerator.Current { get { return Current; } }

        public FormulaEnumerator(Formula formula)
        {
            this.formula= formula;
        }

        public void Dispose() { /* do nothing */ }

        public bool MoveNext()
        {
            if (Current == null) 
            { 
                Current= formula;
                return true;
            }
            if (Current is Constant || Current is Variable)
            {
                if (stack.Count <= 0)
                    return false;
                Current= stack.Pop();
                return true;
            }
            if (Current is Negation)
            {
                Current= (Current as Negation).Child;
                return true;
            }

            stack.Push((Current as Implication).Consequent);
            Current= (Current as Implication).Antecedent;
            return true;
        }

        public void Reset()
        {
            this.Current = null;
        }
    }


}


