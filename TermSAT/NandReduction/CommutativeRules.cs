﻿using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TermSAT.Common;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction;

public static class CommutativeRules
{

    /// <summary>
    /// 'Commutative' rules are rules where the length doesn't change, just the symbols are rearranged.  
    /// </summary>
    public static async Task<ReductionRecord> TryReduceCommutativeFormulas(this LucidDbContext db, ReductionRecord startingRecord)
    {
        // if given formula is not a nand then it must be a variable or constant and is not reducible.
        if (!(startingRecord.Formula is Nand startingNand))
        {
            return null;
        }

        // |.2.1 => |.1.2 
        // A pure ordering rule, because the length doesn't change, just the symbols are rearranged.
        // swap .1 <-> .2
        // This rule has been implemented here even though its subsumed subsumed by paste-and-cut.   
        // I think that the ordering reduction should be explicit because it's explicit in the proof paper.  
        // In paste-and-cut, .1 and .2 can be swapped for each other (when the result is simpler) since both are fgf-cofactors.  
        //
        // NOTE 1/22/25...
        //  Still no such thing as 'instance swapping' in the reduction algorithm.
        //  The concept of wildcard as used here refers to the fact that when one term is F the other is irrelevant.
        //  Therefore we can swap them for each other, but only when the result is simpler.
        {
            if (startingNand.Subsequent.CompareTo(startingNand.Antecedent) < 0)
            {
                var reducedFormula = Formulas.Nand.NewNand(startingNand.Subsequent, startingNand.Antecedent);
                var nextReduction = await db.GetMostlyCanonicalRecordAsync(reducedFormula);

                startingRecord.RuleDescriptor = "|.2.1 => |.1.2";
                startingRecord.NextReductionId =  nextReduction.Id;

                await db.SaveChangesAsync();

                return nextReduction;
            }
        }


        /**
         * 
         * 
         
        // |.2|T|.1.3 => |.1|T|.2.3.  
        // A pure ordering rule, because the length doesn't change, just the symbols are rearranged.
        // swap .1 <-> .2
        // NOTE!!!!!!: This rule could be subsumed by wildcard analysis if wildcard analysis is extended to discover 
        // when two terms are wildcards for each other and swapping them reduces the formula.
        {
            if (startingNand.Subsequent is Expressions.Nand nand
                && nand.Antecedent == Constant.TRUE
                && nand.Subsequent is Expressions.Nand nandSubNand
                && nandSubNand.Antecedent.CompareTo(startingNand.Antecedent) < 0)
            {
                var reducedFormula = Formulas.Nand.NewNand(
                    nandSubNand.Antecedent,
                    Formulas.Nand.NewNand(
                        Constant.TRUE,
                        Formulas.Nand.NewNand(
                            startingNand.Antecedent,
                            nandSubNand.Subsequent)));
                // maybe this is just lazy coding, but it's the easiest way to deal with all order conditions
                if (reducedFormula.CompareTo(startingNand) < 0)
                {
                    var mapping = Enumerable.Repeat(-1, 1) // |
                        .Concat(Enumerable.Range(startingNand.Antecedent.Length + 4, nandSubNand.Antecedent.Length)) // .1
                        .Concat(Enumerable.Range(startingNand.Antecedent.Length + 1, 3)) // |T|
                        .Concat(Enumerable.Range(1, startingNand.Antecedent.Length)) //.2
                        .Concat(Enumerable.Range(startingNand.Antecedent.Length + nandSubNand.Antecedent.Length + 4, nandSubNand.Subsequent.Length)) //.3
                        .ToImmutableList();
                    result = new ReductionRecord(startingNand, reducedFormula, "|.2|T|.1.3 => |.1|T|.2.3", mapping);
                    return true;
                }
            }
        }

        // The remainder of the 'commutative' rules appear to be discoverable via an algorithm 
        // similar to the wildcard analysis algorithm, or maybe as an extension to the wildcard 
        // algorithm.
        // Instead of searching for common pairs of terms where one can be replaced, the
        // 'wormhole algorithm' instead looks for terms in antecedents that can be swapped
        // with constants in the associated subsequent. 
        // The wormhole algorithm searches for these terms in the same way that the wildcard
        // algorithm does, but, for test values of F, looks for reduced formulas where all Ts 
        // have been removed.
        // These constant wildcards can be swapped with 
        // 

        // five operators
        // ||.1|.2.3|.2|T.1 => ||.1|T.2|.2|.1.3 swap T,.3
        // ||.1|.2.3|.3|T.1 => ||.1|T.3|.3|.1.2 swap T,.2



        // ||.1|.2.3|.2|T.1 => ||.1|T.2|.2|.1.3
        // A pure ordering rule, just a reordering of formula symbols
        // NOT reducible via wildcard analysis (verified)
        // Basically the .3 and the T trade places
        // An example of a 'T-slider' rule.
        // NOTE: Can be subsumed by wildcard analysis after extending for 'swappable terms'.
        {
            if (startingNand.Antecedent is Expressions.Nand nandAnt
                && nandAnt.Subsequent is Expressions.Nand nandAntSub
                && startingNand.Subsequent is Expressions.Nand nandSub
                && nandSub.Subsequent is Expressions.Nand nandSubSub
                && nandAntSub.Antecedent.Equals(nandSub.Antecedent)
                && nandAnt.Antecedent.Equals(nandSubSub.Subsequent)
                && nandSubSub.Antecedent.Equals(Constant.TRUE)
                && nandAnt.Antecedent.CompareTo(nandAntSub.Antecedent) < 0
                && nandAntSub.Antecedent.CompareTo(nandAntSub.Subsequent) < 0)
            {
                var reducedFormula = Formulas.Nand.NewNand(
                    Formulas.Nand.NewNand(
                        nandAnt.Antecedent,
                        Formulas.Nand.NewNand(
                            Constant.TRUE,
                            nandAntSub.Antecedent)),
                    Formulas.Nand.NewNand(
                        nandAntSub.Antecedent,
                        Formulas.Nand.NewNand(
                            nandAnt.Antecedent,
                            nandAntSub.Subsequent)));
                if (reducedFormula.CompareTo(startingNand) < 0)
                {
                    var mapping =
                        Enumerable.Repeat(-1, 2) // ||
                        .Concat(Enumerable.Range(2, nandAnt.Antecedent.Length)) // .1
                        .Concat(Enumerable.Repeat(-1, 2)) // |T
                                    // the rightmost .2 in the starting formula is the leftmost .2 in the reduced formula
                        .Concat(Enumerable.Range(nandAnt.Length + 2, nandSub.Antecedent.Length)) // .2
                        .Append(-1) // |
                                    // the leftmost .2 in the starting formula is the rightmost .2 in the reduced formula
                        .Concat(Enumerable.Range(nandAnt.Antecedent.Length + 3, nandAntSub.Subsequent.Length)) // .2
                        .Append(-1) // |
                        .Concat(Enumerable.Range(nandAnt.Length + nandSub.Antecedent.Length + 4, nandAnt.Antecedent.Length)) // .1
                        .Concat(Enumerable.Range(nandAnt.Antecedent.Length + nandAntSub.Antecedent.Length + 3, nandAntSub.Subsequent.Length)) // .3
                        .ToImmutableList();


                    result = new Reduction(startingNand, reducedFormula, "||.1|.2.3|.2|T.1 => ||.1|T.2|.2|.1.3", mapping);
                    return true;
                }
            }
        }

        // ||.1|.2.3|.3|T.1 => ||.1|T.3|.3|.1.2, swap .2 <-> T
        // A pure ordering rule, its simply a reordering of the symbols in the formula
        // An example of a 'T-slider' rule.
        // NOTE: Can be subsumed by wildcard analysis after extending for 'swappable terms'.
        {
            if (startingNand.Antecedent is Expressions.Nand nandAnt
                && nandAnt.Subsequent is Expressions.Nand nandAntSub
                && startingNand.Subsequent is Expressions.Nand nandSub
                && nandSub.Subsequent is Expressions.Nand nandSubSub
                && nandAntSub.Subsequent.Equals(nandSub.Antecedent)
                && nandAnt.Antecedent.Equals(nandSubSub.Subsequent)
                && nandSubSub.Antecedent.Equals(Constant.TRUE)
                //&& nandAnt.Antecedent.CompareTo(nandAntSub.Antecedent) < 0
                && nandAntSub.Antecedent.CompareTo(nandAntSub.Subsequent) < 0)
            {
                var reducedFormula = Formulas.Nand.NewNand(
                    Formulas.Nand.NewNand(
                        nandAnt.Antecedent,
                        Formulas.Nand.NewNand(
                            Constant.TRUE,
                            nandAntSub.Subsequent)),
                    Formulas.Nand.NewNand(
                        nandAntSub.Subsequent,
                        Formulas.Nand.NewNand(
                            nandAnt.Antecedent,
                            nandAntSub.Antecedent)));

                if (reducedFormula.CompareTo(startingNand) < 0)
                {
                    var mapping =
                        Enumerable.Repeat(-1, 2) // ||
                        .Concat(Enumerable.Range(2, nandAnt.Antecedent.Length)) // .1
                        .Concat(Enumerable.Repeat(-1, 2)) // |T
                        // the rightmost .3 in the starting formula is the leftmost .3 in the reduced formula
                        .Concat(Enumerable.Range(nandAnt.Length + 2, nandSub.Antecedent.Length)) // .3
                        .Append(-1) // |
                        // the leftmost .3 in the starting formula is the rightmost .3 in the reduced formula
                        .Concat(Enumerable.Range(nandAnt.Antecedent.Length + nandAntSub.Antecedent.Length + 3, nandAntSub.Subsequent.Length)) // .3
                        .Append(-1) // |
                        .Concat(Enumerable.Range(nandAnt.Length + nandSub.Antecedent.Length + 4, nandAnt.Antecedent.Length)) // .1
                        .Concat(Enumerable.Range(nandAnt.Antecedent.Length + 3, nandAntSub.Antecedent.Length)) // .2
                        .ToImmutableList();

                    result = new ReductionRecord(startingNand, reducedFormula, "||.1|.2.3|.3|T.1 => ||.1|T.3|.3|.1.2", mapping);
                    return true;
                }
            }
        }
        *
        *
        **/

        return null;
    }
}