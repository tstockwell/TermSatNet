using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.RuleDatabase;

namespace TermSAT.Tests
{
    [TestClass]
    public class BasicCofactorsTests : BaseLucidTestClass
    {

        [TestMethod]
        public async Task CofactorTests()
        {
            var trueId = await Lucid.GetConstantExpressionIdAsync(true);
            var falseId = await Lucid.GetConstantExpressionIdAsync(false);

            var one = await Lucid.GetMostlyCanonicalRecordAsync(Formula.GetOrParse(".1"));
            var two = await Lucid.GetMostlyCanonicalRecordAsync(Formula.GetOrParse(".2"));

            var notOne = await Lucid.GetMostlyCanonicalRecordAsync(Formula.GetOrParse("|T.1"));
            var notTwo = await Lucid.GetMostlyCanonicalRecordAsync(Formula.GetOrParse("|T.2"));
            {
                // every expression should be a tgt-cofactor of itself
                var hasUnifedCofactor = Lucid.Cofactors
                    .Where(_ => _.ExpressionId == notTwo.Id 
                            && _.ConclusionId == trueId 
                            && _.SubtermId == notTwo.Id
                            && _.ReplacementId == trueId)
                    .Any();
                Assert.IsTrue(hasUnifedCofactor);
            }

            var oneTwo = await Lucid.GetMostlyCanonicalRecordAsync(Formula.GetOrParse("|.1.2"));

            // Example: Given (1 (T 2)),
            //      since (T 2) is a t-grounding, 
            //      and since 1 is an fgf-grounding of the lhs,  
            //		and since therefore rhs[T<-1] == (1 2) may be substituted for (T 2),  
            //		then (1 2) is also a t-grounding of (1 (T 2)) with a conclusion of (1 (1 2)) 
            var ifOneThenTwo = new ReductionRecord(Formula.GetOrParse("|.1|T.2"));
            await Lucid.InsertFormulaRecordAsync(ifOneThenTwo);
            {
                var hasUnifedCofactor = Lucid.Cofactors
                    .Where(_ => _.ExpressionId == ifOneThenTwo.Id && _.UnifiedSubtermId == oneTwo.Id && _.ConclusionId == trueId)
                    .Any();
                Assert.IsTrue(hasUnifedCofactor);
            }

            var ifTwoThenOne = await Lucid.GetMostlyCanonicalRecordAsync(Formula.GetOrParse("|.2|T.1"));
            {
                var hasUnifedCofactor = Lucid.Cofactors
                    .Where(_ => _.ExpressionId == ifTwoThenOne.Id 
                            && _.UnifiedSubtermId == oneTwo.Id 
                            && _.ConclusionId == trueId)
                    .Any();
                Assert.IsTrue(hasUnifedCofactor);
            }

            var unifiable = new ReductionRecord(Formula.GetOrParse("||.1|T.2|.2|T.1"));
            await Lucid.InsertFormulaRecordAsync(unifiable);
            {
                // ||.1|T.2|.2|T.1 is equivalent to ||.1|.1.2|.2|.1.2,
                // so |.1.2 should be a fgf-cofactor of ||.1|T.2|.2|T.1
                var hasUnifedCofactor = Lucid.Cofactors
                    .Where(_ => _.ExpressionId == unifiable.Id 
                            && _.UnifiedSubtermId == oneTwo.Id 
                            && _.ReplacementId == falseId
                            && _.ConclusionId == falseId)
                    .Any();
                Assert.IsTrue(hasUnifedCofactor);
            }

            /////
            //var hasUnifedCofactor = Lucid.Cofactors
            //    .Where(_ => _.ExpressionId == unifiable.Id && _.SubtermId == oneTwo.Id)
            //    .Any();
            //Assert.IsTrue(hasUnifedCofactor);
        }
    }
}
