using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction;

public static class CofactorReduction
{

    /// <summary>
    /// 
    /// This method implements the lucid expression reduction method described in the wiki... [reduction.md](reduction.md).  
    /// 
    /// </summary>
    public static async Task<ReductionRecord> TryGetCofactorReductionAsync(this LucidDbContext db, ReductionRecord startingRecord)
    {
        // if given formula is not a nand then it must be a variable or constant and is not reducible.
        if (!(startingRecord.Formula is Nand startingNand))
        {
            return null;
        }

        // todo: We should prolly use Ids instead, because EF caches those client-side.  
        // todo: While using the Text column will always result in a query to the server.  
        var lhsRecord = await db.Expressions.Where(_ => _.Text == startingNand.Antecedent.Text).FirstAsync();
        var rhsRecord = await db.Expressions.Where(_ => _.Text == startingNand.Subsequent.Text).FirstAsync();

        var trueId = await db.GetConstantExpressionIdAsync(true);
        var falseId = await db.GetConstantExpressionIdAsync(false);

        // This section does simple deiteration.  
        // This section was written long before the other sections
        // and the following section that does 'paste-and-cut' also does deiteration.  
        // So, it's possible that this section can just be removed.
        //
        // The code below always attempts to reduce the lhs before the rhs.
        // And it always attempts plain deiteration before paste-and-cut.
        // for any f-grounding cofactor on one side, called the *domininate* side  
        //  if the dominate term exists in the other side, called the *subjugate*
        //  then deiterate the term from the subjugate.
        {
            var rFgCofactors = await db.Cofactors
                .Where(_ => _.ExpressionId == rhsRecord.Id && _.ConclusionId == falseId)
                .ToArrayAsync();

            var lFgCofactors = await db.Cofactors
                .Where(_ => _.ExpressionId == lhsRecord.Id && _.ConclusionId == falseId)
                .ToArrayAsync();

            foreach (var cofactor in rFgCofactors)
            {
                var subtermRecord = await db.Expressions.FindAsync(cofactor.SubtermId);
                var replacement = (cofactor.ReplacementId == falseId) ? Constant.TRUE : Constant.FALSE;
                var deiteratedLHS = startingNand.Antecedent.ReplaceAll(subtermRecord.Formula, replacement);
                if (deiteratedLHS.CompareTo(startingNand.Antecedent) < 0)
                {
                    var reducedE = Nand.NewNand(deiteratedLHS, startingNand.Subsequent);
                    var reducedR = await db.GetMostlyCanonicalRecordAsync(reducedE);
                    if (reducedR.Formula.CompareTo(startingNand) < 0)
                    {
                        startingRecord.RuleDescriptor = $"deiteration: LHS[{subtermRecord.Formula}<-{replacement}] =>* {deiteratedLHS}";
                        startingRecord.NextReductionId =  reducedR.Id;
                        await db.SaveChangesAsync();

                        return reducedR;
                    }
                }
            }

            foreach (var cofactor in lFgCofactors)
            {
                var subtermRecord = await db.Expressions.FindAsync(cofactor.SubtermId);
                var replacement = (cofactor.ReplacementId == falseId) ? Constant.TRUE : Constant.FALSE;
                var deiteratedRHS = startingNand.Subsequent.ReplaceAll(subtermRecord.Formula, replacement);
                if (deiteratedRHS.CompareTo(startingNand.Subsequent) < 0)
                {
                    var reducedE = Nand.NewNand(startingNand.Antecedent, deiteratedRHS);
                    if (reducedE.CompareTo(startingNand) < 0)
                    {
                        var reducedR = await db.GetMostlyCanonicalRecordAsync(reducedE);
                        startingRecord.RuleDescriptor = $"deiteration: RHS[{subtermRecord.Formula}<-{replacement}] =>* {deiteratedRHS}";
                        startingRecord.NextReductionId =  reducedR.Id;
                        await db.SaveChangesAsync();
                        return reducedR;
                    }
                }
            }
        }

        // -- paste-and-cut (aka term-swapping, cofactor-swapping, wildcard-swapping)
        // Paste and cut is the preferred name since paste-and-cut is really a shortcut for TWO combined operations, iteration followed by deiteration.  
        // The reason paste-and-cut is combined into two steps is that the iteration step alone would *expand* expressions instead of *reduce* them.  
        // But, when followed by a deiteration of the 'source' terms, the two combined operation form a reduction.
        // The important thing to know is that cat-and-paste is really just iteration and deiteration
        //
        // The basic rule for cut-and-paste is...  
        //  If
        //      E[A<-F] =>* X and
        //      E[B<-F] =>* X
        //      where E is a mostly canonical expression (both the left and right sides are canonical but E is not)
        //  Then
        //      The expression E[A<->B] is equivalent to E.
        //      That is, we can swap A with B without changing E's truth function.
        //
        //  Also, we can constrain this rule to only instances of E where A and B exist in different sides of an expression.  
        //  Because if A and B were on the same side of E then that side would be reducible, 
        //  which they can't because E is mostly canonical.  
        {
            var allCutPasteCofactors = await db.Cofactors
                .Where(_ => _.ExpressionId == startingRecord.Id)
                .Join(db.Cofactors, _ => _.ExpressionId, _ => _.ExpressionId, (l,r) => new {Cofactor1=l, Cofactor2=r})
                .Where(_ => _.Cofactor1.ReplacementId == falseId && _.Cofactor2.ReplacementId == falseId
                    && _.Cofactor1.ConclusionId == _.Cofactor2.ConclusionId
                    && _.Cofactor1.SubtermId != _.Cofactor2.SubtermId
                    && _.Cofactor1.UnifiedSubtermId != _.Cofactor2.UnifiedSubtermId)
                .ToArrayAsync();
            foreach (var cutPasteCofactors in allCutPasteCofactors)
            {
                var unifiedSubterm1Record = await db.Expressions.FindAsync(cutPasteCofactors.Cofactor1.UnifiedSubtermId);
                var unifiedSubterm2Record = await db.Expressions.FindAsync(cutPasteCofactors.Cofactor2.UnifiedSubtermId);

                // terms must be 'independent', where the definition 'independent' is likely to change
                if (!unifiedSubterm1Record.Formula.Contains(unifiedSubterm2Record.Formula) && !unifiedSubterm2Record.Formula.Contains(unifiedSubterm1Record.Formula))
                {
                    var subterm1Record = await db.Expressions.FindAsync(cutPasteCofactors.Cofactor1.SubtermId);
                    var subterm2Record = await db.Expressions.FindAsync(cutPasteCofactors.Cofactor2.SubtermId);

                    var reducedE = startingNand;
                    if (lhsRecord.Formula.Contains(subterm1Record.Formula) && rhsRecord.Formula.Contains(subterm2Record.Formula))
                    {  // term1 == lhs, term2 == rhs
                        var swappedLHS = lhsRecord.Formula.ReplaceAll(subterm1Record.Formula, unifiedSubterm2Record.Formula);

                        var rhsSwapCofactors = db.Cofactors
                            .Where(_ => _.ExpressionId == startingRecord.Id
                                && _.UnifiedSubtermId == cutPasteCofactors.Cofactor2.UnifiedSubtermId 
                                && _.ConclusionId == cutPasteCofactors.Cofactor2.ConclusionId
                                && _.ReplacementId == cutPasteCofactors.Cofactor2.ReplacementId)
                            .ToArray();
                        var swappedRHS = rhsRecord.Formula;
                        foreach (var swapCofactor in rhsSwapCofactors)
                        {
                            var subtermRecord = await db.Expressions.FindAsync(swapCofactor.SubtermId);
                            swappedRHS = swappedRHS.ReplaceAll(subtermRecord.Formula, unifiedSubterm1Record.Formula);
                        }

                        reducedE = Nand.NewNand(swappedLHS, swappedRHS);
                    }


                    if (reducedE.CompareTo(startingNand) < 0)
                    {
                        var reducedR = await db.GetMostlyCanonicalRecordAsync(reducedE);
                        startingRecord.RuleDescriptor = $"swap: {unifiedSubterm1Record} <-> {unifiedSubterm2Record}";
                        startingRecord.NextReductionId =  reducedR.Id;
                        await db.SaveChangesAsync();

                        return reducedR;
                    }
                }
            }
        }

        // -- paste-and-cut --
        // If we have the following cofactors (where A exists in RHS and X,C exist in LHS)...  
        //  RHS[A<-F] =>* X     
        //  LHS[X<-T] =>* Y
        //  LHS[C<-F] =>* Y     
        // then we know that we can replace C with T without changing LHS's truth function because
        // when A == F then LHS == Y and the value of C is irrelevant, and 
        // when C == F then RHS == Y and the value of A is irrelevant.
        //
        // Note that if X == F of Y == F then each of the above cofactors represent plain iteration/deiteration in a single step.  
        //
        // Naturally, these rules also apply in the opposite direction, thus reducing RHS instead of LHS...
        //  LHS[A<-F] =>* X     
        //  RHS[X<-T] =>* Y
        //  RHS[C<-F] =>* Y     
        //
        // This implementation of paste-and-cut reduces to LE's previous implementation when X == Y == F and A == T...  
        //  LHS[T<-F] =>* F
        //  RHS[C<-F] =>* F
        //
        //{
        //    var rCofactors = await db.Cofactors.Where(_ => _.ExpressionId == rhsRecord.Id).ToArrayAsync();
        //    var lCofactors = await db.Cofactors.Where(_ => _.ExpressionId == lhsRecord.Id).ToArrayAsync();

        //    var lRecord = lhsRecord;
        //    var rRecord = rhsRecord;

        //    foreach (var reverseDirection in new[] { true, false })
        //    {
        //        foreach (var rCofactor in rCofactors)
        //        {
        //            // Look through cofactors of RHS for opportunities to reduce LHS.
        //            // RHS[A<-F] =>* X
        //            if (rCofactor.ReplacementId == falseId)
        //            {
        //                var rSubtermRecord = await db.Expressions.FindAsync(rCofactor.SubtermId);
        //                var rUnifiedSubtermRecord = await db.Expressions.FindAsync(rCofactor.UnifiedSubtermId);

        //                // RHS[A<-F] =>* F
        //                if (rCofactor.ConclusionId == falseId)
        //                {
        //                    if (lRecord.Formula.Contains(rSubtermRecord.Formula))
        //                    {
        //                        // found opportunity to deiterate cofactor subterm
        //                        // LHS[A<-T] ; RHS[A<-F] =>* F
        //                        var deiteratedLHS = lRecord.Formula.ReplaceAll(rSubtermRecord.Formula, Constant.TRUE);
        //                        var reducedE = Nand.NewNand(deiteratedLHS, rRecord.Formula);
        //                        if (reducedE.CompareTo(startingNand) < 0)
        //                        {
        //                            var reducedR = await db.GetMostlyCanonicalRecordAsync(reducedE);
        //                            startingRecord.RuleDescriptor = $"deiteration: LHS[{rSubtermRecord.Formula}<-{Constant.TRUE}] =>* {deiteratedLHS}";
        //                            startingRecord.NextReductionId =  reducedR.Id;
        //                            await db.SaveChangesAsync();

        //                            return reducedR;
        //                        }
        //                    }

        //                    // if T is a fgf-cofactor of the lhs
        //                    if (lCofactors.Where(_ => _.SubtermId == trueId && _.ReplacementId == falseId && _.ConclusionId == falseId).Any())
        //                    {
        //                        // found opportunity to swap terms...
        //                        // LHS[T<-UnifiedSubterm]
        //                        // Foreach base subterm s in RHS; RHS = RHS[s<-T]
        //                        var iteratedLHS = lRecord.Formula.ReplaceAll(Constant.TRUE, rUnifiedSubtermRecord.Formula);

        //                        var deiteratedRHS = rRecord.Formula;
        //                        var allUnifiedCofactors = rCofactors
        //                            .Where(_ => _.UnifiedSubtermId == rUnifiedSubtermRecord.Id)
        //                            .Where(_ => _.ConclusionId == rCofactor.ConclusionId && _.ReplacementId == rCofactor.ReplacementId)
        //                            .ToArray();
        //                        foreach (var cofactor in allUnifiedCofactors)
        //                        {
        //                            var subtermRecord = await db.Expressions.FindAsync(cofactor.SubtermId);
        //                            deiteratedRHS = deiteratedRHS.ReplaceAll(subtermRecord.Formula, Constant.TRUE);
        //                        }

        //                        var reducedE = Nand.NewNand(iteratedLHS, deiteratedRHS);
        //                        if (reducedE.CompareTo(startingNand) < 0)
        //                        {
        //                            var reducedR = await db.GetMostlyCanonicalRecordAsync(reducedE);
        //                            startingRecord.RuleDescriptor = $"swap: {rUnifiedSubtermRecord.Formula} ";
        //                            startingRecord.NextReductionId =  reducedR.Id;
        //                            await db.SaveChangesAsync();

        //                            return reducedR;
        //                        }
        //                    }
        //                }

        //                // Look for resolvable cofactors in lhs...
        //                //  LHS[X<-T] =>* Y,
        //                //  LHS[C<-F] =>* Y
        //                // where X matches conclusion of right cofactor
        //                // First, look for the f-grounding cofactors, LHS[C<-F] =>* Y
        //                var lfGroundings = await db.Cofactors
        //                    .Where(_ => _.ExpressionId == lRecord.Id && _.ReplacementId == falseId)
        //                    .ToArrayAsync();
        //                foreach (var lCofactor in lCofactors)
        //                {
        //                    // LHS[C<-F] =>* Y
        //                    if (lCofactor.ReplacementId == falseId)
        //                    {
        //                        // special case when resolution is not required, LHS[C<-F] =>* F
        //                        if (rCofactor.ConclusionId == falseId && lCofactor.ConclusionId == falseId)
        //                        {
        //                            // found opportunity to swap terms
        //                            // LHS[C<-A] AND RHS[A<-C]  ; RHS[A<-F] =>* F, LHS[C<-F] =>* F
        //                            var lUnifiedSubtermRecord = await db.Expressions.FindAsync(lCofactor.UnifiedSubtermId);
        //                            var unifiedLHS = lRecord.Formula.ReplaceAll(lUnifiedSubtermRecord.Formula, rSubtermRecord.Formula);
        //                            var unifiedRHS = rRecord.Formula.ReplaceAll(rSubtermRecord.Formula, lUnifiedSubtermRecord.Formula);
        //                            var reducedE = Nand.NewNand(unifiedLHS, unifiedRHS);
        //                            if (reducedE.CompareTo(startingNand) < 0)
        //                            {
        //                                var reducedR = await db.GetMostlyCanonicalRecordAsync(reducedE);
        //                                startingRecord.RuleDescriptor = $"swap: {rSubtermRecord.Formula} <-> {lUnifiedSubtermRecord.Formula}";
        //                                startingRecord.NextReductionId =  reducedR.Id;
        //                                await db.SaveChangesAsync();

        //                                return reducedR;
        //                            }
        //                        }

        //                        // At this point we have...
        //                        //      RHS[A<-F] =>* X
        //                        //      LHS[C<-F] =>* Y
        //                        // If these also exist...
        //                        //      RHS[Y<-T] =>* X
        //                        //      LHS[X<-T] =>* Y
        //                        // Then can swap A and C, where A and C have no terms in common. 
        //                        var lSubtermRecord = await db.Expressions.FindAsync(lCofactor.SubtermId);

        //                            var lhsSwapCofactors = await db.Cofactors
        //                                .Where(_ => _.ExpressionId == lRecord.Id
        //                                    && _.UnifiedSubtermId == rCofactor.ConclusionId
        //                                    && _.ReplacementId == trueId
        //                                    && _.ConclusionId == lCofactor.ConclusionId)
        //                                .ToArrayAsync();

        //                            var rhsSwapCofactors = await db.Cofactors
        //                                .Where(_ => _.ExpressionId == rRecord.Id
        //                                    && _.UnifiedSubtermId == lCofactor.ConclusionId
        //                                    && _.ReplacementId == trueId
        //                                    && _.ConclusionId == rCofactor.ConclusionId)
        //                                .ToArrayAsync();
        //                            foreach (var lhsSwapCofactor in lhsSwapCofactors)
        //                            {
        //                                foreach (var rhsSwapCofactor in rhsSwapCofactors)
        //                                {
        //                                    // found opportunity to swap terms
        //                                    // LHS[C<-A] AND RHS[A<-C]  ; RHS[A<-F] =>* X, LHS[X<-T] =>* Y, LHS[C<-F] =>* Y 
        //                                    var unifiedLHS = lRecord.Formula.ReplaceAll(lSubtermRecord.Formula, rSubtermRecord.Formula);
        //                                    var unifiedRHS = rRecord.Formula.ReplaceAll(rSubtermRecord.Formula, lSubtermRecord.Formula);
        //                                    var reducedE = Nand.NewNand(unifiedLHS, unifiedRHS);
        //                                    if (reducedE.CompareTo(startingNand) < 0)
        //                                    {
        //                                        var reducedR = await db.GetMostlyCanonicalRecordAsync(reducedE);
        //                                        startingRecord.RuleDescriptor = $"swap: {rSubtermRecord.Formula} <-> {lSubtermRecord.Formula}";
        //                                        startingRecord.NextReductionId =  reducedR.Id;
        //                                        await db.SaveChangesAsync();

        //                                        return reducedR;
        //                                    }

        //                                }

        //                                // We failed to find a rhs 'swap' cofactor that matches lhs swap cofactor.  
        //                                // So, instead of looking for a match to lhsSwapCofactor...
        //                                //      LHS[X<-T] =>* Y,
        //                                // ...of the form...
        //                                //      RHS[Y<-T] =>* X
        //                                // ...we'll look for any S(ubterm) and R(eplacement) such that
        //                                //      Y[S->R] => T 
        //                                var rhsSubtermCofactors = await db.Cofactors
        //                                    .Where(_ => _.ExpressionId == lCofactor.ConclusionId    
        //                                        && _.ConclusionId == trueId)
        //                                    .ToArrayAsync();
        //                                foreach (var rhsSubtermCofactor in rhsSubtermCofactors)
        //                                {
        //                                    // found opportunity to swap terms
        //                                    var unifiedLHS = lRecord.Formula.ReplaceAll(lSubtermRecord.Formula, rSubtermRecord.Formula);
        //                                    var unifiedRHS = rRecord.Formula.ReplaceAll(rSubtermRecord.Formula, lSubtermRecord.Formula);
        //                                    var reducedE = Nand.NewNand(unifiedLHS, unifiedRHS);
        //                                    if (reducedE.CompareTo(startingNand) < 0)
        //                                    {
        //                                        var reducedR = await db.GetMostlyCanonicalRecordAsync(reducedE);
        //                                        startingRecord.RuleDescriptor = $"swap: {rSubtermRecord.Formula} <-> {lSubtermRecord.Formula}";
        //                                        startingRecord.NextReductionId =  reducedR.Id;
        //                                        await db.SaveChangesAsync();

        //                                        return reducedR;
        //                                    }
        //                                }

        //                            }
        //                    }
        //                }
        //            }
        //        }

        //        if (reverseDirection)
        //        {
        //            var m= rCofactors;
        //            rCofactors = lCofactors;
        //            lCofactors = m;

        //            lRecord = rhsRecord;
        //            rRecord = lhsRecord;
        //        }
        //    }
            
        //}

        // the given expression cannot be reduced and is therefore canonical
        return null;
    }


    /// <summary>
    /// There is pseudo code for this method in the wiki, [le-system-pseudo.md](le-system-pseudo.md).
    /// 
    /// Cofactors are added to the COFACTORS table whenever an expression is added the the EXPRESSIONS table.  
    /// 
    /// Cofactors will be examined for each and every expression that's added to the EXPRESSIONS table 
    /// during the reduction of an expression (assuming the database is empty when the reduction process is started), 
    /// because each and every expression that's added will be used to build a more complex expression.  
    /// Therefore there's not much point in creating them lazily.  
    /// Plus, adding them immediately is simpler.
    /// 
    /// </summary>
    public static async Task AddCofactors(this LucidDbContext db, ReductionRecord startingRecord)
    {
        var trueId = await db.GetConstantExpressionIdAsync(true);
        var falseId = await db.GetConstantExpressionIdAsync(false);

        // symmetries can cause this process to generate duplicate cofactors.  
        // So, all the new cofactors are first added to this container
        // and then all items in this container are added to the db context
        var newRecords = new HashSet<CofactorRecord>();

        // Every expression is a tgt-cofactor/fgf-cofactor of itself.  
        // Including constants.  
        // A cofactor of a constant is not an intuitive concept, since constants can be assigned values truth values.  
        // But if you think about a T as an empty space in an existential graph and F as an empty cut
        // then them with a value makes more sense.  
        newRecords.Add(
            new CofactorRecord(
                expressionId: startingRecord.Id,
                subtermId: startingRecord.Id,
                replacementId: trueId,
                conclusionId: trueId));
        newRecords.Add(
            new CofactorRecord(
                expressionId: startingRecord.Id,
                subtermId: startingRecord.Id,
                replacementId: falseId, 
                conclusionId: falseId));

        // add groundings for a nand
        var startingNand = startingRecord.Formula as Nand;
        if (startingNand != null)
        {
            var leftRecord = await db.GetMostlyCanonicalRecordAsync(startingNand.Antecedent);
            var rightRecord = await db.GetMostlyCanonicalRecordAsync(startingNand.Subsequent);

            // for all unique terms in an expression, create cofactors using T & F for Cofactor.Replacement.
            {
                var completed = new HashSet<Formula>();
                var startingFlattterm = startingNand.AsFlatTerm();
                foreach (var term in startingFlattterm)
                {
                    if (!completed.Contains(term))
                    {
                        completed.Add(term);

                        var subtermRecord = await db.Expressions
                            .Where(_ => _.Text == term.Text)
                            .FirstAsync();

                        foreach (var replacementId in new[] { trueId, falseId })
                        {
                            if (subtermRecord.Id != replacementId)
                            {
                                var replacement = replacementId == trueId ? Constant.TRUE : Constant.FALSE;
                                var conclusion = startingNand.ReplaceAll(subtermRecord.Formula, replacement);
                                var conclusionRecord = await db.GetMostlyCanonicalRecordAsync(conclusion);
                                if (startingRecord.Id != conclusionRecord.Id)
                                {
                                    var canonicalConclusionRecord = await db.GetCanonicalRecordAsync(conclusionRecord);
                                    var cofactorRecord = new CofactorRecord(
                                        expressionId: startingRecord.Id,
                                        subtermId: subtermRecord.Id,
                                        replacementId: replacementId,
                                        conclusionId: canonicalConclusionRecord.Id,
                                        unifiedSubtermId: subtermRecord.Id);
                                    newRecords.Add(cofactorRecord);
                                }
                            }
                        }
                    }
                }
            }


            // Every left f-grounding cofactor is a t-grounding cofactor of given expression.
            // This includes the lhs itself.  
            var leftFGroundings = await db.Cofactors.Where(_ => _.ExpressionId == leftRecord.Id && _.ConclusionId == falseId).ToArrayAsync();
            foreach (var leftFGrounding in leftFGroundings)
            {
                if (leftFGrounding.SubtermId != leftFGrounding.ReplacementId)
                {
                    var cofactor = new CofactorRecord(
                        expressionId: startingRecord.Id,
                        subtermId: leftFGrounding.SubtermId,
                        replacementId: leftFGrounding.ReplacementId,
                        conclusionId: trueId,
                        unifiedSubtermId: leftFGrounding.UnifiedSubtermId);
                    //Debug.Assert(newRecords.Contains(cofactor)); // proof that this whole section can just be removed
                    newRecords.Add(cofactor);
                }
            }


            // add groundings to T when rhs side is F.
            var rightFGroundings = await db.Cofactors.Where(_ => _.ExpressionId == rightRecord.Id && _.ConclusionId == falseId).ToArrayAsync();
            foreach (var rightFGrounding in rightFGroundings)
            {
                if (rightFGrounding.SubtermId != rightFGrounding.ReplacementId)
                {
                    var cofactor = new CofactorRecord(
                        expressionId: startingRecord.Id,
                        subtermId: rightFGrounding.SubtermId,
                        replacementId: rightFGrounding.ReplacementId,
                        conclusionId: trueId,
                        unifiedSubtermId: rightFGrounding.UnifiedSubtermId);
                    newRecords.Add(cofactor);
                }
            }


            // then add grounding to F when both sides are forced to true for the same subterm and replacement
            // NOTE: this section could just be deleted were it not for sub-cofactors with 'unified' subterms
            var leftTGroundings = await db.Cofactors
                .Where(_ => _.ExpressionId == leftRecord.Id && _.ConclusionId == trueId/* && _.SubtermId == _.UnifiedSubtermId*/)
                .ToArrayAsync();
            var rightTGroundings = await db.Cofactors
                .Where(_ => _.ExpressionId == rightRecord.Id && _.ConclusionId == trueId/* && _.SubtermId == _.UnifiedSubtermId*/)
                .ToArrayAsync();
            foreach (var leftTGrounding in leftTGroundings)
            {
                foreach(var rightTGrounding in rightTGroundings)
                {
                    if (leftTGrounding.ReplacementId == rightTGrounding.ReplacementId
                        && leftTGrounding.UnifiedSubtermId == rightTGrounding.UnifiedSubtermId)
                    {
                        CofactorRecord cofactor = new (
                            expressionId: startingRecord.Id,
                            conclusionId: falseId,
                            subtermId: leftTGrounding.SubtermId == leftTGrounding.UnifiedSubtermId ? 
                                            rightTGrounding.SubtermId : 
                                            leftTGrounding.SubtermId,
                            replacementId: leftTGrounding.ReplacementId,
                            unifiedSubtermId: leftTGrounding.UnifiedSubtermId);
                        newRecords.Add(cofactor);
#if DEBUG
                        CheckConsistency(newRecords);
#endif
                    }
                }
            }


            // Also... compute unified cofactors.  
            // Since both sides of an expression E are tgf-cofactors of E, 
            // the iterated version of the other side is also a tgf-cofactor of E.  
            // Example: Given (1 (T 2)),
            //      since 1 and (T 2) are tgf-cofactors,  
            //		then (1 2), the iterated version of (T 2), is also a tfg-cofactor of (1 (T 2)).  
            //      LE calls the iterated version of a cofactor's subterm the *unified* subterm.  
            {
                if (leftRecord.Id != trueId && rightRecord.Id != falseId)
                {
                    var iteratedSubterm = rightRecord.Formula.ReplaceAll(Constant.TRUE, leftRecord.Formula);
                    var iteratedRecord = await db.GetMostlyCanonicalRecordAsync(iteratedSubterm);
                    var canonicalSubterm = await db.GetCanonicalRecordAsync(iteratedRecord);
                    var cofactorRecord = new CofactorRecord(
                        expressionId: startingRecord.Id,
                        subtermId: rightRecord.Id,
                        replacementId: falseId,
                        conclusionId: trueId,
                        unifiedSubtermId: canonicalSubterm.Id);
                    newRecords.Add(cofactorRecord);
#if DEBUG
                    CheckConsistency(newRecords);
#endif

                }
            }
            {
                if (rightRecord.Id != trueId && leftRecord.Id != falseId)
                {
                    var iteratedSubterm = leftRecord.Formula.ReplaceAll(Constant.TRUE, rightRecord.Formula);
                    var iteratedRecord = await db.GetMostlyCanonicalRecordAsync(iteratedSubterm);
                    var canonicalSubterm = await db.GetCanonicalRecordAsync(iteratedRecord);
                    var cofactorRecord = new CofactorRecord(
                        expressionId: startingRecord.Id,
                        subtermId: leftRecord.Id,
                        replacementId: falseId,
                        conclusionId: trueId,
                        unifiedSubtermId: canonicalSubterm.Id);
                    newRecords.Add(cofactorRecord);
#if DEBUG
                    CheckConsistency(newRecords);
#endif
                }
            }
            //foreach (var leftFGrounding in leftFGroundings)
            //{
            //    var leftFSubtermRecord = await db.Expressions.FindAsync(leftFGrounding.SubtermId);
            //    foreach (var rightTGrounding in rightTGroundings)
            //    {
            //        var rightTSubtermRecord = await db.Expressions.FindAsync(rightTGrounding.SubtermId);
            //        var iteratedSubterm = rightTSubtermRecord.Formula.ReplaceAll(Constant.TRUE, leftFSubtermRecord.Formula);
            //        var iteratedRecord = await db.GetMostlyCanonicalRecordAsync(iteratedSubterm);
            //        var canonicalSubterm = await db.GetCanonicalRecordAsync(iteratedRecord);
            //        newRecords.Add(
            //            new CofactorRecord(
            //                expressionId: startingRecord.Id,
            //                subtermId: rightTGrounding.SubtermId,
            //                replacementId: falseId,
            //                conclusionId: trueId,
            //                unifiedSubtermId: canonicalSubterm.Id));
            //    }
            //}
            //foreach (var rightFGrounding in rightFGroundings)
            //{
            //    var rightFSubtermRecord = await db.Expressions.FindAsync(rightFGrounding.SubtermId);
            //    foreach (var leftTGrounding in leftTGroundings)
            //    {
            //        var leftTSubtermRecord = await db.Expressions.FindAsync(leftTGrounding.SubtermId);
            //        var iteratedSubterm = leftTSubtermRecord.Formula.ReplaceAll(Constant.TRUE, rightFSubtermRecord.Formula);
            //        var iteratedRecord = await db.GetMostlyCanonicalRecordAsync(iteratedSubterm);
            //        var canonicalSubterm = await db.GetCanonicalRecordAsync(iteratedRecord);
            //        newRecords.Add(
            //            new CofactorRecord(
            //                expressionId: startingRecord.Id,
            //                subtermId: leftTGrounding.SubtermId,
            //                replacementId: falseId,
            //                conclusionId: trueId,
            //                unifiedSubtermId: canonicalSubterm.Id));
            //    }
            //}
        }

        await db.Cofactors.AddRangeAsync(newRecords);

#if DEBUG
        CheckConsistency(newRecords);
#endif

        await db.SaveChangesAsync();
    }

    // for a given subterm and replacement, all conclusions should be the same
    static private void CheckConsistency(HashSet<CofactorRecord> newRecords)
    {
        foreach (var cofactor in newRecords)
        {
            var nonMatch = newRecords
                .Where(_ => _.SubtermId == cofactor.SubtermId
                        && _.UnifiedSubtermId == cofactor.UnifiedSubtermId
                        && _.ReplacementId == cofactor.ReplacementId
                        && _.ConclusionId != cofactor.ConclusionId)
                .ToArray();
            if (nonMatch.Any())
            {
                throw new TermSatException("cofactor conclusions not consistent.");
            }
        }
    }



    public static async Task<IQueryable<CofactorRecord>> GetFGroundingFCofactorsAsync(this LucidDbContext ctx, long expressionId)
    {
        var falseId = await ctx.GetConstantExpressionIdAsync(false);
        return ctx.Cofactors.Where(_ => _.ExpressionId == expressionId && _.ReplacementId == falseId && _.ConclusionId == falseId);
    }}