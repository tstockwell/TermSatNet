using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TermSAT.Common;

namespace TermSAT.Formulas
{

    public interface IFormulaSequence : ISequence<Formula> { }

    // Expresses a reduction to a formula as a change to the formula's dfs odering
    public class ReplacementReduction
    {
        public ReplacementReduction(Formula formula)
        {
            Formula = formula;
        }

        public Formula Formula { get; internal set; }

        /// <summary>
        /// An enumeration of the indexes of subformulas within the formula to be reduced 
        /// and thier replacements
        /// </summary>
        public IDictionary<int, Formula> Replacements { get; internal set; }

        public Formula ReducedFormula { get => Formula.GetDFSOrdering().ToFormula(Replacements); }
    }


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
        public static FlatTerm GetDFSOrdering(this Formula formula) => FlatTerm.GetFlatTerm(formula);

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


    /// <summary>
    /// 
    /// This source module extends the Formula class with an API that represents Formulas as an array of all the 
    /// subformulas in the formula, ordered from the leftmost subformula (the formula itself) to the rightmost 
    /// subformula (the last constant or variable that appears in the formula).
    /// TermSAT calls such an array a 'flatterm'.
    /// The name FlatTerm comes from the chapter on Term Indexing in the book 
    /// 'Handbook of Automated Reasoning',  by R Sekar, V Ramakrishnan, and Andrei Voronkov.
    /// 
    /// The particular ordering implemented by flatterms is known as a 
    /// <a href='https://en.wikipedia.org/wiki/Depth-first_search#DFS_ordering'>DFS ordering</a>, or a 
    /// <a href='https://en.wikipedia.org/wiki/Prefix_order'>prefix ordering</a>.
    /// 
    /// FlatTerms are useful for constructing indexes of sets of formulas.
    /// The indexes built from FlatTerms make it possible to quickly find substitution instances and unifications 
    /// of a given formula in a large set of formulas.
    /// I expect that in the future it will be necessary to express formulas in different ordering, for indexing purposes. 
    /// 
    /// Also, FlatTerms can be easier to use that a vistor API when implementing formula reduction algorithms and such.
    /// 
    /// Example:
    /// Given the formula...
    ///     *a*-bc
    /// ...the associated DFSOrdering is a list of a the subformuals from right to left...
    ///     { *a*-bc, a, *-bc, -b, b, c }
    ///  The above list of formulas is exactly the same enumeration of formulas that would be obtained 
    ///  by navigating the original formula in a depth first enumeration (enumerating antecents before 
    ///  consequents).
    ///  
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
    public class FlatTerm : IFormulaSequence
    {
        private static ConditionalWeakTable<Formula, FlatTerm> sequences =
            new ConditionalWeakTable<Formula, FlatTerm>();

        public static FlatTerm GetFlatTerm(Formula f)
        {
            if (!sequences.TryGetValue(f, out FlatTerm sequence))
            {
                lock (sequences)
                {
                    if (sequences.TryGetValue(f, out sequence))
                        return sequence;
                    sequence = new FlatTerm(f);
                    sequences.Add(f, sequence);
                }
            }

            return sequence;
        }

        public Formula Formula { get; private set; }

        private FlatTerm(Formula formula)
        {
            Formula = formula;
        }

        public int Length => Formula.Length;

        public IEnumerator<Formula> GetEnumerator() => new FormulaDFSEnumerator(Formula);

        IEnumerator IEnumerable.GetEnumerator() => new FormulaDFSEnumerator(Formula);

        public Formula this[int index] { get => Formula.GetFormulaAtPosition(index); }
    }

    public partial class Formula
    {
        abstract public Formula GetFormulaAtPosition(int index);
    }

    public partial class Constant
    {
        override public Formula GetFormulaAtPosition(int index)
        {
            if (index != 0)
                throw new TermSatException("Invalid symbol position:" + index + "in formula " + ToString());
            return this;
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

            stack.Push((Current as Implication).Consequent);
            Current = (Current as Implication).Antecedent;
            return true;
        }

        public void Reset()
        {
            this.Current = null;
        }
    }



}


