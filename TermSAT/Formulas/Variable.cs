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
    public partial class Variable : Formula, IEquatable<Variable>, IComparable<Variable>
    {
        /*
         * Variables are currently never discarded.
         * This might be a problem if millions of variables are created.
         * A better caching strategy would be nice.
         */
        static private readonly Dictionary<int, Variable> __cache = new Dictionary<int, Variable>();

        public static readonly Variable ONE = NewVariable(1);
        public static readonly Variable TWO = NewVariable(2);
        public static readonly Variable THREE = NewVariable(3);

        public static implicit operator Variable(string formulaText) => FormulaParser.GetOrParse(formulaText) as Variable;

        string _text;

        public int Number { get; }

        public static Variable NewVariable(int i)
        {
            if (i < 1)
            {
                throw new TermSatException("Variable numbers must be greater than 0");
            }

            lock(__cache)
            {
                if (!__cache.TryGetValue(i, out Variable v))
                {
                    __cache.Add(i, v= new Variable(i));
                }
                return v;
            }
        }

        private Variable(int number) : base(1)
        {
            _text = "." + number;
            Number = number;
        }

        //~Variable() => __cache.Remove(Number); // remove from cache

        override public bool Evaluate(IDictionary<Variable, Boolean> valuation) => valuation[this];

        override public string ToString() => _text;

        override public bool ContainsVariable(Variable variable) => Equals(variable);

        public override void GetAllSubterms(ICollection<Formula> subterms) => subterms.Add(this);

        public int CompareTo(Variable other)
        {
            return this.Number.CompareTo(other.Number);
        }

        public bool Equals(Variable other)
        {
            return this.Number.Equals(other.Number);
        }
    }
}
