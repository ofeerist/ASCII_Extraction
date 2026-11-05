using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Pawnshop.Endless.Tests
{
    public sealed class HouseRulesTests
    {
        ShopSimulation shop;
        HouseRules house;
        [SetUp] public void Setup(){shop=ShopSimulation.NewRun(12345);house=new HouseRules(shop);}
        ItemState BorrowToDesk()
        {
            house.State.doorOpen=true;house.Borrow();house.PlaceOnTable();return shop.Customer.item;
        }
        ItemState OwnedOnRug()
        {
            var item=shop.Customer.item;shop.Buy();house.Synchronize();house.State.carriedId=item.id;house.PlaceOnRug();return item;
        }
        static InspectionTrace Finished(InspectionTool tool)
        {
            var trace=new InspectionTrace(tool);
            for(var region=0;region<trace.Required;region++)for(var frame=0;frame<10;frame++)trace.Trace(region,.1f);
            return trace;
        }
        [Test] public void SupplierChargesOnceAndToolsCannotBeBoughtTwice()
        {
            var cash=shop.State.cash;house.Purchase(Supply.Loupe);house.Purchase(Supply.Loupe);
            Assert.AreEqual(cash-32,shop.State.cash);Assert.AreEqual(1,house.State.Count(Supply.Loupe));Assert.AreEqual(32,shop.State.report.supplies);
        }
        [Test] public void UnaffordableSuppliesDoNotChangeInventory()
        {
            shop.State.cash=0;house.Purchase(Supply.UVFlashlight);Assert.AreEqual(0,house.State.Count(Supply.UVFlashlight));
        }
        [Test] public void ExchangeIsAtomicAndUsesTheSameRitualInventory()
        {
            house.State.Add(Supply.Milk,1);house.State.Add(Supply.Crackers,1);
            Assert.IsFalse(house.State.TryExchange(new Dictionary<Supply,int>{{Supply.Milk,2}},new Dictionary<Supply,int>{{Supply.Salt,1}}));
            Assert.AreEqual(1,house.State.Count(Supply.Milk));Assert.AreEqual(0,house.State.Count(Supply.Salt));
            Assert.IsTrue(house.State.TryExchange(new Dictionary<Supply,int>{{Supply.Milk,1},{Supply.Crackers,1}},new Dictionary<Supply,int>{{Supply.Salt,2}}));
            Assert.AreEqual(0,house.State.Count(Supply.Milk));Assert.AreEqual(2,house.State.Count(Supply.Salt));
        }
        [Test] public void BorrowedGoodsCannotBeStoredOrUsedInRituals()
        {
            house.State.doorOpen=true;house.Borrow();var id=house.State.carriedId;
            house.PlaceOnShelf(0);house.PlaceOnRug();Assert.AreEqual(id,house.State.carriedId);Assert.AreEqual(0,house.State.ritualId);Assert.IsEmpty(shop.State.stock);
        }
        [Test] public void InspectionRequiresToolAndCompletedPhysicalTrace()
        {
            var item=BorrowToDesk();var actions=shop.State.actions;
            house.CompleteInspection(Finished(InspectionTool.Loupe),item.id);Assert.IsFalse(item.inspected);
            house.Purchase(Supply.Loupe);house.CompleteInspection(new InspectionTrace(InspectionTool.Loupe),item.id);Assert.IsFalse(item.inspected);
            house.CompleteInspection(Finished(InspectionTool.Loupe),item.id);Assert.IsTrue(item.inspected);Assert.AreEqual(actions-1,shop.State.actions);
            house.CompleteInspection(Finished(InspectionTool.Loupe),item.id);Assert.AreEqual(actions-1,shop.State.actions);
        }
        [Test] public void OneClickOrOneRegionCannotCompleteTheExamination()
        {
            var trace=new InspectionTrace(InspectionTool.Loupe);trace.Trace(0,100);Assert.IsFalse(trace.Complete);Assert.Less(trace.Progress(0),1);
            for(var i=0;i<20;i++)trace.Trace(0,.1f);Assert.AreEqual(1,trace.Completed);Assert.IsFalse(trace.Complete);
        }
        [Test] public void GoldReagentIsConsumedOnlyOnCompletedTestAndResultSurvivesReload()
        {
            var item=BorrowToDesk();house.Purchase(Supply.GoldReagent);var trace=Finished(InspectionTool.GoldReagent);
            house.CompleteInspection(trace,item.id);var gold=item.containsGold;Assert.AreEqual(0,house.State.Count(Supply.GoldReagent));Assert.IsTrue(item.goldTested);
            var restored=new ShopSimulation(JsonUtility.FromJson<RunState>(JsonUtility.ToJson(shop.State)));var rules=new HouseRules(restored);
            Assert.AreEqual(gold,rules.Find(item.id).containsGold);var actions=restored.State.actions;rules.CompleteInspection(trace,item.id);Assert.AreEqual(actions,restored.State.actions);
        }
        [Test] public void UVRevealsFixedCurseAndAddsKnowledge()
        {
            var item=BorrowToDesk();item.curse=Curse.Hunger;house.Purchase(Supply.UVFlashlight);
            house.CompleteInspection(Finished(InspectionTool.Ultraviolet),item.id);
            Assert.IsTrue(item.identified);Assert.Contains(Curse.Hunger,shop.State.discoveries);
        }
        [Test] public void SaltRequiresAllSectorsAndCannotBeChargedAgain()
        {
            OwnedOnRug();house.State.Add(Supply.Salt,1);
            for(var i=0;i<7;i++)house.TraceSalt(i);Assert.IsFalse(house.State.saltPlaced);Assert.AreEqual(1,house.State.Count(Supply.Salt));
            house.TraceSalt(7);Assert.IsTrue(house.State.saltPlaced);Assert.AreEqual(0,house.State.Count(Supply.Salt));
            house.TraceSalt(7);Assert.AreEqual(0,house.State.Count(Supply.Salt));
        }
        [Test] public void RitualResumesAfterReloadAndConsumesEachComponentOnce()
        {
            var item=OwnedOnRug();item.curse=Curse.Cold;
            house.State.Add(Supply.Salt,1);house.State.Add(Supply.Milk,1);house.State.Add(Supply.Vodka,1);
            house.PourMilk(true);Assert.AreEqual(1,house.State.Count(Supply.Milk));
            for(var i=0;i<8;i++)house.TraceSalt(i);house.PourMilk(true);
            shop=new ShopSimulation(JsonUtility.FromJson<RunState>(JsonUtility.ToJson(shop.State)));house=new HouseRules(shop);
            Assert.IsTrue(house.State.milkPoured);Assert.IsTrue(house.State.saltPlaced);
            var actions=shop.State.actions;house.Cleanse(Finished(InspectionTool.Loupe));house.Cleanse(Finished(InspectionTool.Loupe));
            Assert.AreEqual(Curse.None,house.Find(item.id).curse);Assert.AreEqual(actions-1,shop.State.actions);
            Assert.AreEqual(0,house.State.Count(Supply.Salt));Assert.AreEqual(0,house.State.Count(Supply.Milk));Assert.AreEqual(0,house.State.Count(Supply.Vodka));
        }
        [Test] public void PurchasedItemsMustBePhysicallyShelvedBeforeClosing()
        {
            var item=OwnedOnRug();house.CloseNight();Assert.AreEqual(NightPhase.Reception,shop.State.phase);
            house.PickUp(item.id);house.PlaceOnShelf(item.slot);house.CloseNight();Assert.AreEqual(NightPhase.Results,shop.State.phase);
        }
        [Test] public void RefusingBorrowedItemClearsItsPhysicalLocation()
        {
            BorrowToDesk();shop.Reject();house.Synchronize();Assert.AreEqual(0,house.State.tableId);Assert.IsFalse(house.State.itemBorrowed);
        }
        [Test] public void PickingUpRitualItemResetsProgressWithoutRefundingUsedComponents()
        {
            var item=OwnedOnRug();house.State.Add(Supply.Salt,1);for(var i=0;i<8;i++)house.TraceSalt(i);
            house.PickUp(item.id);Assert.IsFalse(house.State.saltPlaced);Assert.AreEqual(0,house.State.Count(Supply.Salt));Assert.AreEqual(item.id,house.State.carriedId);
        }
        [Test] public void InvalidInventoryAndPhysicalStateCannotBeSaved()
        {
            Assert.IsTrue(RunStorage.Validate(shop.State));
            house.State.Add(Supply.Milk,100);Assert.IsFalse(RunStorage.Validate(shop.State));
            house.State.inventory.Clear();house.State.playerX=float.NaN;Assert.IsFalse(RunStorage.Validate(shop.State));
            house.State.playerX=1;house.State.milkPoured=true;Assert.IsFalse(RunStorage.Validate(shop.State));
            house.State.milkPoured=false;house.State.carriedId=1;house.State.tableId=1;Assert.IsFalse(RunStorage.Validate(shop.State));
        }
        [Test] public void ExchangeRejectsUnknownGoodsWithoutSpendingAnything()
        {
            house.State.Add(Supply.Milk,1);
            Assert.IsFalse(house.State.TryExchange(new Dictionary<Supply,int>{{Supply.Milk,1}},new Dictionary<Supply,int>{{(Supply)999,1}}));
            Assert.AreEqual(1,house.State.Count(Supply.Milk));
        }
    }
}
