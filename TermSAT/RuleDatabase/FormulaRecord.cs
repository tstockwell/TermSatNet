using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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


    [NotMapped]
    public Formula Formula 
    { 
        get
        {
            if (_formula == null && !string.IsNullOrWhiteSpace(Text))
            {
                _formula = Formula.Parse(Text);
            }
            return _formula;
        }
    }
    private Formula _formula {  get; set; }

    public FormulaRecord(int id, Formula formula, bool isCanonical)
    {
        Id = id;
        Text = formula.ToString();
        Length = formula.Length;
        VarCount = formula.AllVariables.Count;
        TruthValue = TruthTable.GetTruthTable(formula).ToString();
        IsCanonical = isCanonical;
        _formula = formula;
    }
    private FormulaRecord()
    {
    }



    /// <summary>
    /// Set to an internal scheme name: basic, ordering, distributive 
    /// </summary>
    public string IsSubsumedByScheme {  get; set; }
}

