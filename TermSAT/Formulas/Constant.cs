using System.Collections.Generic;
using System.Collections.Immutable;

namespace TermSAT.Formulas
{
    public partial class Constant : Formula
    {
        public static implicit operator Constant(string formulaText) => FormulaParser.ToFormula(formulaText) as Constant;
        public static implicit operator Constant(bool value) => value ? TRUE : FALSE;

        // constants
        public static readonly Constant TRUE = new Constant("T");
        public static readonly Constant FALSE= new Constant("F");

	    readonly string _text;

        protected Constant(string text) : base(length:1) => _text = text;

        override public string ToString() => _text;

        override public bool ContainsVariable(Variable variable) => false;

        public override bool Evaluate(IDictionary<Variable, bool> valuation) => this == TRUE ? true : false;

        public override void GetAllSubterms(ICollection<Formula> subterms) => subterms.Add(this);

        public override IList<Variable> AllVariables { get => ImmutableList<Variable>.Empty; }
    }
}


