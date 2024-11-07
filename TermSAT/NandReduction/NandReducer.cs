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
    /// Returns a canonical formula.
    /// </summary>
    public static Formula NandReduction(this Formula startingFormula, Proof proof)
    {
        // if given formula is not a nand then it must be a variable or constant and is not reducible.
        if (!(startingFormula is Formulas.Nand startingNand))
        {
            return startingFormula;
        }

        Formula reducedFormula = startingFormula;

        //{ // first, reduce both the antecedent and sequent 
        //    {
        //        var childProof = new Proof(proof);
        //        var reducedAntecedent = startingNand.Antecedent.NandReduction(childProof);
        //        if (!reducedAntecedent.Equals(startingNand.Antecedent))
        //        {
        //            // lift the reductions in the child proof up to the parent proof.  
        //            // the child proof will eventually be abandoned.
        //            foreach (var r in childProof.Reductions)
        //            {
        //                var reduced = Formulas.Nand.NewNand(r.ReducedFormula, startingNand.Subsequent);
        //                var mapping = SystemExtensions.ConcatAll(
        //                    new[] { 0 },
        //                    r.Mapping.Select(i => i + 1),
        //                    Enumerable.Range(r.StartingFormula.Length + 1, startingNand.Subsequent.Length)
        //                ).ToImmutableList();
        //                var parentReduction = new Reduction(reducedFormula, reduced, r.RuleDescriptor, mapping);
        //                if (!proof.AddReduction(parentReduction)) 
        //                { 
        //                    break; 
        //                }
        //                reducedFormula = startingNand= reduced;
        //            }
        //        }
        //    }
        //    {
        //        var childProof = new Proof(proof);
        //        var reducedSubsequent = startingNand.Subsequent.NandReduction(childProof);
        //        if (!reducedSubsequent.Equals(startingNand.Subsequent))
        //        {
        //            // lift the reductions in the child proof up to the parent proof.  
        //            foreach (var r in childProof.Reductions)
        //            {
        //                var reduced = Formulas.Nand.NewNand(startingNand.Antecedent, r.ReducedFormula);
        //                var mapping = SystemExtensions.ConcatAll(
        //                    Enumerable.Range(0, startingNand.Antecedent.Length + 1),
        //                    r.Mapping.Select(i => i + startingNand.Antecedent.Length)
        //                ).ToImmutableList();
        //                var parentReduction = new Reduction(reducedFormula, reduced, r.RuleDescriptor, mapping);
        //                if (!proof.AddReduction(parentReduction))
        //                {
        //                    break;
        //                }
        //                reducedFormula = reduced;
        //            }
        //        }
        //    }
        //}

        // now render the two canonical parts together
        // note: just calling NandReduction will result in an infinite loop.
        // Repeatedly calling SingleNandReduction here avoids that problem.
        {
            while (true)
            {
                var result = reducedFormula.SingleNandReduction(proof);
                if (result.RuleDescriptor == Reduction.FORMULA_IS_CANONICAL || !proof.AddReduction(result))
                {
                    break;
                }
                reducedFormula = result.ReducedFormula;
            }
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