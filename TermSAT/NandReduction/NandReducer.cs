using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction;

/// <summary>
/// 
/// Implements the NandReducer.Reduce method, which, for a given Nand formula, returns an equivalent formula in canonical form.  
/// A 'nand formula' is a formula that only uses nand operators, the constants T an F, and numbered variables.
/// 
/// This class also maintains a global collection of 'proofs' that are used to reduce formulas.  
/// This collection is built out at runtime as formulas are reduced, causing reductions to be discovered, 
/// causing proofs to be created and/or updated.  
/// For every formula a global proof is maintained. 
/// A proof specifies...
///     - Reduction: an atomic Reduction to a simpler form of a formula based on a rule.
///         This nextReduction must be populated and never changes.  
///         If the formula is canonical then NextReduction will have a description like "formula is canonical".  
///     - Result: the simplest known form of the formula. 
///         Basically a slot for memoizing the last value found for ReducedFormula.
/// When Result is also known to be canonical then we say that the proof is complete.
/// Note that proofs of equivalent formulas form a kind of skip list to the simplest, canonical form of the formula.  
/// 
/// // todo: Use proofs as rules when reducing formulas.
/// NandReducer also uses proofs as rules when reducing formulas.
/// For example, suppose the formula |||.1.2|.3|T.2||.2.3|.1|T.2 is reduced to |.1.3.
/// Going forward, NandReducer will attempt to use the rule |||.1.2|.3|T.2||.2.3|.1|T.2 => |.1.3 to reduce 
/// all other formulas before attempting to discover new reductions.
/// That is, any substitution instance of |||.1.2|.3|T.2||.2.3|.1|T.2 can be immediately reduced 
/// using the previously built proof.
/// Basically reusing the work we did to reduce |||.1.2|.3|T.2||.2.3|.1|T.2 to reduce another formula.
/// NandReducer can do this efficiently, see TermSAT.Expressions.InstanceRecognizer.
/// 
/// </summary>
public static class NandReducer
{
    public record struct WildcardReduction(ReductionRecord Reduction, int ReductionPosition, int StartingPosition)
    {
        public static WildcardReduction NotFound() => new(null,-1,-1);
    }

    /// <summary>
    /// Returns the first reduction that can be produced by all of NandReducers' internal rules (like wildcard analysis or proof-rules).
    /// Returns null if no next Reduction can be produced, in which case the given formula is canonical.  
    /// </summary>
    public static async Task<ReductionRecord> TryGetNextReductionAsync(this LucidDbContext db, ReductionRecord startingRecord)
    {
#if DEBUG
        //Debug.Assert(!reductionProof.HasReduced(startingFormula), $"reductionProof has already reduced formula {startingFormula}");
        //if (startingFormula.Equals(Formula.GetOrParse("|T||.1.3|T.2")))
        //{

        //}
#endif

        if (0 < startingRecord.NextReductionId)
        {
            var nextReduction = await db.Expressions.FindAsync(startingRecord.NextReductionId);
            Debug.Assert(nextReduction != null, $"internal error: formula not found {startingRecord.NextReductionId}");
            return nextReduction;
        }
       
        // if given formula is not a nand then it must be a variable or constant and is not reducible.
        ReductionRecord result= null;
        if (startingRecord.Formula is Nand lastNand)
        {
            result= await db.TryLookupReductionAsync(lastNand);
            if (result != null)
            {
                goto FoundReduction;
            }

            result= await db.TryReduceDescendantsAsync(startingRecord);
            if (result != null) 
            {
                goto FoundReduction;
            }

            result = await db.TryReduceConstantsAsync(startingRecord);
            if (result != null)
            {
                goto FoundReduction;
            }

            result = await db.TryReduceCommutativeFormulas(startingRecord);
            if (result != null)
            {
                goto FoundReduction;
            }

            result = await db.TryGetCofactorReductionAsync(startingRecord);
            if (result != null)
            {
                goto FoundReduction;
            }

            //result = await db.TryGetWildcardReductionAsync(startingRecord);
            //if (result != null)
            //{
            //    goto FoundReduction;
            //}

            //result = await db.TryGetSwappedWildcardReductionAsync(startingRecord);
            //if (result != null)
            //{
            //    goto FoundReduction;
            //}

            //if (lastNand.TryReduceWildcards_NoConstants(out result))
            //{
            //    return true;
            //}

            //if (lastNand.ReduceDistributiveFormulas(out result))
            //{
            //    return true;
            //}
        }

        return null; // reduction not found

        FoundReduction:;

        return result;
    }

    /// <summary>
    /// Repeatedly reduces startingFormula until it reaches its canonical form.  
    /// The process stops when no more reductions can be made.
    /// Always returns a logically equivalent formula in canonical form.  
    /// </summary>

    /// <summary>
    /// Returns a tuple of...
    ///     - WildcardReduction : the first reduction of startingRecord that identifies the target term as a wildcard.  
    ///     - ReductionPosition : the position within WildcardReduction.Formula of the target term.  
    ///     - StartingPosition : the position within startingRecord.Formula of the target term.  
    /// 
    /// Repeatedly reduces startingFormula until...
    ///     - a reduction that identifies targetTerm as a wildcard is found.  
    ///     - all instances of the target term have been reduced.  
    ///     - startingRecord is reduced to its canonical form and cant be reduced any further.  
    /// Returns a tuple with WildcardReduction == null if no wildcard reduction was found.
    /// 
    /// </summary>
    /// <param name="targetTerm">any sub-term of the starting formula that's not a constant</param>
    public static async Task<WildcardReduction> TryGetWildcardReductionAsync(this LucidDbContext db, ReductionRecord startingRecord, Formula targetTerm, Constant testValue)
    {
        // constants cant be reduced
        if (startingRecord.VarCount == 0)
        {
            return WildcardReduction.NotFound();
        }

        // if formula is known to be canonical then formula cant be reduced
        if (startingRecord.IsCanonical)
        {
            return WildcardReduction.NotFound();
        }

        // follow/create reductions while looking for one that identifies a wildcard
        var prevReduction = startingRecord;
        var prevReductionTerms = prevReduction.Formula.AsFlatTerm().ToArray();
        var nextReduction = await db.TryGetNextReductionAsync(prevReduction);
        var proofPath = new List<ReductionRecord>();
        while (nextReduction != null)
        {
            var nextFlatTerms = nextReduction.Formula.AsFlatTerm().ToArray();

            if (prevReduction.RuleDescriptor == "|.1F => T" || prevReduction.RuleDescriptor == "|F.1 => T")
            {
                // if .1 contains targetTerm then targetTerm is a wildcard
                {
                    for (int i = 0; i < prevReductionTerms.Length; i++)
                    {
                        var startingTerm = prevReductionTerms[i];
                        if (startingTerm.Equals(targetTerm))
                        {
                            int startingPosition = i;
                            foreach (var r in proofPath) 
                            {
                                startingPosition = r.Mapping[startingPosition];
                            }
                            if (0 <= startingPosition) 
                            {
                                return new(nextReduction, i, startingPosition); // WILDCARD FOUND!
                            }
                        }
                    }
                }
            }

            // Replacing a target term during a reduction also identifies a wildcard.
            // Currently, replacement only happens during wildcard analysis
            // See NandSchemeReductionTests.ReduceFormulaWithManyTargetsOneWildcard for an example.
            else if (prevReduction.RuleDescriptor.StartsWith("wildcard")) 
            {
                var reductionPosition = -1;
                {
                    int i = 0;
                    var wildIdentifier = testValue.Equals(Constant.TRUE) ? Constant.FALSE : Constant.TRUE;
                    foreach (var term in nextFlatTerms)
                    {
                        if (term.Equals(wildIdentifier))
                        {
                            var startingTerm = prevReductionTerms[i];
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
                    var ruleTarget = prevReductionTerms[reductionPosition];
                    int subtermPosition = ruleTarget.PositionOf(targetTerm);
                    if (0 <= subtermPosition)
                    {
                        var adjustedReductionPosition = reductionPosition + subtermPosition;
                        int startingPosition = adjustedReductionPosition;
                        foreach (var r in proofPath)
                        {
                            startingPosition = r.Mapping[startingPosition];
                        }
                        WildcardReduction wildcardReduction = new(nextReduction, adjustedReductionPosition, startingPosition); // WILDCARD FOUND!

#if DEBUG
                        if (adjustedReductionPosition != -1)
                        {
                            if (!(0 <= adjustedReductionPosition && adjustedReductionPosition < startingRecord.Length))
                            {
                                // probably indicates that the mapping is wrong or doesnt map enough variable instances
                                throw new AssertFailedException("invalid reduction mapping found");
                            }
                            if (!(targetTerm.Equals(startingRecord.Formula.GetFormulaAtPosition(adjustedReductionPosition))))
                            {
                                throw new AssertFailedException($"an instance of the subterm {targetTerm} was not found at position {adjustedReductionPosition}");
                            }
                        }
#endif
                    }
                }
            }

            // move to, or create, the next reduction
            proofPath.Add(prevReduction);
            prevReduction = nextReduction;
            prevReductionTerms = nextFlatTerms;
            nextReduction = await db.TryGetNextReductionAsync(nextReduction);
        }

        return WildcardReduction.NotFound(); // no wildcard reduction found;
    }

}