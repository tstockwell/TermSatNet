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

        public static readonly Variable ONE = NewVariable(1);
        public static readonly Variable TWO = NewVariable(2);
        public static readonly Variable THREE = NewVariable(3);

        public static implicit operator Variable(string formulaText) => FormulaParser.ToFormula(formulaText) as Variable;

        static private readonly WeakCache<int, Variable> __cache = new WeakCache<int, Variable>();

        string _text;
        readonly List<Variable> _varlist;

        public int Number { get; }

        public static Variable NewVariable(int variableId)
        {
            if (variableId < 1)
                throw new Exception("Variable numbers must be greater than 0");
            return __cache.GetOrCreateValue(variableId, () => new Variable(variableId));
        }

        private Variable(int number) : base(1)
        {
            _text = "." + number;
            Number = number;
            _varlist = new List<Variable>() { this };
        }

        ~Variable() => __cache.Remove(Number); // remove from cache

        override public bool Evaluate(IDictionary<Variable, Boolean> valuation) => valuation[this];

        override public string ToString() => _text;

        override public bool ContainsVariable(Variable variable) => Equals(variable);

        public override void GetAllSubterms(ICollection<Formula> subterms) => subterms.Add(this);

        override public IList<Variable> AllVariables { get => _varlist; }
    }

}
