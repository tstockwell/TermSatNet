using System.Collections;
using System.Collections.Generic;
using TermSAT.Common;

namespace TermSAT.Formulas
{

    public partial class Negation : Formula
    {
        static private WeakCache<Formula, Negation> __cache = new WeakCache<Formula, Negation>();

        public Formula Child { get; }

        public static Negation newNegation(Formula f)
        {
            return __cache.GetOrCreateValue(f, () => new Negation(f));
        }


        /// <summary>
        /// Creates a new formula by negating the given formula
        /// </summary>
        private Negation(Formula subFormula) : base(subFormula.Length + 1)
        {
            Child = subFormula;
        }

        override public bool Evaluate(IDictionary<Variable, bool> valuation)
        {
            return Child.Evaluate(valuation) ? false : true;
        }

        override public bool ContainsVariable(Variable variable)
        {
            return Child.ContainsVariable(variable);
        }

        public override void GetAllSubterms(ICollection<Formula> subterms)
        { 
            Child.GetAllSubterms(subterms);
            subterms.Add(this);
        }
        override public IList<Variable> AllVariables
        {
            get
            {
                return Child.AllVariables;
            }
        }

        public override string ToString()
        {
            return "-" + Child.ToString();
        }
    }


}
