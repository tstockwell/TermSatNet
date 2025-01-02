using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TermSAT.Formulas
{

    /// <summary>
    /// 
    /// In order to add a Formula to a TrieIndex we need a to provide the index with a 'string' that's used 
    /// to index the formula.  However, the 'string' is not a string, it's an Enumerable of objects that 
    /// are more suitable for indexing purposes.
    ///
    /// </summary>

    public static class FormulaKeyExtensions
    {
        public static FormulaIndexingKey GetIndexingKey(this Formula formula) => FormulaIndexingKey.GetIndexingKey(formula);
    }

    public class FormulaIndexingKey : IEnumerable<string>
    {
        private static readonly MemoryCacheOptions cacheOptions = new MemoryCacheOptions();
        private static readonly MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5));
        private static readonly MemoryCache indexingKeys = new(cacheOptions);

        public static FormulaIndexingKey GetIndexingKey(Formula f)
        {
            if (!indexingKeys.TryGetValue(f, out FormulaIndexingKey indexingKey))
            {
                lock (indexingKeys)
                {
                    if (indexingKeys.TryGetValue(f, out indexingKey))
                        return indexingKey;
                    indexingKey = new FormulaIndexingKey(f);
                    indexingKeys.Set(f, indexingKey, cacheEntryOptions);
                }
            }

            return indexingKey;
        }

        public Formula Formula { get; private set; }
        private string[] symbols;

        private FormulaIndexingKey(Formula formula)
        {
            Formula = formula;
            symbols= new string[formula.Length];

            var e= new FormulaDFSEnumerator(formula);
            int i= 0;
            while (e.MoveNext())
                symbols[i++]= e.Current.GetIndexingSymbol();
        }

        public int Length => Formula.Length;

        public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)symbols).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => symbols.GetEnumerator();

        public string this[int index] { get => symbols[index]; }
    }

    public partial class Formula
    {
        abstract public string GetIndexingSymbol();
    }

    public partial class Constant
    {
        override public string GetIndexingSymbol() => ToString();
    }

    public partial class Variable
    {
        override public string GetIndexingSymbol() => ToString();
    }

    public partial class Negation : Formula
    {
        static string symbol= Symbol.Negation.ToString();
        override public string GetIndexingSymbol() => symbol;
    }

    public partial class Implication
    {
        static string symbol= Symbol.Implication.ToString();
        override public string GetIndexingSymbol() => symbol;
    }

    public partial class Nand
    {
        static string symbol = Symbol.Nand.ToString();
        override public string GetIndexingSymbol() => symbol;
    }


}


