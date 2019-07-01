using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TermSAT.Common;

namespace TermSAT.Formulas
{
    public partial class Implication : Formula
    {
        //static readonly WeakCache<Formula, WeakCache<Formula, Implication>> formulaCache = new WeakCache<Formula, WeakCache<Formula, Implication>>();
        static private ConditionalWeakTable<Formula, ConditionalWeakTable<Formula, Implication>> __implications= 
                new ConditionalWeakTable<Formula, ConditionalWeakTable<Formula, Implication>>();

        public static Implication NewImplication(Formula antecedent, Formula consequent)
        {
            var implications = __implications.GetValue(antecedent, (a) => new ConditionalWeakTable<Formula, Implication>());
            var i= implications.GetValue(consequent, (a) => new Implication(antecedent, consequent));
            return i;
        }

        public static implicit operator Implication(string formulaText) =>
            FormulaParser.ToFormula(formulaText) as Implication;

        public Formula Antecedent { get; }
        public Formula Consequent { get; }

        private Implication(Formula antecedent, Formula consequent)
            : base(antecedent.Length + consequent.Length + 1)
        {
            Antecedent = antecedent;
            Consequent = consequent;
        }

        ~Implication()
        {
            var i= GetHashCode();
            var j= WeakCacheFlag.Value;
        }

        override public bool Evaluate(IDictionary<Variable, bool> values) => 
            (Antecedent.Evaluate(values) && !Consequent.Evaluate(values)) ? false : true;

        public override void GetAllSubterms(ICollection<Formula> subterms)
        {
            Antecedent.GetAllSubterms(subterms);
            Consequent.GetAllSubterms(subterms);
            subterms.Add(this);
        }

        override public bool ContainsVariable(Variable variable) => 
            Antecedent.ContainsVariable(variable) || Consequent.ContainsVariable(variable);

        public override string ToString() =>
            "*" + Antecedent.ToString() + Consequent.ToString();
    }

}

