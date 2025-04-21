using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    public static async Task<ReductionRecord> TryGetCofactorReductionAsync(this LucidDbContext db, ReductionRecord mostlyCanonical)
    {
        // if given formula is not a nand then it must be a variable or constant and is not reducible.
        if (!(mostlyCanonical.Formula is Nand startingNand))
        {
            return null;
        }

        // todo: We should prolly use Ids instead, because EF caches those client-side.  
        // todo: While using the Text column will always result in a query to the server.  
        var lhsRecord = await db.Expressions.Where(_ => _.Text == startingNand.Antecedent.Text).FirstAsync();
        var rhsRecord = await db.Expressions.Where(_ => _.Text == startingNand.Subsequent.Text).FirstAsync();

        var trueId = await db.GetConstantExpressionIdAsync(true);
        var falseId = await db.GetConstantExpressionIdAsync(false);

        var rFgfCofactors = await db.GetFGroundingFCofactorsAsync(rhsRecord.Id);
        var lFgfCofactors = await db.GetFGroundingFCofactorsAsync(lhsRecord.Id);

        // The code below always attempts to reduce the lhs before the rhs.
        // And it always attempts plain deiteration before paste-and-cut.

        // for any f-ground f-cofactor on one side, called the *domininate* side  
        //  if the dominate term exists in the other side, called the *subjugate* then
        //      deiterate the term from the subjugate.
        {
            foreach (var cofactor in rFgfCofactors)
            {
                var subtermRecord = await db.Expressions.FindAsync(cofactor.SubtermId);
                var deiteratedLHS = startingNand.Antecedent.ReplaceAll(subtermRecord.Formula, Constant.TRUE);
                if (deiteratedLHS.CompareTo(startingNand.Antecedent) < 0)
                {
                    var reducedE = Nand.NewNand(deiteratedLHS, startingNand.Subsequent);
                    if (reducedE.CompareTo(startingNand) < 0)
                    {
                        return new ReductionRecord(reducedE);
                    }
                }
            }

            foreach (var cofactor in lFgfCofactors)
            {
                var subtermRecord = await db.Expressions.FindAsync(cofactor.SubtermId);
                var deiteratedRHS = startingNand.Subsequent.ReplaceAll(subtermRecord.Formula, Constant.TRUE);
                if (deiteratedRHS.CompareTo(startingNand.Subsequent) < 0)
                {
                    var reducedE = Nand.NewNand(startingNand.Antecedent, deiteratedRHS);
                    if (reducedE.CompareTo(startingNand) < 0)
                    {
                        return new ReductionRecord(reducedE);
                    }
                }
            }
        }

        // -- paste-and-cut --
        // for any f-ground f-cofactor of the dominate side  
        //  if T is a fgf-cofactor of the subjugate side then 
        //      iterate the cofactor's term into the subjugate, replacing the T's, and
        //      deiterate the term from the dominate side 
        // Example: (T (1 (1 2)) (2 (1 2))) => ((1 2) ((1 T) (2 T)))
        {
            if (lFgfCofactors.Where(_ => _.SubtermId == trueId).Any())
            {
                if (startingNand.Antecedent.Contains(Constant.TRUE))
                {
                    var unifiedSubTermIds = await rFgfCofactors.Select(_ => _.UnifiedSubtermId).Distinct().ToArrayAsync();
                    foreach (var unifiedSubtermId in unifiedSubTermIds)
                    {
                        var unifiedSubtermRecord = await db.Expressions.FindAsync(unifiedSubtermId);
                            var deiteratedRHS = startingNand.Subsequent;
                            var iteratedLHS = startingNand.Antecedent.ReplaceAll(Constant.TRUE, unifiedSubtermRecord.Formula);
                        var unifiedFgfCofactors = await rFgfCofactors.Where(_ => _.UnifiedSubtermId == unifiedSubtermId).ToArrayAsync();
                            foreach (var cofactor in unifiedFgfCofactors)
                            {
                                var subtermRecord = await db.Expressions.FindAsync(cofactor.SubtermId);
                                deiteratedRHS = deiteratedRHS.ReplaceAll(subtermRecord.Formula, Constant.TRUE);
                            }
                            var reducedE = Nand.NewNand(iteratedLHS, deiteratedRHS);
                            if (reducedE.CompareTo(startingNand) < 0)
                            {
                                return new ReductionRecord(reducedE);
                            }
                    }
                }
            }

            if (rFgfCofactors.Where(_ => _.SubtermId == trueId).Any())
            {
                if (startingNand.Subsequent.Contains(Constant.TRUE))
                {
                    foreach (var cofactor in lFgfCofactors)
                    {
                        var subtermRecord = await db.Expressions.FindAsync(cofactor.SubtermId);
                        if (startingNand.Antecedent.Contains(subtermRecord.Formula))
                        {
                            var iteratedRHS = startingNand.Subsequent.ReplaceAll(Constant.TRUE, subtermRecord.Formula);
                            var deiteratedLHS = startingNand.Antecedent.ReplaceAll(subtermRecord.Formula, Constant.TRUE);
                            var reducedE = Nand.NewNand(deiteratedLHS, iteratedRHS);
                            if (reducedE.CompareTo(startingNand) < 0)
                            {
                                return new ReductionRecord(reducedE);
                            }
                        }
                    }
                }
            }
        }

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
    /// Therefore there's not much point in creating them lazily, and adding them immediately is way simpler.
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
        // If you think about a T as an empty space in an existential graph and 
        // F as an empty cut then replacing T with an F and F with T is more intuitive.  
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

        var startingNand = startingRecord.Formula as Nand;
        if (startingNand != null)
        {
            // add groundings for a nand
            // first, add groundings to T, when either side is F.
            //      and for each f-grounding of either side
            // then add grounding to F when both sides are true
            var leftRecord = await db.GetMostlyCanonicalRecordAsync(startingNand.Antecedent);
            var leftFGroundings = await db.Cofactors.Where(_ => _.ExpressionId == leftRecord.Id && _.ConclusionId == falseId).ToArrayAsync();

            // add groundings to T when lhs side is F.
            foreach (var leftFGrounding in leftFGroundings)
            {
                newRecords.Add(
                    new CofactorRecord(
                        expressionId: startingRecord.Id,
                        subtermId: leftFGrounding.SubtermId,
                        replacementId: leftFGrounding.ReplacementId,
                        conclusionId: trueId));
            }

            // the lhs is a tgf-cofactor of the starting expression
            newRecords.Add(
                new CofactorRecord(
                    expressionId: startingRecord.Id,
                    subtermId: leftRecord.Id,
                    replacementId: falseId,
                    conclusionId: trueId));

            var rightRecord = await db.GetMostlyCanonicalRecordAsync(startingNand.Subsequent);
            var rightFGroundings = await db.Cofactors.Where(_ => _.ExpressionId == rightRecord.Id && _.ConclusionId == falseId).ToArrayAsync();

            // add groundings to T when rhs side is F.
            foreach (var rightFGrounding in rightFGroundings)
            {
                newRecords.Add(
                    new CofactorRecord(
                        expressionId: startingRecord.Id,
                        subtermId: rightFGrounding.SubtermId,
                        replacementId: rightFGrounding.ReplacementId,
                        conclusionId: trueId));
            }

            // the rhs is a tgf-cofactor of the starting expression
            newRecords.Add(
                new CofactorRecord(
                    expressionId: startingRecord.Id,
                    subtermId: rightRecord.Id,
                    replacementId: falseId,
                    conclusionId: trueId));

            // then add grounding to F when both sides are true
            var leftTGroundings = await db.Cofactors
                .Where(_ => _.ExpressionId == leftRecord.Id && _.ConclusionId == trueId).ToArrayAsync();
            var rightTGroundings = await db.Cofactors
                .Where(_ => _.ExpressionId == rightRecord.Id && _.ConclusionId == trueId).ToArrayAsync();
            var commonGroundingTerms = leftTGroundings
                .Join(rightTGroundings, _ => _.UnifiedSubtermId, _ => _.UnifiedSubtermId, (l, r) => (lhs: l, rhs: r));
            foreach (var match in commonGroundingTerms)
            {
                newRecords.Add(
                    new CofactorRecord(
                        expressionId: startingRecord.Id,
                        conclusionId: falseId,
                        subtermId: match.lhs.SubtermId,
                        replacementId: match.lhs.ReplacementId,
                        unifiedSubtermId: match.lhs.UnifiedSubtermId));
                newRecords.Add(
                    new CofactorRecord(
                        expressionId: startingRecord.Id,
                        conclusionId: falseId,
                        subtermId: match.rhs.SubtermId,
                        replacementId: match.rhs.ReplacementId,
                        unifiedSubtermId: match.rhs.UnifiedSubtermId));
            }

            // Also...
            // For f-groundings of either side,  
            // the iterated version of the other side is also an f-grounding,  
            // and thus E has a t-grounding based on that derived f-grounding.  
            // Example: Given (1 (T 2)),
            //      since 1 and (T 2) are both f-groundings,  
            //		and since therefore rhs[T<-1] == (1 2) may be substituted for (T 2),  
            //		then (1 2) is also a t-grounding of (1 (T 2)) 
            foreach (var leftFGrounding in leftFGroundings)
            {
                var leftFSubtermRecord = await db.Expressions.FindAsync(leftFGrounding.SubtermId);
                foreach (var rightTGrounding in rightTGroundings)
                {
                    var rightTSubtermRecord = await db.Expressions.FindAsync(rightTGrounding.SubtermId);
                    var iteratedSubterm = rightTSubtermRecord.Formula.ReplaceAll(Constant.TRUE, leftFSubtermRecord.Formula);
                    var iteratedRecord = await db.GetMostlyCanonicalRecordAsync(iteratedSubterm);
                    var canonicalSubterm = await db.GetCanonicalRecordAsync(iteratedRecord);
                    newRecords.Add(
                        new CofactorRecord(
                            expressionId: startingRecord.Id,
                            subtermId: rightTGrounding.SubtermId,
                            replacementId: falseId,
                            conclusionId: trueId,
                            unifiedSubtermId: canonicalSubterm.Id));
                }
            }


            //// no fgf-cofactors found, attempt unification of tgf-cofactors to create an fgf-cofactor
            //{
            //    var leftCofactors = await db.Cofactors
            //        .Where(_ => _.ExpressionId == leftRecord.Id 
            //                    && _.ConclusionId == trueId 
            //                    && _.ReplacementId == falseId
            //                    && _.SubtermId != trueId && _.SubtermId != falseId)
            //        .ToArrayAsync();
            //    var rightCofactors = await db.Cofactors
            //        .Where(_ => _.ExpressionId == rightRecord.Id 
            //                    && _.ConclusionId == trueId 
            //                    && _.ReplacementId == falseId
            //                    && _.SubtermId != trueId && _.SubtermId != falseId)
            //        .ToArrayAsync();
            //    foreach (var lCofactor in leftCofactors)
            //    {
            //        var lRecord = await db.Expressions.FindAsync(lCofactor.SubtermId);
            //        foreach (var rCofactor in rightCofactors)
            //        {
            //            var rRecord = await db.Expressions.FindAsync(rCofactor.SubtermId);
            //            var substitutions = Formula.TryUnify(lRecord.Formula, rRecord.Formula);
            //            if (substitutions != null && substitutions.Any())
            //            {
            //                var unifiedExpression = startingNand.CreateSubstitutionInstance(substitutions);
            //                var commonSubterm = lRecord.Formula.CreateSubstitutionInstance(substitutions);
            //                var commonRecord = await db.GetMostlyCanonicalRecordAsync(commonSubterm);
            //                var unifiedRecord = await db.GetMostlyCanonicalRecordAsync(unifiedExpression);
            //                var cofactorRecord = new CofactorRecord()
            //                {
            //                    ExpressionId = startingRecord.Id,
            //                    ConclusionId = falseId,
            //                    SubtermId = commonRecord.Id,
            //                    ReplacementId = falseId,
            //                    UnifiedExpressionId = unifiedRecord.Id,
            //                };
            //                newRecords.Add(cofactorRecord);
            //            }
            //        }
            //    }
            //}
        }

        await db.Cofactors.AddRangeAsync(newRecords);

        await db.SaveChangesAsync();
    }



    public static IQueryable<CofactorRecord> GetCofactorsAsync(this LucidDbContext ctx, long expressionId)
        => ctx.Cofactors.Where(_ => _.ExpressionId == expressionId);
    public static async Task<IQueryable<CofactorRecord>> GetFCofactorsAsync(this LucidDbContext ctx, long expressionId)
    {
        var falseId = await ctx.GetConstantExpressionIdAsync(false);
        return ctx.Cofactors.Where(_ => _.ExpressionId == expressionId && _.ReplacementId == falseId);
    }
    public static async Task<IQueryable<CofactorRecord>> GetTCofactorsAsync(this LucidDbContext ctx, long expressionId)
    {
        var trueId = await ctx.GetConstantExpressionIdAsync(true);
        return ctx.Cofactors.Where(_ => _.ExpressionId == expressionId && _.ReplacementId == trueId);
    }
    public static async Task<IQueryable<CofactorRecord>> GetFGroundingCofactorsAsync(this LucidDbContext ctx, long expressionId)
    {
        var falseId = await ctx.GetConstantExpressionIdAsync(false);
        return ctx.Cofactors.Where(_ => _.ExpressionId == expressionId && _.ConclusionId == falseId);
    }
    public static async Task<IQueryable<CofactorRecord>> GetTGroundingCofactorsAsync(this LucidDbContext ctx, long expressionId)
    {
        var trueId = await ctx.GetConstantExpressionIdAsync(true);
        return ctx.Cofactors.Where(_ => _.ExpressionId == expressionId && _.ConclusionId == trueId);
    }
    public static async Task<IQueryable<CofactorRecord>> GetFGroundingFCofactorsAsync(this LucidDbContext ctx, long expressionId)
    {
        var falseId = await ctx.GetConstantExpressionIdAsync(false);
        return ctx.Cofactors.Where(_ => _.ExpressionId == expressionId && _.ReplacementId == falseId && _.ConclusionId == falseId);
    }
    public static async Task<IQueryable<CofactorRecord>> GetCommonFGroundingFCofactorsAsync(this LucidDbContext ctx, long lhsId, long rhsId)
    {
        var falseId = await ctx.GetConstantExpressionIdAsync(false);
        return ctx.Cofactors.Where(_ => (_.ExpressionId == lhsId || _.ExpressionId == rhsId) && _.ReplacementId == falseId && _.ConclusionId == falseId);
    }
}