using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    ///  formulas as strings in a convenient way.
    ///
    /// </summary>
    public interface IFormulaSequence : ISequence<Formula> { }


    public static class FormulaSequenceExtensions 
    {
        public static FormulaSequence ToSequence(this Formula formula) => FormulaSequence.GetSequence(formula);
    }

    public class FormulaSequence : IFormulaSequence
    {
        private static readonly Formula[] TRUE = new Formula[] { Constant.TRUE };
        private static readonly Formula[] FALSE = new Formula[] { Constant.TRUE };
        private static ConditionalWeakTable<Formula, FormulaSequence> sequences = 
            new ConditionalWeakTable<Formula, FormulaSequence>();

        public static FormulaSequence GetSequence(Formula f)
        {
            if (!sequences.TryGetValue(f, out FormulaSequence sequence))
            {
                lock (sequences)
                {
                    if (sequences.TryGetValue(f, out sequence))
                        return sequence;

                    var s = new Formula[f.Length];
                    int i = 0;
                    var e = new FormulaEnumerator(f);
                    while (e.MoveNext())
                        s[i++] = e.Current;

                    sequence = new FormulaSequence(s);
                    sequences.Add(f, sequence);
                }
            }

            return sequence;
        }

        private Formula[] formulaEnumeration;

        private FormulaSequence(Formula[] formulaEnumeration)
        {
            this.formulaEnumeration = formulaEnumeration;
        }

        public int Length { get => formulaEnumeration[0].Length; }

        public Formula this[int index]  { get => formulaEnumeration[index];  }

        public int CompareTo(ISequence<Formula> other) => this[0].CompareTo(other[0]);

        public bool Equals(ISequence<Formula> other) => this[0].Equals(other[0]);

        public IEnumerator<Formula> GetEnumerator() => (IEnumerator<Formula>)formulaEnumeration.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => formulaEnumeration.GetEnumerator();
    }

    /**
     * This class enumerates all of a Formula's subformulas in the same order as they 
     * are written.  Basically that means that implication antecedents come before consequents.
     * The first formula returned is the enumerated formula itself.
     */
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


