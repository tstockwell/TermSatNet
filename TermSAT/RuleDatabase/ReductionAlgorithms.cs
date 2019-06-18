using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TermSAT.Formulas;

namespace TermSAT.RuleDatabase
{
    /// <summary>
    /// After the RuleGenerator program generated all reduction rules necessary to reduce 
    /// all formulas with three or less variables it became apparent that ALL of those 
    /// reduction rules could be reduced to a simple algorithm.
    /// 
    /// That algorthm is implemented here, along with any other algorithms 'discovered' 
    /// by the RuleCompletionGenerator program.
    /// 
    /// </summary>
    public static class ReductionAlgorithms
    {
        /// <summary>
        /// While developing TermSAT, the RuleGenerator program was used to generate reduction rules 
        /// for all formulas of three variables or less.  Then it was discovered that all these 
        /// reduction rules could be replaced by a simple algorith that TermSAT calls the 
        /// 'single replacement' algorithm.
        /// 
        /// Basically the algorithm is this...  
        /// 
        /// If a formula C (could be a single variable) appears in the consequent of an implication and 
        /// if replacing all occurence of formula C in the consequent with either T or F makes the entire 
        /// consequent true (and hence, makes the original formula true) then 
        /// replace all occurences of C that appear in the original formula's antecendent with the opposite 
        /// value. For instance, if replacing C with T makes the consequent true then replace all occurences 
        /// of C in the antecedent with F.
        /// 
        /// Also, the converse is true...
        /// 
        /// If a formula C (could be a single variable) appears in the antecedent of an implication and 
        /// if replacing all occurence of formula C in the antecedent with either T or F makes the entire 
        /// antecedent false (and hence, makes the original formula true) then 
        /// replace all occurences of C that appear in the original formula's consequent with the opposite 
        /// value. For instance, if replacing C with T makes the antecedent true then replace all occurences 
        /// of C in the consequent with F.
        /// 
        /// </summary>
        /// <returns>A reduced formula, or the original formula if the orginal formula connot be reduced.</returns>
        async static public Task<Formula> reduceUsingSingleReplacement(Formula formula)
        {
            if (formula is Constant)
                return formula;
            if (formula is Variable)
                return formula;
            if (formula is Negation)
            {
                var child = (formula as Negation).Child;
                var reducedChild = await reduceUsingSingleReplacement(child);
                if (reducedChild != child)
                    return Negation.newNegation(reducedChild);
                return formula;
            }

            Implication i = formula as Implication;
            var consequentTask = Task.Run(() => { return reduceUsingSingleReplacement(i.Consequent); });
            var antecedentTask = Task.Run(() => { return reduceUsingSingleReplacement(i.Antecedent); });

            var consequent = await consequentTask;
            var antecedent = await consequentTask;

            if (consequent != i.Consequent || antecedent != i.Antecedent)
                return Implication.newImplication(antecedent, consequent);

            return formula;
        }
    }
}
