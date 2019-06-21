using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TermSAT.Common;

namespace TermSAT.Formulas
{
    public partial class Implication : Formula
    {


        static readonly WeakCache<Formula, WeakCache<Formula, Implication>> formulaCache = new WeakCache<Formula, WeakCache<Formula, Implication>>();

        public static Implication NewImplication(Formula antecedent, Formula consequent)
        {
            var implicationCache = formulaCache.GetOrCreateValue(antecedent, () => new WeakCache<Formula, Implication>());
            return implicationCache.GetOrCreateValue(consequent, () => new Implication(antecedent, consequent));
        }

        public static implicit operator Implication(string formulaText)
        {
            return FormulaParser.ToFormula(formulaText) as Implication;
        }

        public Formula Antecedent { get; }
        public Formula Consequent { get; }

        private Implication(Formula antecedent, Formula consequent)
            : base(antecedent.Length + consequent.Length + 1)
        {
            Antecedent = antecedent;
            Consequent = consequent;
        }

        override public bool Evaluate(IDictionary<Variable, bool> values)
        {
            return (Antecedent.Evaluate(values) && !Consequent.Evaluate(values)) ? false : true;
        }

        public override void GetAllSubterms(ICollection<Formula> subterms)
        {
            Antecedent.GetAllSubterms(subterms);
            Consequent.GetAllSubterms(subterms);
            subterms.Add(this);
        }

        public override IList<Variable> AllVariables
        {
            get
            {
                var vars = new HashSet<Variable>();
                vars.UnionWith(Antecedent.AllVariables);
                vars.UnionWith(Consequent.AllVariables);
                var l= new List<Variable>();
                l.AddRange(vars);
                l.Sort();
                return l;
            }
        }

        override public bool ContainsVariable(Variable variable)
        {
            return Antecedent.ContainsVariable(variable) || Consequent.ContainsVariable(variable);
        }

        public override string ToString()
        {
            return "*" + Antecedent.ToString() + Consequent.ToString();
        }
    }

}

