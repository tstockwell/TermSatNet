using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
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

        // todo: We should be looking up children by Ids instead, because EF caches those client-side,  
        // and using the Text column will always result in a query to the server.  
        var lhsRecord = await db.Expressions.Where(_ => _.Text == startingNand.Antecedent.Text).FirstAsync();
        var rhsRecord = await db.Expressions.Where(_ => _.Text == startingNand.Subsequent.Text).FirstAsync();

        var trueId = await db.GetConstantExpressionIdAsync(true);
        var falseId = await db.GetConstantExpressionIdAsync(false);

        // This section does simple deiteration.  
        //
        // The code below attempts to reduce the lhs before the rhs.
        // And it attempts plain deiteration before paste-and-cut.
        // For any f-grounding cofactor on one side, called the *domininate* side  
        //  If the dominate term exists in the other side, called the *subjugate*
        //  Then deiterate the term from the subjugate.
        {
            var rFgCofactors = await db.Expressions.Where(e => e.Id == rhsRecord.Id || e.NextReductionId == rhsRecord.Id || e.CanonicalId == rhsRecord.Id)
                .Join(db.Cofactors, _ => _.Id, _ => _.ExpressionId, (e, c) => c)
                .Where(_ => _.ConclusionId == falseId)
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

                    startingRecord.RuleDescriptor = $"deiteration: LHS[{subtermRecord.Formula}<-{replacement}] =>* {deiteratedLHS}";
                    startingRecord.NextReductionId =  reducedR.Id;

                    await db.SaveChangesAsync();

                    return reducedR;
                }
            }

            var lFgCofactors = await db.Expressions.Where(e => e.Id == lhsRecord.Id || e.NextReductionId == lhsRecord.Id || e.CanonicalId == lhsRecord.Id)
                .Join(db.Cofactors, _ => _.Id, _ => _.ExpressionId, (e, c) => c)
                .Where(_ => _.ConclusionId == falseId)
                .ToArrayAsync();

            foreach (var cofactor in lFgCofactors)
            {
                var subtermRecord = await db.Expressions.FindAsync(cofactor.SubtermId);
                var replacement = (cofactor.ReplacementId == falseId) ? Constant.TRUE : Constant.FALSE;
                var deiteratedRHS = startingNand.Subsequent.ReplaceAll(subtermRecord.Formula, replacement);
                if (deiteratedRHS.CompareTo(startingNand.Subsequent) < 0)
                {
                    var reducedE = Nand.NewNand(startingNand.Antecedent, deiteratedRHS);
                    var reducedR = await db.GetMostlyCanonicalRecordAsync(reducedE);

                    startingRecord.RuleDescriptor = $"deiteration: RHS[{subtermRecord.Formula}<-{replacement}] =>* {deiteratedRHS}";
                    startingRecord.NextReductionId =  reducedR.Id;
                    await db.SaveChangesAsync();

                    return reducedR;
                }
            }
        }

        // -- paste-and-cut (aka term-swapping, cofactor-swapping, wildcard-swapping)
        // Paste and cut is the preferred name since paste-and-cut is really a shortcut for TWO combined operations, iteration followed by deiteration.  
        // The reason paste-and-cut is combined into two steps is that the iteration step alone would *expand* expressions instead of *reduce* them.  
        // But, when followed by a deiteration of the 'source' terms, the two combined operation form a reduction.
        // The important thing to know is that paste-and-cut is really just iteration followed by deiteration
        //
        // The basic rule for paste-and-cut is...  
        //  Iteration: Given a subterm S of an expression E of the form (L R),  
        //	        where S is a left or right, F-grounding cofactor of E where S.R is T or F, 
        //	        then any or all copies of S.R in the other side of the expression may be replaced with S.
        //  Let E be a mostly-canonical expression of the form (L R) (both L and R are canonical but E is not).
        //  Let E! be a set that contains E and every possible immediate iteration of E.
        //  If there exists expressions E1 and E2 in E! such that 
        //      E1[A<-F] =>* X and
        //      E2[B<-F] =>* X
        //  Then
        //      The expression E[A<->B] is equivalent to E.
        //  That is, we can swap A with B without changing E's truth function.
        //  In certain cases the result is a reduced version of E.
        {
            var allCutPasteCofactors = 
                await db.Cofactors.Where(_ => _.ExpressionId == startingRecord.Id)
                .Join(db.Cofactors, _ => _.ExpressionId, _ => _.ExpressionId, (l,r) => new {Cofactor1=l, Cofactor2=r})
                .Where(_ => _.Cofactor1.ReplacementId == falseId 
                    && _.Cofactor2.ReplacementId == falseId
                    && _.Cofactor1.ConclusionId == _.Cofactor2.ConclusionId
                    && _.Cofactor1.SubtermId != _.Cofactor2.SubtermId)
                .ToArrayAsync();
            foreach (var cutPasteCofactors in allCutPasteCofactors)
            {
                var subterm1Record = await db.Expressions.FindAsync(cutPasteCofactors.Cofactor1.SubtermId);
                var subterm2Record = await db.Expressions.FindAsync(cutPasteCofactors.Cofactor2.SubtermId);

                // terms must be 'independent', where the definition 'independent' is likely to change
                if (!subterm1Record.Formula.Contains(subterm2Record.Formula) && !subterm1Record.Formula.Contains(subterm2Record.Formula))
                {
                    var reducedE = startingNand;
                    //if (lhsRecord.Formula.Contains(subterm1Record.Formula) && rhsRecord.Formula.Contains(subterm2Record.Formula))
                    //{  // term1 == lhs, term2 == rhs
                        var swappedLHS = lhsRecord.Formula.ReplaceAll(subterm1Record.Formula, subterm2Record.Formula);

                        var rhsSwapCofactors = db.Cofactors
                            .Where(_ => _.ExpressionId == startingRecord.Id
                                && _.SubtermId == cutPasteCofactors.Cofactor2.SubtermId 
                                && _.ConclusionId == cutPasteCofactors.Cofactor2.ConclusionId
                                && _.ReplacementId == cutPasteCofactors.Cofactor2.ReplacementId)
                            .ToArray();
                        var swappedRHS = rhsRecord.Formula;
                        foreach (var swapCofactor in rhsSwapCofactors)
                        {
                            var subtermRecord = await db.Expressions.FindAsync(swapCofactor.SubtermId);
                            swappedRHS = swappedRHS.ReplaceAll(subtermRecord.Formula, subterm1Record.Formula);
                        }

                        reducedE = Nand.NewNand(swappedLHS, swappedRHS);
                    //}


                    if (reducedE.CompareTo(startingNand) < 0)
                    {
                        var reducedR = await db.GetMostlyCanonicalRecordAsync(reducedE);
                        startingRecord.RuleDescriptor = $"paste-and-cut: {subterm1Record} <-> {subterm2Record}";
                        startingRecord.NextReductionId =  reducedR.Id;
                        await db.SaveChangesAsync();

                        return reducedR;
                    }
                }
            }
        }


        // the given expression cannot be reduced and is therefore canonical
        return null;
    }


    /// <summary>
    /// 
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
    /// <param name="startingRecord">Should be mostly canonical</param>
    public static async Task AddCofactors(this LucidDbContext db, ReductionRecord startingRecord)
    {
        var trueId = await db.GetConstantExpressionIdAsync(true);
        var falseId = await db.GetConstantExpressionIdAsync(false);

        // symmetries can cause this process to generate duplicate cofactors.  
        // So, all the new cofactors are first added to this container
        // and then all items in this container are added to the db context
        var newRecords = new HashSet<CofactorRecord>();
        var addNewCofactorRecord = (CofactorRecord cofactorRecord) =>
        {
                newRecords.Add(cofactorRecord);
#if DEBUG
                CheckConsistency(newRecords);
#endif
        };

        // Every expression is a tgt-cofactor/fgf-cofactor of itself.  
        // Including constants.  
        // A cofactor of a constant is not an intuitive concept, since constants cant be assigned values.  
        // But if you think about a T as an empty space in an existential graph and F as an empty cut
        // then it makes more sense, replacing constants are like filling empty spaces in a graph.
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
            // Since startingRecord is mostly canonical, left and right expressions should be canonical
            var leftRecord = await db.GetMostlyCanonicalRecordAsync(startingNand.Antecedent);
            Debug.Assert(leftRecord.IsCanonical, "Since startingRecord is mostly canonical, left and right expressions should be canonical");
            var rightRecord = await db.GetMostlyCanonicalRecordAsync(startingNand.Subsequent);
            Debug.Assert(rightRecord.IsCanonical, "Since startingRecord is mostly canonical, left and right expressions should be canonical");

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
                                        conclusionId: canonicalConclusionRecord.Id);

                                    addNewCofactorRecord(cofactorRecord);
                                }
                            }
                        }
                    }
                }
            }


            // Every left f-grounding cofactor is a t-grounding cofactor of given expression.
            // This includes the lhs itself.  
            var leftFGroundings = await db.Expressions.Where(e => e.Id == leftRecord.Id)
                .Join(db.Cofactors, _ => _.Id, _ => _.ExpressionId, (e, c) => c )
                .Where(_ => _.ConclusionId == falseId)
                .ToArrayAsync();
            foreach (var leftFGrounding in leftFGroundings)
            {
                if (leftFGrounding.SubtermId != leftFGrounding.ReplacementId)
                {
                    var cofactorRecord = new CofactorRecord(
                        expressionId: startingRecord.Id,
                        subtermId: leftFGrounding.SubtermId,
                        replacementId: leftFGrounding.ReplacementId,
                        conclusionId: trueId);
                    addNewCofactorRecord(cofactorRecord);
                }
            }


            // Similarly, every right f-grounding cofactor is a t-grounding cofactor of given expression.
            // This includes the rhs itself.  
            var rightFGroundings = await db.Expressions.Where(e => e.Id == rightRecord.Id)
                .Join(db.Cofactors, _ => _.Id, _ => _.ExpressionId, (e, c) => c)
                .Where(_ => _.ConclusionId == falseId)
                .ToArrayAsync();
            foreach (var rightFGrounding in rightFGroundings)
            {
                if (rightFGrounding.SubtermId != rightFGrounding.ReplacementId)
                {
                    var cofactorRecord = new CofactorRecord(
                        expressionId: startingRecord.Id,
                        subtermId: rightFGrounding.SubtermId,
                        replacementId: rightFGrounding.ReplacementId,
                        conclusionId: trueId);
                    addNewCofactorRecord(cofactorRecord);
                }
            }


            // then add grounding to F when both sides are forced to true for the same subterm and replacement
            // NOTE: this section could just be deleted were it not for sub-cofactors with 'unified' subterms
            var leftTGroundings = await db.Expressions
                .Where(e => e.Id == leftRecord.Id || e.NextReductionId == leftRecord.Id || e.CanonicalId == leftRecord.Id)
                .Where(e => e.Id != trueId && e.Id != falseId)
                .Join(db.Cofactors, _ => _.Id, _ => _.ExpressionId, (e, c) => c)
                .Where(_ => _.SubtermId != trueId)
                .Where(_ => _.ConclusionId == trueId)
                .ToArrayAsync();
            var rightTGroundings = await db.Expressions
                .Where(e => e.Id == rightRecord.Id || e.NextReductionId == rightRecord.Id || e.CanonicalId == rightRecord.Id)
                .Where(e => e.Id != trueId && e.Id != falseId)
                .Join(db.Cofactors, _ => _.Id, _ => _.ExpressionId, (e, c) => c)
                .Where(_ => _.SubtermId != trueId)
                .Where(_ => _.ConclusionId == trueId)
                .ToArrayAsync();
            foreach (var leftTGrounding in leftTGroundings)
            {
                foreach (var rightTGrounding in rightTGroundings)
                {
                    if (leftTGrounding.ReplacementId == rightTGrounding.ReplacementId
                        && leftTGrounding.SubtermId == rightTGrounding.SubtermId)
                    {
                        CofactorRecord cofactorRecord = new(
                            expressionId: startingRecord.Id,
                            conclusionId: falseId,
                            subtermId: leftTGrounding.SubtermId,
                            replacementId: leftTGrounding.ReplacementId);
                        addNewCofactorRecord(cofactorRecord);
                    }
                }
            }


            // Also... compute unified cofactors 
            // Since both sides of an expression E are tgf-cofactors of E, 
            // the iterated version of the other side is also a tgf-cofactor of E.  
            // Example: Given (1 (T 2)),
            //      since 1 is a tgf-cofactor,  
            //		then (1 2), the iterated version of (T 2), is also a tfg-cofactor of (1 (1 2)).  
            // Insight...
            // By computing the iteration of a side we identify an equivalence
            // between the starting expression and an expanded form of the starting equation.
            // This is the opposite of what happens when minimizing an expression, where, 
            // by computing the iteration of a side we identify an equivalence
            // between the starting expression and a minimized form of the starting equation.
            {
                if (leftRecord.Id != trueId && leftRecord.Id != falseId && rightRecord.Id != falseId)
                {
                    var iteratedRhs = rightRecord.Formula.ReplaceAll(Constant.TRUE, leftRecord.Formula);
                    var iteratedRhsRecord = await db.GetMostlyCanonicalRecordAsync(iteratedRhs);
                    if (iteratedRhs == iteratedRhsRecord.Formula)
                    {
                        var iteratedExpression = Nand.NewNand(leftRecord.Formula, iteratedRhsRecord.Formula);
                        var iteratedExpressionRecord = await db.GetMostlyCanonicalRecordAsync(iteratedExpression);

                        if (iteratedExpressionRecord.Id != startingRecord.Id
                            && iteratedExpressionRecord.Id != trueId)
                        {
                            if (0 < startingRecord.CanonicalId)
                            {
                                iteratedExpressionRecord.CanonicalId = startingRecord.CanonicalId;
                            }
                            if (iteratedExpressionRecord.NextReductionId <= 0 && !iteratedExpressionRecord.IsCanonical)
                            {
                                iteratedExpressionRecord.NextReductionId = startingRecord.Id;
                                iteratedExpressionRecord.RuleDescriptor = $"deiteration: RHS[{leftRecord.Formula}<-T] =>* {startingNand}";
                            }
                        }
                    }
                }
            }
            {
                if (rightRecord.Id != trueId && rightRecord.Id != falseId && leftRecord.Id != falseId)
                {
                    var iteratedLhs = leftRecord.Formula.ReplaceAll(Constant.TRUE, rightRecord.Formula);
                    var iteratedLhsRecord = await db.GetMostlyCanonicalRecordAsync(iteratedLhs);
                    if (iteratedLhs == iteratedLhsRecord.Formula)
                    {
                        var iteratedExpression = Nand.NewNand(iteratedLhsRecord.Formula, rightRecord.Formula);
                        var iteratedExpressionRecord = await db.GetMostlyCanonicalRecordAsync(iteratedExpression);

                        if (iteratedExpressionRecord.Id != startingRecord.Id
                            && iteratedExpressionRecord.Id != trueId)
                        {
                            if (0 < startingRecord.CanonicalId)
                            {
                                iteratedExpressionRecord.CanonicalId = startingRecord.CanonicalId;
                            }
                            if (iteratedExpressionRecord.NextReductionId <= 0 && !iteratedExpressionRecord.IsCanonical)
                            {
                                iteratedExpressionRecord.NextReductionId = startingRecord.Id;
                                iteratedExpressionRecord.RuleDescriptor = $"deiteration: LHS[{rightRecord.Formula}<-T] =>* {startingNand}";
                            }
                        }
                    }
                }
            }
        }

        var recordsToSave = new List<CofactorRecord>();
        foreach (var cofactorRecord in newRecords)
        {
            // Must match modelBuilder.Entity<CofactorRecord>().HasKey(_ => new { _.ExpressionId, _.ConclusionId, _.ReplacementId, _.SubtermId });
            var existingCofactor = await db.Cofactors.FindAsync(
                cofactorRecord.ExpressionId,
                cofactorRecord.ConclusionId,
                cofactorRecord.ReplacementId,
                cofactorRecord.SubtermId);

            if (existingCofactor == null)
            {
                await db.Cofactors.AddAsync(cofactorRecord);
            }
        }

        await db.SaveChangesAsync();
    }

    // for a given subterm and replacement, all conclusions should be the same
    static private void CheckConsistency(HashSet<CofactorRecord> newRecords)
    {
        foreach (var cofactor in newRecords)
        {
            var nonMatch = newRecords
                .Where(_ => _.SubtermId == cofactor.SubtermId
                        && _.ReplacementId == cofactor.ReplacementId
                        && _.ConclusionId != cofactor.ConclusionId)
                .ToArray();
            if (nonMatch.Any())
            {
                //throw new TermSatException("cofactor conclusions not consistent.");
            }
        }
    }



    public static async Task<IQueryable<CofactorRecord>> GetFGroundingFCofactorsAsync(this LucidDbContext ctx, long expressionId)
    {
        var falseId = await ctx.GetConstantExpressionIdAsync(false);
        return ctx.Cofactors.Where(_ => _.ExpressionId == expressionId && _.ReplacementId == falseId && _.ConclusionId == falseId);
    }}