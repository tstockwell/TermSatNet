using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TermSAT.Common;

namespace TermSAT.Formulas
{
    public partial class Implication : Formula
    {

        public static Implication NewImplication(Formula antecedent, Formula consequent)
        {
            var text = $"{Implication.symbol}{antecedent.Text}{consequent.Text}";
            return Formula.GetOrCreate(text, () => new Implication(antecedent, consequent));
        }

        public static implicit operator Implication(string formulaText) =>
            FormulaParser.GetOrParse(formulaText) as Implication;

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

