using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using TermSAT.Common;

namespace TermSAT.Formulas
{

    /**
     * Variables are represented as integers greater than zero.
     * The only way to create a Variable or get a reference to a Variable is the Variable.newVariable method.
     * Only a single instance of any Variable is ever created.
     * 
     * Variables are represented textually by a '." followed by a number.
     * For instance, ".1".
     *
     */
    public partial class Variable : Formula {

        /// <summary>
        /// I would like to use an int as the key to the cache but keys must be reference types
        /// </summary>
        class CacheKey
        {
            private int key;
            public CacheKey(int k)
            {
                this.key = k;
            }

            public override int GetHashCode()
            {
                return key;
            }
        }


        static private readonly WeakCache<CacheKey, Variable> __cache = new WeakCache<CacheKey, Variable>();

        string _text;
        readonly List<Variable> _varlist;

        public int Number { get; }

        public static Variable newVariable(int variableId)
        {
            if (variableId < 1)
                throw new Exception("Variable numbers must be greater than 0");
            return __cache.GetOrCreateValue(new CacheKey(variableId), () => new Variable(variableId));
        }

        private Variable(int number) : base(1)
        {
            _text = "#" + number;
            Number = number;
            _varlist = new List<Variable>() { this };
        }

        override public bool evaluate(IDictionary<Variable, Boolean> valuation)
        {
            return valuation[this];
        }

        override public string ToString()
        {
            return _text;
        }

        override public bool containsVariable(Variable variable)
        {
            return this.Equals(variable);
        }

        override public ICollection<Formula> AllSubterms {
            get {
                return _varlist as ICollection<Formula>;
            }
        }

        override public IList<Variable> AllVariables
        {
            get
            {
                return _varlist;
            }
        }
    }

}
