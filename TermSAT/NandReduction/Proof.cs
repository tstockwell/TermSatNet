
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using TermSAT.Formulas;
using static System.Net.Mime.MediaTypeNames;

namespace TermSAT.NandReduction;

/// <summary>
/// A proof is a list of reductions from some formula to a reduced formula that 
/// proves that the terms and reduced formulas are equivalent.
/// 
/// NandSAT basically works by building a set of proofs, for every term in a formula, to the terms canonical form.
/// These proofs are called 'reduction proofs'.
/// Reduction proofs are built from the bottom up.
/// Initially a formula will have a reduction proof that has no reductions.
/// If a formula can no longer be reduced then it 
/// then it has been proved to be canonical and 
/// the last reduction in a proof will have RuleDescription == CANONICAL.
/// When a reduction proofs ReducedFormula is canonical then the proof is complete and no longer updated.
/// 
/// </summary>
public class Proof 
{
    private static readonly MemoryCacheOptions cacheOptions = new MemoryCacheOptions();
    private static readonly MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5));
    private static readonly MemoryCache __cache = new(cacheOptions);

    public static Proof GetReductionProof(Formula startingFormula)
    {
        if (!__cache.TryGetValue(startingFormula, out Proof result))
        {
            result = new Proof(startingFormula);
            __cache.Set(startingFormula, result, cacheEntryOptions);
        }
        return result;
    }

    protected Proof(Formula startingFormula)
    {
        StartingFormula = startingFormula;
    }


    //public Formula ReducedFormula
    //{
    //    get
    //    {
    //        if (0 < _reductions.Count)
    //        {
    //            return _reductions.Last().ReducedFormula;
    //        }
    //        return null;
    //    }
    //}

    public Formula StartingFormula { get; }


    public Reduction NextReduction { get; private set; } = null;

    Formula _reducedFormula= null;
    Formula _canonicalFormula = null;
    public Formula ReducedFormula 
    { 
        get
        {
            if (_canonicalFormula != null)
            {
                return _canonicalFormula;
            }
            if (NextReduction == null)
            {
                return StartingFormula;
            }
            if (_reducedFormula == null)
            {
                _reducedFormula = NextReduction.ReducedFormula;
            }
            while(true)
            {
                var nextProof = Proof.GetReductionProof(_reducedFormula);
                if (nextProof.IsComplete())
                {
                    _canonicalFormula = _reducedFormula = nextProof.ReducedFormula;
                    break;
                }
                if (nextProof.NextReduction == null)
                {
                    break;
                }
                _reducedFormula = nextProof.NextReduction.ReducedFormula;
            }
            return _reducedFormula;
        }
    }


    /// <summary>
    /// Returns false if the reduction already exists in the proof.  
    /// </summary>
    public virtual bool SetNextReduction(Reduction reduction)
    {

#if DEBUG
        var currentReduced = (ReducedFormula == null) ? StartingFormula : ReducedFormula;
        if (!reduction.StartingFormula.Equals(currentReduced))
        {
            throw new TermSatException("A reduction's starting formula should match the current reduced formula");
        }
#endif

        var todoProof = this;
        while (todoProof != null)
        {
            if (todoProof.NextReduction == null)
            {
                todoProof.NextReduction = reduction;
                break;
            }
            if (todoProof.NextReduction.RuleDescriptor.Equals(Reduction.PROOF_IS_COMPLETE))
            {
                break;
            }
            todoProof = Proof.GetReductionProof(todoProof.ReducedFormula);
        }

#if DEBUG
        var mapping = Mapping.ToImmutableList();
        if (mapping.Count <= 0)
        {
            throw new TermSatException("A reduction mapping's size should be the same as the formula from which it maps");
        }
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
    /// Returns an enumeration of reductions from StartingFormula to StartingFormula's canonical formula.
    /// </summary>
    public IEnumerable<Reduction> Reductions
    {
        get
        {
            var todo = this;
            while (todo.NextReduction != null && !todo.NextReduction.RuleDescriptor.Equals(Reduction.PROOF_IS_COMPLETE))
            {
                yield return todo.NextReduction;
                todo = Proof.GetReductionProof(todo.NextReduction.ReducedFormula);
            }
            yield break;
        }
    }

    /// <summary>
    /// Returns a mapping that maps from the ReducedFormula of the last reduction to the 
    /// StartingNand of the first reduction
    /// </summary>
    public virtual IImmutableList<int> Mapping
    {
        get
        {

            // default mapping, for a canonical formula
            List<int> map = Enumerable.Range(0, Math.Max(ReducedFormula.Length, StartingFormula.Length)).ToList();
            IImmutableList<int> result = map.ToImmutableList();

            if (Reductions.Any())
            {
                foreach (var reduction in Reductions.Reverse())
                {
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
                result = map.GetRange(0, ReducedFormula.Length).ToImmutableList();
            }

            return result;
        }

    }

#if DEBUG
    /// <summary>
    /// The ReducedFormula mapped to StartingFormula, as a string, where elements that map to -1 displayed using '#'
    /// This field is currently just used for debugging, but its super handy.
    /// </summary>
    public string MappedFormula
    {
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
            return string.Join("", mapped);
        }
    }
#endif



}

public static class ProofExtensions
{
    public static bool IsComplete(this Proof proof)
    {
        var nextProof = proof;
        while(nextProof.NextReduction != null)
        {
            if (nextProof.NextReduction.RuleDescriptor.Equals(Reduction.PROOF_IS_COMPLETE))
            {
                return true;
            }
            nextProof = Proof.GetReductionProof(nextProof.ReducedFormula);
        }
        return false;
    }


    public static void AddCompletionMarker(this Proof proof, Formula defaultStartingFormula)
    {
#if DEBUG
        if (defaultStartingFormula == null)
        {
            throw new ArgumentNullException(nameof(defaultStartingFormula));
        }
#endif
        var startingFormula = proof.ReducedFormula;
        if (startingFormula == null) 
        { 
            startingFormula = defaultStartingFormula;
        }
        proof.SetNextReduction(Reduction.CreateCompletionMarker(startingFormula));
    }

}