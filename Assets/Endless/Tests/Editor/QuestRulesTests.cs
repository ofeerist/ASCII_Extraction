using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Pawnshop.Endless.Tests
{
    public class QuestRulesTests
    {
        static QuestRules New() => new QuestRules(ShopSimulation.NewRun(12345));
        static void Noon(QuestRules q) { q.Advance(); q.State.doorOpened = true; }
        static void Night(QuestRules q) { Noon(q); while (q.Shop.Customer != null) { q.State.doorOpened = true; q.Deal(2); } q.Advance(); q.Advance(); }
        static ItemState Add(QuestRules q, Curse curse) { var i = new ItemState { id = q.Shop.State.nextId++, template = 5, condition = 100, baseValue = 110, slot = q.Shop.State.stock.Count, curse = curse, askingPrice = 80 }; q.Shop.State.stock.Add(i); return i; }
        [Test] public void PhasesGuardActionsAndPreserveMoney()
        {
            var q = New(); var money = q.Shop.State.cash; q.Deal(1); q.List(1, 1, true); q.Ritual(1, 1);
            Assert.AreEqual(money, q.Shop.State.cash); Assert.AreEqual(0, q.Shop.State.stock.Count);
            Noon(q); q.Purchase(Supply.Salt); Assert.AreEqual(0, q.House.State.Count(Supply.Salt));
            q.Advance(); Assert.AreEqual(DayPhase.Noon, q.State.phase);
        }
        [Test] public void TrustBottleDeliveryListingAndPoliceAreOneShot()
        {
            var q = New(); q.AnswerStreet(1); q.AnswerStreet(0); q.AnswerStreet(1);
            q.PromiseRobbery(); Assert.IsFalse(q.State.robberyPromised);
            q.House.State.Add(Supply.Vodka, 2); q.PromiseRobbery(); q.PromiseRobbery(); Assert.AreEqual(1, q.House.State.Count(Supply.Vodka));
            Night(q); q.Close(); q.NextDay(); Noon(q);
            Assert.AreEqual(1, q.Shop.State.stock.Count(x => x.stolen));
            while (q.Shop.Customer != null) { q.State.doorOpened = true; q.Deal(2); }
            q.Advance(); var stolen = q.Shop.State.stock.Single();
            q.List(stolen.id, 100000, true); q.List(stolen.id, 100000, false); Assert.IsTrue(q.State.policePending);
            q.AnswerPolice(0); Assert.AreEqual(0, q.State.policeQuestion);
            q.Advance(); q.Close(); q.NextDay(); Noon(q);
            q.AnswerPolice(0); q.AnswerPolice(0); q.AnswerPolice(0);
            var cash = q.Shop.State.cash; q.AnswerPolice(1);
            Assert.IsTrue(q.State.policeResolved); Assert.IsFalse(q.State.policePending); Assert.AreEqual(cash, q.Shop.State.cash); Assert.IsFalse(q.Shop.State.stock.Any(x => x.stolen));
        }
        [Test] public void BadAnswersDoNotAuthorizeRobbery()
        {
            var q = New(); q.AnswerStreet(0); q.AnswerStreet(1); q.AnswerStreet(0); q.House.State.Add(Supply.Vodka, 1); q.PromiseRobbery();
            Assert.IsFalse(q.State.robberyPromised); Assert.AreEqual(1, q.House.State.Count(Supply.Vodka));
        }
        [TestCase(Curse.Whisper)] [TestCase(Curse.Hunger)] [TestCase(Curse.Cold)]
        public void RitualRequiresCorrectRecipeAndConsumesOnce(Curse curse)
        {
            var q = New(); var item = Add(q, curse); q.House.State.Add(Supply.Salt, 2); q.House.State.Add(Supply.Milk, 2); Night(q);
            q.Ritual(item.id, 4); Assert.AreEqual(curse, item.curse);
            for (int i = 0; i < 6; i++) q.Ritual(item.id, 1);
            Assert.AreEqual(1, q.House.State.Count(Supply.Salt));
            if (curse != Curse.Whisper) { q.Ritual(item.id, 4); Assert.AreEqual(curse, item.curse); q.Ritual(item.id, 2); q.Ritual(item.id, 2); Assert.AreEqual(1, q.House.State.Count(Supply.Milk)); }
            if (curse == Curse.Cold) { q.Ritual(item.id, 4); Assert.AreEqual(curse, item.curse); q.Ritual(item.id, 3); }
            int actions = q.Shop.State.actions; q.Ritual(item.id, 4); q.Ritual(item.id, 4);
            Assert.AreEqual(Curse.None, item.curse); Assert.AreEqual(actions-1, q.Shop.State.actions);
        }
        [Test] public void BarterRejectsWrongGoodsAndTransfersOnlyOnce()
        {
            var q = New(); q.StartChapter(); Noon(q);
            q.Inspect(InspectionTool.Loupe); q.Compare(0, Evidence.Wear); q.Compare(1, Evidence.Mark);
            q.House.State.Add(Supply.Salt, 3);
            q.Barter(Supply.Salt); Assert.AreEqual(3, q.House.State.Count(Supply.Salt)); Assert.IsEmpty(q.Shop.State.stock);
            q.Barter(Supply.Crackers); q.Barter(Supply.Crackers);
            Assert.AreEqual(0, q.House.State.Count(Supply.Crackers)); Assert.AreEqual(1, q.Shop.State.stock.Count);
        }
        [Test] public void PartialRitualAndQuestSurviveJsonReload()
        {
            var q = New(); var item = Add(q, Curse.Cold); q.House.State.Add(Supply.Salt, 1); Night(q); q.Ritual(item.id, 1); q.Ritual(item.id, 1);
            q.State.trust = 2; q.State.streetQuestion = 3; q.State.robberyPromised = true;
            var restored = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(q.Shop.State));
            Assert.IsTrue(RunStorage.Validate(restored));
            Assert.AreEqual(2, restored.quest.saltPoints); Assert.AreEqual(item.id, restored.quest.ritualId); Assert.IsTrue(restored.quest.robberyPromised);
        }
        [Test] public void SoldRitualItemDoesNotLockNextRitual()
        {
            var q = New(); var item = Add(q, Curse.Cold); item.listed = true; item.askingPrice = 1;
            q.House.State.Add(Supply.Salt, 1); Night(q); q.Ritual(item.id, 1); q.Close();
            Assert.AreEqual(0, q.State.ritualId); Assert.AreEqual(0, q.State.saltPoints);
        }
    }
}
