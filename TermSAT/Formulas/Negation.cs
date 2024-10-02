using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TermSAT.Common;

namespace TermSAT.Formulas
{
    public partial class Negation : Formula
    {
        static private ConditionalWeakTable<Formula, Negation> __negations= new ConditionalWeakTable<Formula, Negation>();

        public Formula Child { get; }

        public static Negation NewNegation(Formula child) { 
            lock( __negations )
            {
                var n = __negations.GetValue(child, c => new Negation(child));
                return n;
            }
        }

        public static implicit operator Negation(string formulaText) => formulaText.ToNegation();


        /// <summary>
        /// Creates a new formula by negating the given formula
        /// </summary>
        private Negation(Formula subFormula) : base(length: subFormula.Length + 1)
        {
            Child = subFormula;
        }
        //~Negation() => __cache.Remove(Child);

        public override bool Evaluate(IDictionary<Variable, bool> valuation) => Child.Evaluate(valuation) ? false : true;

        public override bool ContainsVariable(Variable variable) => Child.ContainsVariable(variable);

        public override void GetAllSubterms(ICollection<Formula> subterms)
        {
            Child.GetAllSubterms(subterms);
            subterms.Add(this);
        }

        public override string ToString() => "-" + Child.ToString();
    }


}
