
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using TermSAT.Formulas;

namespace TermSAT.NandReduction;

/// <summary>
/// Proofs are used by the Nand Reduction Algorithm to detect 'wildcards'.
/// Proofs are also super useful for testing and debugging.
/// 
/// A proof shows how to transform one formula to an equivalent, less complex, formula.
/// A proof is a list of reductions from a starting formula to a reduced formula that 
/// proves that the starting and reduced formulas are equivalent.
/// 
/// Also, a proof provides a map of the terms in the reduced formula to the terms in the starting formula.
/// 
/// </summary>
public class Proof 
{
    private List<Reduction> _reductions = new();
    private Proof ParentProof {  get; }
    private IImmutableList<int> _mapping = null;

    public Proof()
    {
    }

    /// <summary>
    /// Create a child proof.
    /// The given ref to the parent is only used to check for infinite loops
    /// </summary>
    /// <param name="proof"></param>
    public Proof(Proof proof)
    {
        ParentProof = proof;
    }

    public IReadOnlyList<Reduction> Reductions => _reductions;

    public Formula ReducedFormula
    {
        get
        {
            if (0 < _reductions.Count)
            {
                return _reductions.Last().ReducedFormula;
            }
            return null;
        }
    }

    public Formula StartingFormula
    {
        get
        {
            if (0 < _reductions.Count)
            {
                return _reductions.First().StartingFormula;
            }
            return null;
        }
    }

    /// <summary>
    /// Returns false if the reduction already exists in the proof.  
    /// </summary>
    public virtual bool AddReduction(Reduction reduction)
    {

        // ignore
        if (reduction.RuleDescriptor == Reduction.FORMULA_IS_CANONICAL)
        {
            return true;
        }

        // detect infinite loops
        var pp = this;
        while(pp != null)
        {
            if (pp.Reductions.Where(r => r.StartingFormula.Equals(reduction.StartingFormula)).Any())
            {
                return false;
            }
            pp = pp.ParentProof;
        }


#if DEBUG
        // A reduction's starting formula should match the previous reductions' reduced formula
        if (0 < _reductions.Count && !ReducedFormula.Equals(reduction.StartingFormula))
        {
            throw new TermSatException("A reduction's starting formula should match the previous reductions' reduced formula");
        }
#endif

        _reductions.Add(reduction);
        _mapping = null;

#if DEBUG
        var mapping = ReductionMapping.ToImmutableList();
        if (0 < mapping.Count && ReducedFormula.Length !=  mapping.Count)
        {
            throw new TermSatException("A reduction mapping's size should be the same as the formula from which it maps");
        }
        foreach (var t in mapping)
        {
            if (t < -1)
            {
                throw new TermSatException($"A negative value, other than -1, is a valid reduction mapping value");
            }
            if (-1 < t)
            {
                if (StartingFormula.Length <= t)
                {
                    throw new TermSatException($"A reduction mapping value must be within range of the starting formula");
                }
            }
        }
#endif


        return true;
    }

    /// <summary>
    /// Returns a mapping that maps from the ReducedFormula of the last reduction to the 
    /// StartingNand of the first reduction
    /// </summary>
    public virtual IImmutableList<int> ReductionMapping
    {
        get
        {
            if (_mapping == null)
            {
                _mapping = ImmutableList<int>.Empty;

                if (_reductions != null && 0 < _reductions.Count)
                {
                    List<int> map = Enumerable.Range(0, ReducedFormula.Length).ToList();
                    for (int r = _reductions.Count; 0 < r--;)
                    {
                        var reduction = _reductions[r];
                        while (map.Count < reduction.ReducedFormula.Length)
                        {
                            map.Add(map.Count);
                        }
                        while (map.Count < reduction.StartingFormula.Length)
                        {
                            map.Add(map.Count);
                        }
                        for (int i = 0; i < reduction.ReducedFormula.Length; i++)
                        {
                            if (0 <= map[i])
                            {
                                map[i] = reduction.Mapping[map[i]];
                            }
                        }
                    }
                    _mapping = map.Take(ReducedFormula.Length).ToImmutableList();
                }
            }
            return _mapping;
        }
        //get
        //{
        //    int[] map = Enumerable.Range(0, ReducedFormula.Length).ToArray();
        //    if (0 < Reductions.Count)
        //    {
        //        for (int i = 0; i < ReducedFormula.Length; i++)
        //        {
        //            var m = map[i];
        //            for (int r = Reductions.Count; 0 <= m && 0 < r--;)
        //            {
        //                map[i] = Reductions[r].Mapping.ElementAt(m);
        //            }
        //        }
        //    }
        //    return map;
        //}

    }
}

public static class ProofExtensions
{
    public static int GetPositionAtStartOfProof(this Proof proof, int currentPosition) 
    { 
        if (currentPosition < 0 || proof.Reductions.Count <= 0 || proof.ReducedFormula.Length <= currentPosition)
        {
            return -1;  // the term at position currentPosition in ReducedFormula is not found in the starting formula
        }


        int startingPosition = currentPosition;

        for (int i = proof.Reductions.Count; 0 <= --i;)
        {
            var reduction = proof.Reductions[i];
            var reducedPosition = reduction.Mapping.Skip(startingPosition).First();
            startingPosition = reducedPosition;
        }

        return startingPosition;
    }
}