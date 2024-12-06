using System.ComponentModel.DataAnnotations;
using TermSAT.Formulas;

namespace TermSAT.RuleDatabase;

public class FormulaRecord
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Text { get; set; }
    [Required]
    public int Length { get; set; }
    [Required]
    public int VarCount { get; set; }
    [Required]
    public string TruthValue { get; set; }
    [Required]
    public bool IsCanonical { get; set; }
    public bool IsCompleted { get; set; }

    public FormulaRecord(int id, Formula formula, bool isCanonical)
    {
        Id = id;
        Text = formula.ToString();
        Length = formula.Length;
        VarCount = formula.AllVariables.Count;
        TruthValue = TruthTable.NewTruthTable(formula).ToString();
        IsCanonical = isCanonical;
    }
    private FormulaRecord()
    {
    }



    /// <summary>
    /// Set to an internal scheme name: basic, ordering, distributive 
    /// </summary>
    public string IsSubsumedByScheme {  get; set; }
}

