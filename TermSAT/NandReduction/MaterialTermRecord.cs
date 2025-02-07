namespace TermSAT.NandReduction;

/// <summary>
/// Records the discovery of a 'material term' in a formula.
/// Essentially, the material term table in the RR DB memoizes the result of doing 'relevance analysis'.
/// 
/// A statement that about what terms in a formula can be used to *compel* the formula to have a certain value...  
/// ```Assigning {termValue} to {term} compels {formula} to have the value {formulaValue}```
/// put another way...
/// ```Replacing {term} with a constant {termValue} causes {formula} to reduce to a constant {formulaValue}```
/// 
/// </summary>
public class MaterialTermRecord
{
    public MaterialTermRecord(long termId, bool termValue, long formulaId, bool formulaValue)
    {
        FormulaId = formulaId;
        TermId = termId;
        TermValue = termValue;
        FormulaValue = formulaValue;
    }
    public MaterialTermRecord()
    {
    }
    public long FormulaId { get; init; }
    public long TermId { get; init; }
    public bool TermValue { get; init; }
    public bool FormulaValue { get; init; }
}
