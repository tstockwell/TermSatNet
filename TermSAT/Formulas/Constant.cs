using System.Collections.Generic;

namespace TermSAT.Formulas
{
    public partial class Constant : Formula
    {

        static internal List<Variable> EMPTY_VARIABLE_LIST= new List<Variable>();

        public static implicit operator Constant(string formulaText)
        {
            return FormulaParser.ToFormula(formulaText) as Constant;
        }

        // constants
        public static readonly Formula TRUE = new Constant("T");
        public static readonly Formula FALSE= new Constant("F");

	    readonly string _text;

        protected Constant(string text) : base(1)
        {
            _text = text;
        }

        override public string ToString()
        {
            return _text;
        }

        override public bool ContainsVariable(Variable variable)
        {
            return false;
        }

        public override bool Evaluate(IDictionary<Variable, bool> valuation)
        {
            return this == TRUE ? true : false;
        }

        public override void GetAllSubterms(ICollection<Formula> subterms)
        {
            subterms.Add(this);
        }

        public override IList<Variable> AllVariables { get { return EMPTY_VARIABLE_LIST; } }
    }


}


