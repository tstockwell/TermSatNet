using Microsoft.EntityFrameworkCore;
using TermSAT.RuleDatabase;

namespace TermSAT.NandReduction;

/// TODO: Rename to Cofactors
/// <summary>
/// 
/// A grounding is a subset of all the instances of some term in some formula such that 
/// when all the terms in the grounding are replaced by a constant,  
/// the formula reduces to a constant.  
/// 
/// Groundings are a statement about what instances of a given term in a formula that can compel the formula to reduce to T or F...  
///     > Assigning {termValue} to all the instances of {term} listed in {nameof(Positions)} 
///     > compels {formula} to have the value {formulaValue}
///     
/// Put another way... {termValue} is a {positive/negative} cofactor of formula {formula}.
/// 
/// Example: |.1|.1.2
/// Note that replacing .1 with F causes the entire formula to reduce to T
///     => |F|F.2
///     => |FT
///     => T
/// And the ground-list for that cut is...
///     WildcardRecord(.1, F, |.1|.1.2, T, [1,3]) 
/// 
/// A cutting is similar to a grounding, but a cutting only reduces a formula to another formula, not all the way to ground.  
/// 
/// ## The Special T-Grounding
/// The T terms in a formula can be groundings, they can be groundings where termValue == F.
/// T-Groundings are the basis for performing 'wildcard swapping'.  
/// 
/// Notice that groundings and cuttings are both *reductions*.  
/// In RR, a valid reduction only reduces formulas to logically equivalent formulas.
/// 
/// Notice that RR's concepts of grounding and cutting only cover a single sub-term.  
/// That is, all the terms in a groundings or cutting are just different instances of a single sub-term.  
/// But reductions could cover two variables, and three variables, and so forth.  
/// RR calls these 'higher-order' reductions.  
/// 
/// Groundings are a super useful tool for reducing proposition formulas.  
/// Groundings are used by RR to identify opportunities for cuts and branches.  
/// The RR reduction algorithm is a kind of dynamic programming algorithm that reduces formulas by repeatedly 
/// calculating, and remembering, groundings and using them to discover new cuts. 
/// Those new cuts produce new groundings, and so on.  
/// 
/// The design of the RR algorithm starts with assuming that, given a nand formula, 
/// that the left and right sides have already been reduced to their canonical form.
/// Therefore the RR algorithm always starts with a formula that is 'mostly canonical', 
/// where the left and right sides are both canonical but the parent nand formula is not.
/// The RR reduction algorithm is a kind of dynamic programming algorithm that reduces formulas by repeatedly...
///     - use sub-term groundings to calculate all possible groundings for the parent formula.
///     - use new groundings to discover and apply cuts, thus discovering, and remembering, the reduction.
///         > I'm pretty sure that I can prove that only one cut is required.
///         > Anyway, I'm also pretty that I could prove that if more than one cut is required then its 
///         > only because a sub-term wasn't reduced to ground, and thus RR is not complete.
///         > So, this step should really be one and done.
///         > If not then I won't have confidence that RR is complete.
///         
/// The RR algorithm can be shown to be complete by induction of the length of the formula... 
/// Assuming that RR can reduce all the sub-terms in a formula (because they're shorter) then 
/// if no cut can be discovered in the formula then the formula 
///     
/// </summary>
public class GroundingRecord
{
    public static void OnModelCreating(ModelBuilder modelBuilder, string tableName)
    {
        modelBuilder.Entity<GroundingRecord>(f => f.ToTable(tableName));

        modelBuilder.Entity<GroundingRecord>().HasKey(f => new { f.FormulaId, f.FormulaValue, f.TermId, f.TermValue, f.Positions });

        modelBuilder.Entity<GroundingRecord>().Property(f => f.FormulaId).IsRequired();
        modelBuilder.Entity<GroundingRecord>().Property(f => f.FormulaValue).IsRequired();
        modelBuilder.Entity<GroundingRecord>().Property(f => f.TermId).IsRequired();
        modelBuilder.Entity<GroundingRecord>().Property(f => f.TermValue).IsRequired();
        modelBuilder.Entity<GroundingRecord>().Property(f => f.Positions).IsRequired();

        modelBuilder.Entity<GroundingRecord>().HasIndex(_ => new { _.FormulaId, _.FormulaValue, _.TermId}); 
    }

    public GroundingRecord(long termId, bool termValue, long formulaId, bool formulaValue, int[] positions)
    {
        FormulaId = formulaId;
        TermId = termId;
        TermValue = termValue;
        FormulaValue = formulaValue;
        Positions=positions;
    }
    public GroundingRecord()
    {
    }
    public long FormulaId { get; init; }
    public long TermId { get; init; }
    public bool TermValue { get; init; }
    public bool FormulaValue { get; init; }

    public int[] Positions {  get; init; }
}
