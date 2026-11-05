using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace Pawnshop.Endless.Tests
{
    public class ChapterOneTests
    {
        static QuestRules New() { var q=new QuestRules(ShopSimulation.NewRun(876));q.StartChapter();return q; }
        static void Noon(QuestRules q) { q.Advance();q.State.doorOpened=true; }
        static void EndDay(QuestRules q)
        {
            while(q.Shop.Customer!=null){q.State.doorOpened=true;q.Deal(2);}
            q.Advance();q.Advance();q.Close();
        }
        static void Next(QuestRules q){EndDay(q);q.NextDay();Noon(q);}
        static void Prove(QuestRules q){q.Inspect(InspectionTool.Loupe);q.Compare(0,Evidence.Wear);q.Compare(1,Evidence.Mark);}
        [Test] public void ChapterHasThreeVisitsPerDayAndStopsAtThree()
        {
            var q=New();Noon(q);
            Assert.AreEqual(3,q.Shop.State.customers.Count);Next(q);Next(q);EndDay(q);
            q.NextDay();Assert.AreEqual(3,q.Shop.State.night);q.FinishChapter(false);
            Assert.AreEqual(ChapterEnding.OpenLedger,q.State.ending);
            q.NextDay();Assert.AreEqual(3,q.Shop.State.night);Assert.IsTrue(RunStorage.Validate(q.Shop.State));
        }
        [Test] public void EvidenceMustBeObservedCannotRepeatAndDropsPriceManyTimes()
        {
            var q=New();Noon(q);var c=q.Shop.Customer;int minimum=c.minimum;
            q.Compare(0,Evidence.Wear);Assert.AreEqual(minimum,c.minimum);
            Prove(q);Assert.LessOrEqual(c.minimum,minimum/8);
            int price=c.minimum;q.Compare(0,Evidence.Wear);Assert.AreEqual(price,c.minimum);
            q.Deal(0,price);q.Compare(2,Evidence.Voice);Assert.AreEqual(price,c.minimum);
        }
        [Test] public void FalseAccusationsDismissVisitorWithoutPurchase()
        {
            var q=New();Noon(q);q.Inspect(InspectionTool.Loupe);var c=q.Shop.Customer;
            for(int i=0;i<3;i++)q.Compare(2,Evidence.Mark);
            Assert.AreNotSame(c,q.Shop.Customer);Assert.IsEmpty(q.Shop.State.stock);Assert.IsFalse(q.State.doorOpened);
        }
        [Test] public void BorisComesWithItemsAndBuyingRingEndsAtInterrogation()
        {
            var q=New();Noon(q);Assert.AreEqual("boris",q.Shop.Customer.story);Assert.AreEqual(9,q.Shop.Customer.item.template);
            Next(q);Assert.AreEqual(5,q.Shop.Customer.item.template);Prove(q);q.Deal(0,q.Shop.Customer.minimum);q.Deal(1);
            Assert.IsTrue(q.State.robberyDelivered);Assert.IsFalse(q.State.policePending);
            Next(q);Assert.IsTrue(q.State.policePending);
            for(int i=0;i<3;i++)q.AnswerPolice(0);
            Assert.AreEqual(ChapterEnding.Interrogation,q.State.ending);Assert.IsTrue(RunStorage.Validate(q.Shop.State));
            int cash=q.Shop.State.cash;q.AnswerPolice(1);Assert.AreEqual(cash,q.Shop.State.cash);
        }
        [Test] public void RefusingRingAvoidsPoliceAndPreservesOtherEndings()
        {
            var q=New();Noon(q);Next(q);q.Deal(2);Next(q);Assert.IsFalse(q.State.policePending);
        }
        [Test] public void AgreementRequiresUncleansedCarrierAndAnnaStory()
        {
            var q=New();Noon(q);Next(q);Next(q);
            q.Deal(2);q.State.doorOpened=true;q.Deal(2);q.State.doorOpened=true;
            q.AskStory();q.Inspect(InspectionTool.Loupe);q.Inspect(InspectionTool.Ultraviolet);
            q.Compare(0,Evidence.Wear);q.Compare(2,Evidence.Voice);q.Deal(0,q.Shop.Customer.minimum);q.Deal(1);
            EndDay(q);Assert.IsTrue(q.CanAgree);q.FinishChapter(true);Assert.AreEqual(ChapterEnding.Agreement,q.State.ending);
        }
        [Test] public void ProgressEvidenceAndEndingSurviveSave()
        {
            var q=New();Noon(q);Prove(q);
            var restored=new QuestRules(new ShopSimulation(JsonUtility.FromJson<RunState>(JsonUtility.ToJson(q.Shop.State))));
            Assert.IsTrue(RunStorage.Validate(restored.Shop.State));Assert.AreEqual(3,restored.Shop.Customer.evidenceMask);
            Assert.AreEqual(q.Shop.Customer.minimum,restored.Shop.Customer.minimum);Assert.AreEqual("boris",restored.Shop.Customer.story);
        }
        [Test] public void RecordsSaleIsOneShotAndHasConsequence()
        {
            var q=New();Noon(q);Next(q);Next(q);q.Deal(2);q.State.doorOpened=true;
            int cash=q.Shop.State.cash;q.SellRecords();q.SellRecords();Assert.AreEqual(cash+100,q.Shop.State.cash);Assert.IsTrue(q.State.recordsSold);
        }
    }
}
