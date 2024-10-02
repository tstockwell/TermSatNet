SELECT a.*
from FormulaRecords a
join (
    SELECT *
    from FormulaRecords f
    join (
        select *, count(*) as qty
        from FormulaRecords
        group by TruthValue, Length
        having count(*) > 1
    ) t on f.TruthValue = t.TruthValue and f.Length = t.Length 
    WHERE f.IsCanonical = 1
) b on a.TruthValue = b.TruthValue and a.Length = b.Length 
