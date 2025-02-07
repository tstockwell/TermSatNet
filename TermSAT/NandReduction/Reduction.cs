using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction
{
    /// <summary>
    /// A reduction is an atomic transform from one formula to a simpler, logically equivalent formula.  
    /// A proof is a list of reductions that transform a starting formula to an equivalent, less complex, formula.
    /// A reduction is a step in a proof.
    /// A reduction maps all the elements in the simpler formulas' flatterm to its position in the starting formula.  
    /// In this way it's possible to understand exactly how a starting formula was modified to get the resulting formula.
    /// 
    /// For instance, if a new formula was created by swapping the arguments to a nand operator (|.2.1 => |.1.2) then...
    /// ```
    ///     reduction.Mapping = Enumerable.Range(0,1)
    ///         .Concat(Enumerable.Range(formula.Antecedent.Length + 1, formula.Subsequent.Length))
    ///         .Concat(Enumerable.Range(1, formula.Antecedent.Length));
    /// ```
    /// ...is a mapping that maps all the terms in |.2.1, for 0 to (|.2.1).Length-1, to terms in |.1.2.
    ///     
    /// A reduction is an asymmetric operation because there may be new subterms in the reduced formula that 
    /// do not exist in the starting formula.
    /// 
    /// NandSAT provides a global dictionary of reductions for every formula that reduce every formula to a 
    /// less complex formula based on a rule.
    /// NandSAT basically works by building a set of reductions that reduce a given formula to it canonical form until such that, for every term in a formula, 
    /// to the terms canonical form.
    /// These proofs are called 'reduction proofs'.
    /// Reduction proofs are built from the bottom up.
    /// Initially a formula will have a reduction proof that has no reductions.
    /// If a formula can no longer be reduced then it 
    /// then it has been proved to be canonical and 
    /// the last reduction in a proof will have RuleDescription == CANONICAL.
    /// When a reduction proofs ReducedFormula is canonical then the proof is complete and no longer updated.
    /// 
    /// 
    /// </summary>
    public class Reduction 
    {
        public static string PROOF_IS_COMPLETE = "PROOF_IS_COMPLETE";
        //public static string PROOF_IS_INCOMPLETE = "PROOF_IS_INCOMPLETE";
        public static Reduction CreateCompletionMarker(Formula startingFormula, IImmutableList<Reduction> incompleteProofs= null) =>
            new Reduction(
                startingFormula, 
                startingFormula, 
                Reduction.PROOF_IS_COMPLETE, 
                Enumerable.Range(0, startingFormula.Length).ToImmutableList(), 
                incompleteProofs);

        public Formula StartingFormula {  get; }
        public Formula ReducedFormula { get; }

        /// <summary>
        /// Set to Reduction.PROOF_IS_COMPLETE when StartingNand is canonical.
        /// todo: maybe instead of using a string to identify reduction types use objects instead.  
        /// </summary>
        public string RuleDescriptor { get; }

        /// <summary>
        /// Maps terms in ReducedFormula to terms in StartingNand. 
        /// Used during wildcard analysis to associate terms in ReducedFormula with terms in StartingNand .
        /// </summary>
        public IImmutableList<int> Mapping { get; }


        /// <summary>
        /// When, in the course of constructing a reduction, a dependent/child reduction would result in an infinite loop, 
        ///     and is therefore skipped, 
        /// Then
        ///     the skipped reduction is included in this list
        ///     
        /// A reduction is a reduction, 
        ///     so if ReducedFormula < StartingFormula 
        ///     then a reduction can be considered valid in any context and could be globally cached, 
        ///         even if it's imcomplete.
        /// However, if for some reduction, ReducedFormula == StartingFormula and the reduction is incomplete 
        /// then it can only be considered valid in the context in which it was created and cannot be globally cached.
        /// 
        /// Currently only used for debugging purposes
        /// </summary>
        public IImmutableList<Reduction> IncompleteProofs { get; }

      

#if DEBUG
        /// <summary>
        /// The ReducedFormula mapped to StartingFormula, as a string, where elements that map to -1 displayed using '#'
        /// This field is currently just used for debugging, but its super handy.
        /// </summary>
        public string MappedFormula { 
            get
            {
                var terms = StartingFormula.AsFlatTerm().ToArray();
                string[] mapped = new string[ReducedFormula.Length];
                for (int i = 0; i < mapped.Length; i++)
                {
                    mapped[i] = "#";
                    var m = Mapping[i];
                    if (-1 < m)
                    {
                        mapped[i] = terms[m].GetIndexingSymbol();
                    }
                }
                return string.Join("",mapped);
            }
        }
#endif

        public Reduction(Formula startingFormula, Formula reducedFormula, string ruleDescriptor, IImmutableList<int> mapping, IImmutableList<Reduction> incompleteProofs= null, Proof childProof = null)
        {
            StartingFormula=startingFormula;
            ReducedFormula=reducedFormula;
            RuleDescriptor=ruleDescriptor;
            Mapping=mapping;
            IncompleteProofs=incompleteProofs;

            if (IncompleteProofs == null)
            {
                IncompleteProofs = ImmutableList<Reduction>.Empty;
            }

            Validate();
        }

        public override string ToString()
        {
            return $"{RuleDescriptor}: {StartingFormula} => {ReducedFormula}";
        }

        void Validate() 
        {
#if DEBUG
            if (!RuleDescriptor.Equals(PROOF_IS_COMPLETE))
            {
                var reducedTT = ReducedFormula.GetTruthTable().ToString();
                var startingTT = StartingFormula.GetTruthTable().ToString();
                if (!reducedTT.Equals(startingTT))
                {
                    throw new TermSatException($"{ReducedFormula} is not a valid reduction for {StartingFormula}");
                }

                if (Mapping.Count() < ReducedFormula.Length) 
                {
                    throw new TermSatException("A reduction mapping must provide a mapping for every symbol in the reduced formula");
                }

                for (int i = 0; i < ReducedFormula.Length; i++)
                {
                    var reducedTerm = ReducedFormula.GetFormulaAtPosition(i);
                    var startingPosition = Mapping[i];
                    if (0 <= startingPosition)
                    {
                        var startingTerm = StartingFormula.GetFormulaAtPosition(startingPosition);
                        //Debug.Assert(reducedTerm.Equals(startingTerm), $"Mapped terms do not match");
                        Debug.Assert(reducedTerm.GetIndexingSymbol().Equals(startingTerm.GetIndexingSymbol()), $"Mapped terms do not match");
                    }
                }
            }

            if ("|T|T.1 => .1" == RuleDescriptor)
            {
                if (!(ReducedFormula.Length < StartingFormula.Length))
                {
                    throw new TermSatException($"This reduction should have reduced the length of the starting formula: {this}");
                }
            }
#endif
        }

        //public ProofTracer(Formulas.Nand currentFormula, int targetPosition, Formula replacement, string ruleDescriptor);
    }
}


public static class ReductionExtensions
{
 
}