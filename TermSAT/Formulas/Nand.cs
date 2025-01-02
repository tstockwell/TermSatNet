using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TermSAT.Common;

namespace TermSAT.Formulas
{
    public partial class Nand : Formula
    {
        public static Nand NewNand(Formula antecedent, Formula consequent)
        {
            var text = $"{Nand.symbol}{antecedent.Text}{consequent.Text}";
            return Formula.GetOrCreate(text, () => new Nand(antecedent, consequent));
        }

        public static implicit operator Nand(string formulaText) =>
            FormulaParser.GetOrParse(formulaText) as Nand;

        public Formula Antecedent { get; }
        public Formula Subsequent { get; }

        private Nand(Formula antecedent, Formula consequent)
            : base(antecedent.Length + consequent.Length + 1)
        {
            Antecedent = antecedent;
            Subsequent = consequent;
        }

        override public bool Evaluate(IDictionary<Variable, bool> values) => 
            (Antecedent.Evaluate(values) && Subsequent.Evaluate(values)) ? false : true;

        public override void GetAllSubterms(ICollection<Formula> subterms)
        {
            Antecedent.GetAllSubterms(subterms);
            Subsequent.GetAllSubterms(subterms);
            subterms.Add(this);
        }

        override public bool ContainsVariable(Variable variable) => 
            Antecedent.ContainsVariable(variable) || Subsequent.ContainsVariable(variable);

        public override string ToString() =>
            "|" + Antecedent.ToString() + Subsequent.ToString();
    }

}

