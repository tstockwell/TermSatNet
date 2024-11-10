using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using TermSAT.Common;
using TermSAT.Formulas;

namespace TermSAT.NandReduction;

public static class NandReducer
{
    /// <summary>
    /// Reduces formulas composed of constants, variables, and nand operators to their canonical form.  
    /// Repeatedly discovers reducible sub-formulas and reduces them.
    /// Reductions are added to the given Proof.
    /// The reduction process stops if a reduction cannot be added to the proof (cuz infinite loop or sumpin) 
    /// of cuz no more reductions can be made.
    /// Returns a canonical formula.
    /// </summary>
    public static Formula NandReduction(this Formula startingFormula, Proof proof)
    {
        // if given formula is not a nand then it must be a variable or constant and is not reducible.
        if (!(startingFormula is Nand))
        {
            return startingFormula;
        }

        // Repeatedly call SingleNandReduction until no more reductions
        Formula reducedFormula = startingFormula;
        while (true)
        {
            var result = reducedFormula.SingleNandReduction(proof);

            if (result.ReducedFormula.CompareTo(reducedFormula) < 0)
            {
                if (proof.AddReduction(result))
                {
                    reducedFormula = result.ReducedFormula;
                    continue;
                }
            }
            break;
        }

        return reducedFormula;
    }

    static private ConditionalWeakTable<Formula, Reduction> __reductionResults = new ConditionalWeakTable<Formula, Reduction>();

    public static Reduction SingleNandReduction(this Formula startingFormula, Proof proof)
    {
        Reduction reductionResult = null;
        List<Reduction> incompleteProofs = new();

        {   // return any cached value
            if (__reductionResults.TryGetValue(startingFormula, out reductionResult))
            {
                return reductionResult;
            }
        }

        // if given formula is not a nand then it must be a variable or constant and is not reducible.
        if (startingFormula is Formulas.Nand startingNand)
        {
            reductionResult= NandReducerDescendantRules.ReduceDescendants(startingNand, proof);
            if (reductionResult.ReducedFormula.CompareTo(reductionResult.StartingFormula) < 0)
            {
                goto ReductionFound;
            }
            incompleteProofs.AddRange(reductionResult.IncompleteProofs);

            reductionResult= startingNand.ReduceConstants(proof);
            if (reductionResult.ReducedFormula.CompareTo(reductionResult.StartingFormula) < 0)
            {
                goto ReductionFound;
            }
            incompleteProofs.AddRange(reductionResult.IncompleteProofs);

            reductionResult= startingNand.ReduceWildcards(proof);
            if (reductionResult.ReducedFormula.CompareTo(reductionResult.StartingFormula) < 0)
            {
                goto ReductionFound;
            }
            incompleteProofs.AddRange(reductionResult.IncompleteProofs);

            reductionResult= startingNand.ReduceDistributiveFormulas(proof);
            if (reductionResult.ReducedFormula.CompareTo(reductionResult.StartingFormula) < 0)
            {
                goto ReductionFound;
            }
            incompleteProofs.AddRange(reductionResult.IncompleteProofs);

            reductionResult= startingNand.ReduceCommutativeFormulas(proof);
            if (reductionResult.ReducedFormula.CompareTo(reductionResult.StartingFormula) < 0)
            {
                goto ReductionFound;
            }
            incompleteProofs.AddRange(reductionResult.IncompleteProofs);
        }

        reductionResult = Reduction.NoChange(startingFormula, incompleteProofs.ToImmutableList());

        ReductionFound:

        if (incompleteProofs.Any())
        {
            return reductionResult;
        }

        lock (__reductionResults)
        {
            // return any cached value or cache found reduction
            if (__reductionResults.TryGetValue(startingFormula, out var currentCached))
            {
                // recursive rules (like distributive) *may* cause NO_CHANGE to be cached when 
                // a rule is not reduced because it would cause infinite loop.
                if (currentCached.ReducedFormula.CompareTo(reductionResult.ReducedFormula) <= 0)
                {
                    return currentCached;
                }
            }
            __reductionResults.AddOrUpdate(startingFormula, reductionResult);
        }

        return reductionResult;
    }

    public static Formula NandReduction(this Formula startingFormula)
    {
        Proof proof = new Proof();
        return NandReduction(startingFormula, proof);
    }
}