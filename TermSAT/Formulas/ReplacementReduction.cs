using System.Collections.Generic;

namespace TermSAT.Formulas
{
    // Expresses a reduction to a formula as a change to the formula's dfs odering
    public class ReplacementReduction
    {
        public ReplacementReduction(Formula formula)
        {
            Formula = formula;
        }

        public Formula Formula { get; internal set; }

        /// <summary>
        /// An enumeration of the indexes of subformulas within the formula to be reduced 
        /// and their replacements
        /// </summary>
        public IDictionary<int, Formula> Replacements { get; internal set; }

        public Formula ReducedFormula { get => Formula.WithReplacements(Replacements); }
    }



}


