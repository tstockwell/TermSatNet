using Microsoft.EntityFrameworkCore;
using TermSAT.Formulas;

namespace TermSAT.NandReduction;

/// <summary>
/// Cofactors, as a concept, are documented in [lucid-expressions.md](lucid-expressions.md)
/// 
/// With regard to their implementation...
/// 
/// It's important to note that even though most cofactors are t-, f-, and grounding cofactors,  
/// where both the replacement and conclusion are constants,  
/// not all cofactors always have constants for the replacement and conclusion.  
/// So a cofactor is just a tuple of expressions, it didn't used to be that way.  
/// 
/// But that introduces a nasty side-effect.  The code will everywhere have to lookup the IDs for T and F.  
/// I'm sure that the server will optimize this but it would be better to have a client-side way to cache 
/// the values and avoid calls to the server.  
/// The current way that these calls are avoided is to put the ID for T and F in the META table.  
/// We can EF to cache this value using the Find method.  
/// So, since the META tables' primary key is a string we can lookup value like so...
/// 
///     var trueID = dbCtx.META.Find("trueID");
///     var falseID = dbCtx.META.Find("falseID");
///     
/// ...or something like that.  And EF caches it for us.  And if EF doesn't have a value then it calls for it.
///     
/// 
/// </summary>
public record CofactorRecord
{
    public static void OnModelCreating(ModelBuilder modelBuilder, string tableName)
    {
        modelBuilder.Entity<CofactorRecord>(f => f.ToTable(tableName));

        modelBuilder.Entity<CofactorRecord>().HasKey(_ => new { _.ExpressionId, _.ConclusionId, _.ReplacementId, _.SubtermId });

        modelBuilder.Entity<CofactorRecord>().Property(f => f.ExpressionId).IsRequired();
        modelBuilder.Entity<CofactorRecord>().Property(f => f.ConclusionId).IsRequired();
        modelBuilder.Entity<CofactorRecord>().Property(f => f.SubtermId).IsRequired();
        modelBuilder.Entity<CofactorRecord>().Property(f => f.ReplacementId).IsRequired();

        modelBuilder.Entity<CofactorRecord>().HasIndex(_ => new { _.ExpressionId, _.ConclusionId, _.ReplacementId, _.SubtermId}); 
    }

    public CofactorRecord(long expressionId, long subtermId, long replacementId, long conclusionId)
    {
        ExpressionId = expressionId;
        SubtermId = subtermId;
        ReplacementId = replacementId;
        ConclusionId = conclusionId;

#if DEBUG
        Validate();
#endif
    }

#if DEBUG
    private void Validate()
    { 
        if (0 >= ExpressionId) 
            throw new TermSatException("invalid cofactor");
        if (0 >= SubtermId)
            throw new TermSatException("invalid cofactor");
        if (0 >= ReplacementId)
            throw new TermSatException("invalid cofactor");
        if (0 >= ConclusionId)
            throw new TermSatException("invalid cofactor");
        if (ExpressionId == ConclusionId)
            throw new TermSatException("invalid cofactor");
        if (SubtermId == ReplacementId)
            throw new TermSatException("invalid cofactor");
    }
#endif

    /// <summary>
    /// The starting expression, 'E' in the wiki documentation.
    /// ExpressionId references a mostly-canonical expression
    /// </summary>
    public long ExpressionId { get; set; }

    /// <summary>
    /// The sub-expression to replace, 'S' in the wiki documentation
    /// </summary>
    public long SubtermId { get; set; }

    /// <summary>
    /// The replacement value.  'R' in the documentation
    /// Most of the time S is T or F but not necessarily.   
    /// </summary>
    public long ReplacementId { get; set; }

    /// <summary>
    /// The expression after replacement.  'C' in the documentation
    /// </summary>
    public long ConclusionId { get; set; }



    ///// <summary>
    ///// When this cofactor is derived via unification then this references a unified version of the subterm referenced by SubtermId.  
    ///// This version of the subterm used later to derive reductions.  
    ///// Null when not a derived cofactor.  
    ///// </summary>
    //public long UnifiedSubtermId { get; set; }

}
