using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TermSAT.Common;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.Nand
{
    /// <summary>
    /// A reduction is an atomic transform that 'reduces' a formula to a simpler, logically equivalent formula.  
    /// A reduction maps all the elements in the simpler formulas' flatterm to its position in the starting formula.  
    /// In this way it's possible to understand exactly how a starting formula was modified to get the resulting formula.
    /// 
    /// For instance, if a new formula was created by swapping the arguments to a nand operator (|.2.1 => |.1.2) then...
    ///     reduction.Mapping[1] = StartingFormula.Antecedent.Length + 1
    /// and...
    ///     reduction.Mapping[ReducedFormula.Antecedent.Length + 1] = 1
    ///     
    /// A reduction is an asymmetric operation because there may be new subterms in the reduced formula that 
    /// do not exist in the starting formula.
    /// 
    /// A proof is a list of reductions that transform a starting formula to an equivalent, less complex, formula.
    /// A reduction is a step in a proof.
    /// </summary>
    public class Reduction 
    {
        public static string RULE_NO_CHANGE = "NO CHANGE";

        public Formula StartingFormula {  get; }
        public Formula ReducedFormula { get; }
        public string RuleDescriptor { get; }

        public IEnumerable<int> Mapping { get; }
        //public int TermPosition { get; init; }
        //public Formula Replacement { get; init; }
        //public Formula ReducedTerm { get; init; }

        public static Reduction NoChange(Formula startingFormula) =>
            new (startingFormula, startingFormula, Reduction.RULE_NO_CHANGE, null /*Enumerable.Range(0, startingFormula.Length).ToList()*/);

        public Reduction(Formula startingFormula, Formula reducedFormula, string ruleDescriptor, IEnumerable<int> mapping)
        {
            StartingFormula=startingFormula;
            ReducedFormula=reducedFormula;
            RuleDescriptor=ruleDescriptor;
            Mapping=mapping;

            Validate();
        }

        public override string ToString()
        {
            return $"{RuleDescriptor}: {StartingFormula} => {ReducedFormula}";
        }

        void Validate() 
        {
#if DEBUG
            if (!RuleDescriptor.Equals(RULE_NO_CHANGE))
            {
                var reducedTT = ReducedFormula.GetTruthTable().ToString();
                var startingTT = StartingFormula.GetTruthTable().ToString();
                if (!reducedTT.Equals(startingTT))
                {
                    throw new TermSatException($"{ReducedFormula} is not a valid reduction for {StartingFormula}");
                }

                Debug.Assert(Mapping.Count() == ReducedFormula.Length, "A reduction mapping must provide a mapping for every symbol in the reduced formula");

                //for (int i = 0; i < ReducedFormula.Length; i++)
                //{
                //    var reducedTerm = ReducedFormula.GetFormulaAtPosition(i);
                //    var startingPosition = Mapping.Skip(i).First();
                //    if (0 <= startingPosition)
                //    {
                //        var startingTerm = StartingFormula.GetFormulaAtPosition(startingPosition);
                //        Debug.Assert(reducedTerm.Equals(startingTerm), $"Mapped terms do not match");
                //    }
                //}

                //Debug.Assert(ReducedFormula.Equals(StartingFormula.ReplaceAt(TermPosition, Replacement)));
            }
#endif
        }

        //public ProofTracer(Formulas.Nand currentFormula, int targetPosition, Formula replacement, string ruleDescriptor);
    }
}
