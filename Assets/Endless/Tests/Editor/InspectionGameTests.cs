using NUnit.Framework;
namespace Pawnshop.Endless.Tests
{
    public class InspectionGameTests
    {
        [TestCase(InspectionTool.Loupe)] [TestCase(InspectionTool.Ultraviolet)]
        public void ScanNeedsAllThreeRegionsAndHeldInput(InspectionTool tool)
        {
            var g=new InspectionGame(tool);
            for(int i=0;i<100;i++)g.Scan(0,.02f,false);
            Assert.AreEqual(0,g.Progress);
            for(int i=0;i<100;i++)g.Scan(0,.02f,true);
            Assert.IsFalse(g.Complete);
            for(int r=1;r<3;r++)for(int i=0;i<40;i++)g.Scan(r,.02f,true);
            Assert.IsTrue(g.Complete);
        }
        [Test] public void ReagentRequiresContinuousScratchThenDropAtCenter()
        {
            var g=new InspectionGame(InspectionTool.GoldReagent);
            g.ApplyReagent(1,true,.02f,true);Assert.AreEqual(0,g.Scratch);
            g.ApplyReagent(0,true,.02f,true);g.ApplyReagent(.8f,true,.02f,true);Assert.AreEqual(0,g.Scratch);
            for(int i=0;i<=20;i++)g.ApplyReagent(i/20f,true,.02f,true);
            Assert.AreEqual(1,g.Scratch);Assert.IsFalse(g.Complete);
            for(int i=0;i<80;i++)g.ApplyReagent(.9f,true,.02f,true);
            Assert.AreEqual(0,g.Reaction);
            for(int i=0;i<80;i++)g.ApplyReagent(.5f,true,.02f,true);
            Assert.IsTrue(g.Complete);
        }
        [Test] public void LargeFrameCannotInstantlyFinishScan()
        {
            var g=new InspectionGame(InspectionTool.Loupe);for(int i=0;i<3;i++)g.Scan(i,1000,true);Assert.IsFalse(g.Complete);
        }
        [Test] public void SaleCardDoesNotRevealUnknownProperties()
        {
            var item=new ItemState {cost=17,condition=40,curse=Curse.Cold,stolen=true};
            var card=QuestRules.ItemDetails(item);StringAssert.Contains("17 ₽",card);StringAssert.Contains("Износ: не проверен",card);StringAssert.DoesNotContain("Мёртвый холод",card);
            item.inspected=true;item.identified=true;item.observedMark="Клеймо: Ю-041";
            card=QuestRules.ItemDetails(item);StringAssert.Contains("износ 60%",card);StringAssert.Contains("Мёртвый холод",card);StringAssert.Contains("Ю-041",card);
        }
        [Test] public void RitualHintAndReadyFollowRecipe()
        {
            var q=new QuestRules(ShopSimulation.NewRun(123));var item=new ItemState{id=999,curse=Curse.Cold};q.Shop.State.stock.Add(item);
            q.House.State.Add(Supply.Salt,1);q.House.State.Add(Supply.Milk,1);
            StringAssert.Contains("Шаг 1",q.RitualHint(999));q.State.saltPoints=5;
            StringAssert.Contains("Шаг 2",q.RitualHint(999));Assert.IsFalse(q.RitualReady(999));
            q.State.milk=true;StringAssert.Contains("Шаг 3",q.RitualHint(999));q.State.boiled=true;
            Assert.IsTrue(q.RitualReady(999));item.curse=Curse.None;Assert.IsFalse(q.RitualReady(999));
        }
    }
}
