using System;
using System.Collections.Generic;
using System.Text;

namespace TermSAT.Formulas
{
    public static class FormulaParser
    {

        /**
         * Parses out the first formula from the beginning of the given string
         */
        public static Formula ToFormula(this string formulaText)
        {
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
                        case '-': break;
                        case '*': count--; break;
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
                throw new Exception("Not a valid formula:" + formulaText);


            Stack<Formula> stack = new Stack<Formula>();
            for (int i = last; 0 < i--;)
            {
                char c = formulaText[i];
                if (Symbol.IsNegation(c))
                {
                    Formula f = stack.Pop();
                    stack.Push(Negation.NewNegation(f));
                }
                else if (Symbol.IsImplication(c))
                {
                    Formula antecendent = stack.Pop();
                    Formula consequent = stack.Pop();
                    stack.Push(Implication.NewImplication(antecendent, consequent));
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

            Formula formula2 = stack.Pop();
            return formula2;
        }

    }
}
