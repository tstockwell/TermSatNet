using System.Collections.Generic;

namespace TermSAT.Formulas
{
    public partial class Constant : Formula
    {

        static internal List<Variable> EMPTY_VARIABLE_LIST= new List<Variable>();

        // constants
        public static readonly Formula TRUE = new Constant("T");
        public static readonly Formula FALSE= new Constant("F");

	    readonly string _text;
	    readonly IList<Formula> _subformulas;

        protected Constant(string text) : base(1)
        {
            _text = text;
            var subterms = new List<Formula>();
            subterms.Add(this);
            _subformulas = subterms.AsReadOnly();
        }

        override public string ToString()
        {
            return _text;
        }

        override public bool containsVariable(Variable variable)
        {
            return false;
        }

        public override bool evaluate(IDictionary<Variable, bool> valuation)
        {
            return this == TRUE ? true : false;
        }

        override public ICollection<Formula> AllSubterms
        {
            get { return _subformulas; }
        }

        public override IList<Variable> AllVariables { get { return EMPTY_VARIABLE_LIST; } }
    }


}


