using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TermSAT.Formulas;

namespace TermSAT.NandReduction
{
    public static class NandFormulaParser
    {

        private static readonly ConditionalWeakTable<string, Formula> __cache = new ConditionalWeakTable<string, Formula>();


        /**
         * Parses out the first formula from the beginning of the given string
         */
        public static Formula ToNandFormula(this string formulaText)
        {
            Formula formula;
            lock (__cache)
            {
                if (__cache.TryGetValue(formulaText, out formula))
                    return formula;
            }

            // find the end of the formula
            int last = -1;
            {
                int count = 0;
                int max = formulaText.Length - 1;
                for (int i = 0; i <= max; i++)
                {
                    char c = formulaText[i];
                    switch (c)
                    {
                        case '|': count--; break;
                        case 'T': count++; break;
                        case 'F': count++; break;
                        case '.': { count++; } break;
                    }
                    if (0 < count)
                    {
                        last = i + 1;
                        break;
                    }
                }
            }
            if (last < 0)
                throw new Exception("Not a valid nand formula:" + formulaText);


            Stack<Formula> stack = new ();
            for (int i = last; 0 < i--;)
            {
                char c = formulaText[i];
                if (Symbol.IsNand(c))
                {
                    Formula antecendent = stack.Pop();
                    Formula consequent = stack.Pop();
                    stack.Push(Formulas.Nand.NewNand(antecendent, consequent));
                }
                else if (Symbol.IsTrue(c))
                {
                    stack.Push(Constant.TRUE);
                }
                else if (Symbol.IsFalse(c))
                {
                    stack.Push(Constant.FALSE);
                }
                else if (Symbol.IsVariable(c))
                {
                    int start = i + 1, end= i + 2;
                    while (end < formulaText.Length && Char.IsDigit(formulaText[end]))
                        end++;
                    var n = int.Parse(formulaText.Substring(start, end - start));
                    stack.Push(Variable.NewVariable(n));
                }
                else if (Char.IsDigit(c))
                {
                    // skip
                }
                else
                    throw new Exception("Unknown symbol:" + c);
            }

            if (stack.Count != 1)
                throw new Exception("Invalid postcondition after evaluating formula wellformedness: count < 1");

            formula = stack.Pop();
            lock (__cache)
            {
                __cache.AddOrUpdate(formulaText, formula);
            }
            return formula;
        }

    }
}
