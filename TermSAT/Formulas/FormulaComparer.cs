using System.Collections.Generic;

namespace TermSAT.Formulas;

public class FormulaComparer : IComparer<Nand>
{
    public int Compare(Nand x, Nand y)
    {
        return x.CompareTo(y);
    }
}
