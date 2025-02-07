using System;
using System.Collections;
using System.Collections.Generic;

namespace TermSAT.Formulas
{
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
            if (Current is Nand)
            {
                var nand = Current as Nand;
                stack.Push(nand.Subsequent);
                Current = nand.Antecedent;
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


