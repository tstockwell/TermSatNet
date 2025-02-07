using TermSAT.Formulas;
using TermSAT.NandReduction;

namespace TermSAT.SchemeReducer;

/// <summary>
/// This class is an early implementation of what is now 'NandReducer'.
/// <see cref="NandReducer.TryGetNextReductionAsync"/>
/// There are lots of differences, starting with using just nand op instead of implication and negation, 
/// and all production rules are replaced by 'critical term discovery', unification,  and 'demorgan reduction'.
/// 
/// There are a lot of test cases that need to be ported from SchemeReducer to NandReducer before deleting this class.
/// </summary>
public static class SchemeReducer
{
    public static Formula ReduceUsingBasicScheme(this Formula formula)
    {
        throw new System.Exception($"moved to {nameof(NandReducer)}.{nameof(NandReducer.TryGetNextReductionAsync)}");
    //    if (formula is Constant)
    //    {
    //        return formula;
    //    }

    //    if (formula is Variable)
    //    {
    //        return formula;
    //    }

    //    if (formula is Negation)
    //    {
    //        Negation negation = (Negation)formula;

    //        var reducedChild = negation.Child.ReduceUsingBasicScheme();
    //        if (reducedChild == Constant.TRUE)
    //        {
    //            return Constant.FALSE;
    //        }
    //        if (reducedChild == Constant.FALSE)
    //        {
    //            return Constant.TRUE;
    //        }
    //        if (reducedChild is Negation)
    //        {
    //            return ((Negation)reducedChild).Child.ReduceUsingBasicScheme(); 
    //        }
    //        if (reducedChild != negation.Child)
    //        {
    //            return Negation.NewNegation(reducedChild).ReduceUsingBasicScheme();
    //        }

    //        return formula;
    //    }

    //    if (formula is Formulas.Nand nand)
    //    {
    //        return NandReducer.GetCanonicalRecordAsync(nand);
    //    }

    //    //if (formula is Implication)
    //    //{
    //    //    Implication implication = (Implication)formula;
    //    //    var reducedAntecent = implication.Antecedent.Reduce();
    //    //    var reducedConsequent = implication.Subsequent.Reduce();
            
    //    //    if (reducedAntecent == Constant.TRUE)
    //    //    {
    //    //        return reducedConsequent;
    //    //    }
    //    //    if (reducedConsequent == Constant.FALSE)
    //    //    {
    //    //        return Negation.NewNegation(reducedConsequent);
    //    //    }

    //    //    return formula;
    //    //}

    //    if (formula is Implication)
    //    {
    //        Formula reducedFormula = formula;
    //        Implication implication = (Implication)formula;

    //        var reducedAntecent = implication.Antecedent.ReduceUsingBasicScheme();
    //        var reducedConsequent = implication.Consequent.ReduceUsingBasicScheme();

    //        if (reducedAntecent == Constant.TRUE)
    //        {
    //            return reducedConsequent;
    //        }
    //        if (reducedAntecent == Constant.FALSE)
    //        {
    //            return Constant.TRUE;
    //        }
    //        if (reducedConsequent == Constant.TRUE)
    //        {
    //            return Constant.TRUE;
    //        }
    //        if (reducedConsequent == Constant.FALSE)
    //        {
    //            return Negation.NewNegation(reducedAntecent).ReduceUsingBasicScheme();
    //        }

    //        {
    //            // *-.2.1 => *-.1.2 and *.2-.1 => *.1-.2
    //            if (reducedAntecent is Negation negatedAntecedent && reducedConsequent.CompareTo(negatedAntecedent.Child) < 0)
    //            {
    //                return Implication.NewImplication(Negation.NewNegation(reducedConsequent), negatedAntecedent.Child).ReduceUsingBasicScheme();
    //            }
    //            if (reducedConsequent is Negation negatedConsequent && negatedConsequent.Child.CompareTo(reducedAntecent) < 0)
    //            {
    //                return Implication.NewImplication(negatedConsequent.Child, Negation.NewNegation(reducedAntecent)).ReduceUsingBasicScheme();
    //            }
    //        }

    //        {
    //            //  *.2*.1.3 => *.1*.2.3
    //            if (reducedConsequent is Implication implConsequent 
    //                && implConsequent.Antecedent.CompareTo(reducedAntecent) < 0
    //                && implConsequent.Consequent.CompareTo(reducedAntecent) > 0)
    //            {
    //                return Implication.NewImplication(
    //                        implConsequent.Antecedent, 
    //                        Implication.NewImplication(reducedAntecent, implConsequent.Consequent))
    //                    .ReduceUsingBasicScheme();
    //            }
    //        }

    //        {
    //            // **.1.2-*.1.3 => -*.1-*.2-.3
    //            if (reducedAntecent is Implication implA 
    //                && reducedConsequent is Negation negC 
    //                && negC.Child is Implication implCC 
    //                && implCC.Antecedent.Equals(implA.Antecedent)
    //                && implA.Antecedent.CompareTo(implA.Consequent) < 0
    //                && implA.Consequent.CompareTo(implCC.Consequent) < 0)
    //            {
    //                return Negation.NewNegation(
    //                        Implication.NewImplication(
    //                            implA.Antecedent,
    //                            Negation.NewNegation(
    //                                Implication.NewImplication(
    //                                    implA.Consequent, 
    //                                    Negation.NewNegation(implCC.Consequent)))))
    //                    .ReduceUsingBasicScheme();
                        
    //            }
    //        }

    //        {
    //            // **.1.2-*.3.2 ==> -**-.1.3.2
    //            if (reducedAntecent is Implication implA 
    //                && reducedConsequent is Negation negC 
    //                && negC.Child is Implication implCC 
    //                && implCC.Consequent.Equals(implA.Consequent)
    //                && implA.Antecedent.CompareTo(implA.Consequent) < 0
    //                && implCC.Consequent.CompareTo(implCC.Antecedent) < 0)
    //            {
    //                return Negation.NewNegation(
    //                        Implication.NewImplication(
    //                            Implication.NewImplication(
    //                                Negation.NewNegation(implA.Antecedent),
    //                                implCC.Antecedent),
    //                            implCC.Consequent))
    //                    .ReduceUsingBasicScheme();
    //            }
    //        }

    //        {
    //            // **.1.3-*.2.3 ==> -**-.1.2.3
    //            if (reducedAntecent is Implication implA
    //                && reducedConsequent is Negation negC
    //                && negC.Child is Implication implCC
    //                && implCC.Consequent.Equals(implA.Consequent)
    //                && implA.Antecedent.CompareTo(implA.Consequent) < 0
    //                && implCC.Antecedent.CompareTo(implCC.Consequent) < 0)
    //            {
    //                return Negation.NewNegation(
    //                        Implication.NewImplication(
    //                            Implication.NewImplication(
    //                                Negation.NewNegation(implA.Antecedent),
    //                                implCC.Antecedent),
    //                            implCC.Consequent))
    //                    .ReduceUsingBasicScheme();
    //            }
    //        }
    //        {
    //            // **.2.1-*.2.3 ==> -*.2-*.1-.3
    //            if (reducedAntecent is Implication implA
    //                && reducedConsequent is Negation negC
    //                && negC.Child is Implication implCC
    //                && implCC.Antecedent.Equals(implA.Antecedent)
    //                && implA.Consequent.CompareTo(implA.Antecedent) < 0
    //                && implA.Consequent.CompareTo(implCC.Consequent) < 0)
    //            {
    //                return Negation.NewNegation(
    //                        Implication.NewImplication(
    //                            implA.Antecedent,
    //                            Negation.NewNegation(
    //                                Implication.NewImplication(
    //                                    implA.Consequent,
    //                                    Negation.NewNegation(implCC.Consequent)))))
    //                    .ReduceUsingBasicScheme();

    //            }
    //        }
    //        {
    //            // **.2.1-*.3.1 ==> -**-.2.3.1
    //            if (reducedAntecent is Implication implA
    //                && reducedConsequent is Negation negC
    //                && negC.Child is Implication implCC
    //                && implCC.Consequent.Equals(implA.Consequent)
    //                && implA.Consequent.CompareTo(implA.Antecedent) < 0
    //                && implCC.Consequent.CompareTo(implCC.Antecedent) < 0)
    //            {
    //                return Negation.NewNegation(
    //                            Implication.NewImplication(
    //                                Implication.NewImplication(
    //                                    Negation.NewNegation(implA.Antecedent),
    //                                    implCC.Antecedent),
    //                                implCC.Consequent))
    //                    .ReduceUsingBasicScheme();
    //            }
    //        }


    //        {
    //            // *-.1-*.2.3 ==> **.2.3.1
    //            // It's important that this condition is last
    //            if (reducedAntecent is Negation negatedAntecedent && reducedConsequent is Negation negatedConsequent)
    //            {
    //                return Implication.NewImplication(negatedConsequent.Child, negatedAntecedent.Child).ReduceUsingBasicScheme();
    //            }
    //        }


    //        foreach (var subterm in reducedAntecent.AllSubterms)
    //        {
    //            var subAntecedent = reducedAntecent.ReplaceAll(subterm, Constant.TRUE);
    //            subAntecedent = subAntecedent.ReduceUsingBasicScheme();

    //            if (subAntecedent == Constant.FALSE)
    //            {
    //                var subReducedConsequent = reducedConsequent.ReplaceAll(subterm, Constant.FALSE);
    //                subReducedConsequent = subReducedConsequent.ReduceUsingBasicScheme();

    //                if (subReducedConsequent != reducedConsequent)
    //                {
    //                    return Implication.NewImplication(reducedAntecent, subReducedConsequent).ReduceUsingBasicScheme();
    //                }
    //            }

    //            subAntecedent = reducedAntecent.ReplaceAll(subterm, Constant.FALSE);
    //            subAntecedent = subAntecedent.ReduceUsingBasicScheme();

    //            if (subAntecedent == Constant.FALSE)
    //            {
    //                var subReducedConsequent = reducedConsequent.ReplaceAll(subterm, Constant.TRUE);
    //                subReducedConsequent = subReducedConsequent.ReduceUsingBasicScheme();

    //                if (subReducedConsequent != reducedConsequent)
    //                {
    //                    return Implication.NewImplication(reducedAntecent, subReducedConsequent).ReduceUsingBasicScheme();
    //                }
    //            }


    //            var subConsequent = reducedConsequent.ReplaceAll(subterm, Constant.FALSE);
    //            subConsequent = subConsequent.ReduceUsingBasicScheme();
    //            if (subConsequent == Constant.TRUE)
    //            {
    //                var subReducedAntecent = reducedAntecent.ReplaceAll(subterm, Constant.TRUE);
    //                subReducedAntecent = subReducedAntecent.ReduceUsingBasicScheme();

    //                if (subReducedAntecent != reducedAntecent)
    //                {
    //                    return Implication.NewImplication(subReducedAntecent, reducedConsequent).ReduceUsingBasicScheme();
    //                }
    //            }

    //            subConsequent = reducedConsequent.ReplaceAll(subterm, Constant.TRUE);
    //            subConsequent = subConsequent.ReduceUsingBasicScheme();
    //            if (subConsequent == Constant.TRUE)
    //            {
    //                var subReducedAntecent = reducedAntecent.ReplaceAll(subterm, Constant.FALSE);
    //                subReducedAntecent = subReducedAntecent.ReduceUsingBasicScheme();

    //                if (subReducedAntecent != reducedAntecent)
    //                {
    //                    return Implication.NewImplication(subReducedAntecent, reducedConsequent).ReduceUsingBasicScheme();
    //                }
    //            }

    //        }


    //        if (!reducedAntecent.Equals(implication.Antecedent) || !reducedConsequent.Equals(implication.Consequent))
    //        {
    //            return Implication.NewImplication(reducedAntecent, reducedConsequent).ReduceUsingBasicScheme();
    //        }

    //        return implication;
    //    }

    //    throw new TermSatException($"Unknown formula type:{formula.GetType().FullName}");
    }

}
