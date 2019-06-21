using System.Collections;
using System.Collections.Generic;
using TermSAT.Common;

namespace TermSAT.Formulas
{

    public partial class Negation : Formula
    {
        static private WeakCache<Formula, Negation> __cache = new WeakCache<Formula, Negation>();

        public Formula Child { get; }

        public static Negation NewNegation(Formula child) =>
            __cache.GetOrCreateValue(child, () => new Negation(child));

        public static implicit operator Negation(string formulaText) =>
            FormulaParser.ToFormula(formulaText) as Negation;


        /// <summary>
        /// Creates a new formula by negating the given formula
        /// </summary>
        private Negation(Formula subFormula) : base(length: subFormula.Length + 1)
        {
            Child = subFormula;
        }
        ~Negation() => __cache.Remove(Child); 

        public override bool Evaluate(IDictionary<Variable, bool> valuation) =>
            Child.Evaluate(valuation) ? false : true;

        public override bool ContainsVariable(Variable variable) => 
            Child.ContainsVariable(variable);

        public override void GetAllSubterms(ICollection<Formula> subterms)
        { 
            Child.GetAllSubterms(subterms);
            subterms.Add(this);
        }

        public override IList<Variable> AllVariables { get => Child.AllVariables; }

        public override string ToString() => "-" + Child.ToString();
    }


}
