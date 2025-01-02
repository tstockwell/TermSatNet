using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Linq;
using TermSAT.Formulas;

namespace TermSAT.NandReduction;

/// <summary>
/// Implements the portion of the nand reduction algorithm that discovers **wildcards**.   
/// Wildcards are subterms of a non-canonical formula that may be replaced with anything, without effecting the truth value of the formula.
/// Wildcards are discovered by observing proofs and identifying those terms that are removed 
/// from the formula by rules like |F.1 => T, |.1F => T, or terms that are replaced with a constant during wildcard analysis.
/// Note that in these rules .1 represents a formula that can be anything, the resulting truth value will not change.  
/// Such terms may be replaced with anything and the formula will still reduce to the same canonical formula.
///
/// NandSat takes advantage of wildcards by testing formulas for wildcards by...
///     - replacing all instances of a term in a formulas antecedent/subsequent with a constant test value (that is, T or F), and 
///     - if reducing the result identifies all matching terms in the formula's subsequent/antecedent as wildcards then 
///     - replace all matching terms in the formulas subsequent/antecedent with the opposite of the test value (F or T).  
/// 
/// This class will detect...
/// - reductions by the rules |F.1 => T and |.1F => T. that remove a given subterm from the starting formula.
/// - terms that are replaced with a constant during wildcard analysis.
/// If, after reducing the starting formula, this proof tracer has detected a wildcard then FoundReductionTarget will 
/// be non-negative.
/// 
/// Create an instance of this class before reducing a formula.
/// Then pass a ref to the OnReduction method as the proofTracer to the NandReducer.Reduce method.
/// 
/// Note: Once upon a time NandSat took advantage of wildcards by testing formulas for wildcards by...
///     - replacing all instances of a term in a formulas antecedent with a constant test value (that is, T or F), and 
///     - if reducing the result identifies a wildcard then 
///     - replacing all matching terms in the formulas subsequent the opposite of the test value.  
/// It was thought at the time that just finding the first such wildcard was sufficient.  
/// It was also though that there was a performance advantage in stopping a reduction after the first wildcard was found.  
/// But it turns out that its necessary for all matching instances of the test term to be wildcards.
/// 
/// </summary>
public class WildcardAnalyzer : Proof
{
    /// <summary>
    /// The formula that will be reduced.
    /// Callbacks to the OnReduction method will have reduction.StartingNand == StartingNand 
    /// </summary>
    public Formulas.Nand StartingNand { get; }
    /// <summary>
    /// The subterm that we're watching for
    /// </summary>
    public Formula Subterm {  get; }

    public Constant TestValue {  get; }

    public WildcardAnalyzer(Formulas.Nand startingFormula, Formula subterm, Constant testValue)
        :base(startingFormula)
    {
        this.Subterm=subterm;
        this.StartingNand=startingFormula;
        this.TestValue=testValue;
        Debug.Assert(0 <= startingFormula.PositionOf(Subterm));
    }

    ///// <summary>
    ///// A reduction to StartingNand that identifies a 'wildcard' 
    ///// that matches the subterm this proof tracer is looking for.
    ///// </summary>
    //public ReductionResult WildcardReduction { get; private set; }

    /// <summary>
    /// The position within StartingFormula of an instance of Subterm also a 'wildcard'.
    /// </summary>
    public int ReductionPosition { get; private set; } = -2;
    public bool FoundReductionTarget() => 0 <= ReductionPosition;

    //public IEnumerable<int> Mapping 
    //{
    //    get 
    //    {
    //        var deltaLength = StartingNand.Length - ReducedFormula.Length;
    //        return Enumerable.Repeat(0, ReductionPosition)
    //                .Concat(Enumerable.Repeat(ReductionPosition + deltaLength, deltaLength));
    //    }
    //}



    /// <summary>
    /// This method detects wildcards.  
    /// It works by tracking, for each reduction, the position of all instances of SubTerm in StartingFormula.  
    /// When a wildcard is detected it calculates the position, in the starting formula, of the matching 
    /// wildcard in the reduction.  
    /// </summary>
    /// <param name="reduction"></param>
    override public bool SetNextReduction(Reduction reduction)
    {
        if (ReductionPosition < -1)
        {
            // todo: using strings is fragile, at least get off your lazy ass and create constants.
            if (reduction.RuleDescriptor == "|.1F => T" || reduction.RuleDescriptor == "|F.1 => T")
            {
                // discover the position at which the reduction occurred
                var reductionPosition = -1;
                var startingFlatTerms = new FormulaDFSEnumerator(StartingFormula).ToArray();
                {
                    int i = 0;
                    foreach (var term in new FormulaDFSEnumerator(ReducedFormula))
                    {
                        if (term.Equals(Constant.TRUE))
                        {
                            var startingTerm = startingFlatTerms[i];
                            if (!term.Equals(startingTerm))
                            {
                                reductionPosition = i;
                                break;
                            }
                        }
                        i++;
                    }
                }

                if (0 <= reductionPosition)
                {
                    var ruleTarget = startingFlatTerms[reductionPosition];
                    int subtermPosition = ruleTarget.PositionOf(Subterm);
                    if (0 <= subtermPosition)
                    {
                        ReductionPosition = reductionPosition + subtermPosition;

                        if (Reductions.Any())
                        {
                            ReductionPosition = Mapping[ReductionPosition];
                        }

                        if (0 <= ReductionPosition)
                        {

                            var ok = base.SetNextReduction(reduction);

#if DEBUG
                            if (ok)
                            {
                                if (!(0 <= ReductionPosition && ReductionPosition < StartingNand.Length))
                                {
                                    throw new AssertFailedException("0 <= ReductionPosition && ReductionPosition < StartingFormula.Length");
                                }
                                if (!(Subterm.Equals(StartingNand.GetFormulaAtPosition(ReductionPosition))))
                                {
                                    throw new AssertFailedException($"an instance of the subterm {Subterm} was not found at position {ReductionPosition}");
                                }
                            }
#endif
                            return true;

                        }
                    }
                }
            }
            else if (reduction.RuleDescriptor.StartsWith("wildcard")) // todo: this is inefficient and fragile, maybe introduce a subclass of ReductionResult? Or marker interface?
            {
                var reductionPosition = -1;
                int i = 0;
                var reducedFlatTerm = reduction.ReducedFormula.AsFlatTerm();
                var startingFlatTerm = reduction.StartingFormula.AsFlatTerm();
                var wildIdentifier = TestValue.Equals(Constant.TRUE) ? Constant.FALSE : Constant.TRUE;
                foreach (var term in reducedFlatTerm)
                {
                    if (term.Equals(wildIdentifier))
                    {
                        var startingTerm = startingFlatTerm[i];
                        if (!term.Equals(startingTerm))
                        {
                            reductionPosition = i;
                            break;
                        }
                    }
                    i++;
                }
                if (0 <= reductionPosition)
                {
                    var ruleTarget = startingFlatTerm[reductionPosition];
                    int subtermPosition = ruleTarget.PositionOf(Subterm);
                    if (0 <= subtermPosition)
                    {
                        if (Reductions.Any())
                        {
                            ReductionPosition = Mapping.ElementAt(reductionPosition + subtermPosition);
                            //ReductionPosition = ReductionProof.GetPositionAtStartOfProof(reductionPosition + subtermPosition);
                        }
                        else
                        {
                            ReductionPosition = subtermPosition;
                        }
#if DEBUG
                        if (ReductionPosition != -1)
                        {
                            if (!(0 <= ReductionPosition && ReductionPosition < StartingNand.Length))
                            {
                                // probably indicates that the mapping is wrong or doesnt map enough variable instances
                                throw new AssertFailedException("invalid reduction mapping found");
                            }
                            if (!(Subterm.Equals(StartingNand.GetFormulaAtPosition(ReductionPosition))))
                            {
                                throw new AssertFailedException($"an instance of the subterm {Subterm} was not found at position {ReductionPosition}");
                            }
                        }
#endif
                    }
                }
            }
        }

        return base.SetNextReduction(reduction);
    }
}