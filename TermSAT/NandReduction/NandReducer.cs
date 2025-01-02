using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TermSAT.Formulas;

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
///         This reduction must be populated and never changes.  
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
/// NandReducer can do this efficiently, see TermSAT.Formulas.InstanceRecognizer.
/// 
/// </summary>
public static class NandReducer
{
    /// <summary>
    /// Returns the first reduction that can be produced by all of NandReducers' internal rules (like wildcard analysis or proof-rules).
    /// Returns false if no reduction is found, in which case the given formula is canonical.  
    /// </summary>
    static bool TryFindReduction(this Formula startingFormula, out Reduction result)
    {
#if DEBUG
        //Debug.Assert(!reductionProof.HasReduced(startingFormula), $"reductionProof has already reduced formula {startingFormula}");
        if (startingFormula.Equals(Formula.GetOrParse("|T||.1.3|T.2")))
        {

        }
#endif

        var lastReduction = startingFormula;

        {   // get any cached value
            var reductionProof = Proof.GetReductionProof(startingFormula);
            if (reductionProof.ReducedFormula != null)
            {
                lastReduction = reductionProof.ReducedFormula;
            }
        }

        List<Reduction> incompleteProofs = new();

        // if given formula is not a nand then it must be a variable or constant and is not reducible.
        if (lastReduction is Nand lastNand)
        {
            if (lastNand.TryReduceDescendants(out result)) 
            {
                return true;
            }

            if (lastNand.TryReduceConstants(out result))
            {
                return true;
            }

            if (lastNand.TryReduceWildcards(out result))
            {
                return true;
            }

            //if (lastNand.TryReduceWildcards_NoConstants(out result))
            //{
            //    return true;
            //}

            //if (lastNand.TryReduceCommutativeFormulas(out result))
            //{
            //    return true;
            //}

            //if (lastNand.ReduceDistributiveFormulas(out result))
            //{
            //    return true;
            //}
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Repeatedly reduces startingFormula to its canonical form.  
    /// The reduction process stops when no more reductions can be made.
    /// Returns a logically equivalent formula in canonical form.
    /// </summary>
    public static Formula Reduce(this Formula startingFormula)
    {
        var startingProof = Proof.GetReductionProof(startingFormula);
        if (!startingProof.IsComplete())
        {
            // Repeatedly reduce formulas, depth-first, until starting formula is complete
            Stack<Proof> todo = new Stack<Proof>();
            todo.Push(startingProof);
            var reducedFormula = startingProof.ReducedFormula;
            if (reducedFormula == null) 
            { 
                reducedFormula = startingFormula;
            }

            while (todo.Any())
            {
                var todoProof = todo.Peek();
                if (!todoProof.IsComplete())
                {
                    // handle an empty proof
                    if (todoProof.NextReduction == null)
                    {
                        if (todoProof.StartingFormula.TryFindReduction(out var reduction))
                        {
                            todoProof.SetNextReduction(reduction);
                            todo.Push(Proof.GetReductionProof(reduction.ReducedFormula));
                            reducedFormula = reduction.ReducedFormula;
                        }
                        else
                        {
                            // the given formula is canonical
                            todoProof.AddCompletionMarker(todoProof.StartingFormula);
                            reducedFormula = todoProof.StartingFormula;
                        }
                        continue;
                    }

                    // if last formula is not yet complete then complete it first.
                    var lastProof = Proof.GetReductionProof(todoProof.ReducedFormula);
                    if (!lastProof.IsComplete())
                    {
                        todo.Push(lastProof);
                        continue;
                    }
                    Debug.Assert(lastProof.ReducedFormula.CompareTo(reducedFormula) <= 0);
                    reducedFormula = lastProof.ReducedFormula;

                    // all that's missing is a completion marker, add it
                    todoProof.AddCompletionMarker(todoProof.StartingFormula);
                }
                todo.Pop();
            }
        }
        return startingProof.ReducedFormula;
    }
}