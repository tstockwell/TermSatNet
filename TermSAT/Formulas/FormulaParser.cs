using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;

namespace TermSAT.Formulas
{
    public static class FormulaParser
    {
        public static Constant ToConstant(this string formulaText) => GetOrParse(formulaText) as Constant;
        public static Variable ToVariable(this string formulaText) => GetOrParse(formulaText) as Variable;
        public static Negation ToNegation(this string formulaText) => GetOrParse(formulaText) as Negation;
        public static Implication ToImplication(this string formulaText) => GetOrParse(formulaText) as Implication;

        private static readonly MemoryCacheOptions cacheOptions = new MemoryCacheOptions();
        private static readonly MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5));
        private static readonly MemoryCache __cache = new (cacheOptions);

        public static T GetOrCreate<T>(this string formulaText, Func<T> provider)
            where T:Formula
        {
            T formula;
            if (__cache.TryGetValue(formulaText, out formula))
                return formula;
            formula = provider();
            lock (__cache)
            {
                if (__cache.TryGetValue(formulaText, out T _f))
                    return _f;
                __cache.Set(formulaText, formula, cacheEntryOptions);
            }
            return formula;
        }

        /**
         * Parses out the first formula from the beginning of the given string
         */
        public static Formula GetOrParse(this string formulaText)
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
                        case '-': break;
                        case '*': count--; break;
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
                    Formula antecedent = stack.Pop();
                    Formula consequent = stack.Pop();
                    stack.Push(Implication.NewImplication(antecedent, consequent));
                }
                else if (Symbol.IsNand(c))
                {
                    Formula antecedent = stack.Pop();
                    Formula consequent = stack.Pop();
                    stack.Push(Nand.NewNand(antecedent, consequent));
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

            lock (__cache)
            {
                // always gotta check twice
                if (__cache.TryGetValue(formulaText, out formula))
                {
                    return formula;
                }

                formula = stack.Pop();
                __cache.Set(formulaText, formula, cacheEntryOptions);
            }

            return formula;
        }

    }
}
