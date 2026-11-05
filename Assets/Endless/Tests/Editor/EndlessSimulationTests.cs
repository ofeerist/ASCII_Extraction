using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Pawnshop.Endless.Tests
{
    public sealed class EndlessSimulationTests
    {
        ShopSimulation game;
        [SetUp] public void Setup() => game = ShopSimulation.NewRun(12345);
        ItemState Stock(Curse curse, int slot, int value = 100)
        {
            var item = new ItemState { id = game.State.nextId++, template = 0, baseValue = value, condition = 100, curse = curse, suspicious = curse != Curse.None, slot = slot, askingPrice = value, acquiredNight = game.State.night };
            game.State.stock.Add(item); return item;
        }
        ShopSimulation Reload() => new ShopSimulation(JsonUtility.FromJson<RunState>(JsonUtility.ToJson(game.State)));

        [Test] public void SeedReproducesEveryCustomerAndWarning()
        {
            Assert.AreEqual(JsonUtility.ToJson(game.State), JsonUtility.ToJson(ShopSimulation.NewRun(12345).State));
            for (var n = 0; n < 200; n++)
            {
                Assert.IsTrue(game.State.customers.All(c => c.item.curse == Curse.None || c.item.suspicious));
                game.State.cash = 10000; game.CloseNight(); game.NextNight();
            }
        }
        [Test] public void AcceptedDealSurvivesReloadAndCostsExactlyOnce()
        {
            var price = game.Customer.minimum;
            game.Offer(price);
            game = Reload();
            var before = game.State.cash; var id = game.Customer.item.id;
            game.Buy();
            Assert.AreEqual(before - price, game.State.cash);
            Assert.AreEqual(id, game.State.stock.Single().id);
            Assert.AreEqual(1, game.State.customerIndex);
        }
        [Test] public void DiagnosticsAreFixedAndRepeatedTestsAreFree()
        {
            var item = game.Customer.item;
            item.curse = Curse.Hunger; item.suspicious = true;
            game.Diagnose(item.id, Diagnostic.Scales);
            var cash = game.State.cash; var actions = game.State.actions;
            game.Diagnose(item.id, Diagnostic.Scales);
            Assert.AreEqual(cash, game.State.cash); Assert.AreEqual(actions, game.State.actions);
            Assert.IsTrue(item.identified); Assert.Contains(Curse.Hunger, game.State.discoveries);
        }
        [Test] public void DiagnosticsCannotSpendMissingResources()
        {
            game.State.cash = 0; var actions = game.State.actions;
            game.Diagnose(game.Customer.item.id, Diagnostic.Occult);
            Assert.AreEqual(actions, game.State.actions); Assert.IsFalse(game.Customer.item.tested);
            game.State.cash = 100; game.State.actions = 0;
            game.Diagnose(game.Customer.item.id, Diagnostic.Loupe);
            Assert.AreEqual(100, game.State.cash);
        }
        [Test] public void RepeatedArgumentCannotLowerPriceAgain()
        {
            game.Customer.item.inspected = true; game.UseArgument();
            var minimum = game.Customer.minimum; game.UseArgument(); Assert.AreEqual(minimum, game.Customer.minimum);
        }
        [Test] public void ThreeLowOffersSendTheCustomerAwayWithoutPayment()
        {
            var cash = game.State.cash;
            game.Offer(1); game.Offer(1); game.Offer(1);
            Assert.AreEqual(1, game.State.customerIndex); Assert.AreEqual(cash, game.State.cash);
        }
        [Test] public void InvalidOffersDoNotConsumeAttempts()
        {
            game.Offer(-1); game.Offer(game.State.cash + 1);
            Assert.AreEqual(3, game.Customer.attempts);
        }
        [Test] public void OrthogonalAdjacencyDoesNotWrapAcrossRows()
        {
            Assert.IsTrue(game.IsAdjacent(0, 1)); Assert.IsTrue(game.IsAdjacent(0, 4));
            Assert.IsFalse(game.IsAdjacent(3, 4)); Assert.IsFalse(game.IsAdjacent(0, 5));
            Assert.IsFalse(game.IsAdjacent(0, ShopSimulation.IsolationSlot));
        }
        [Test] public void IsolationBlocksColdButNotHunger()
        {
            Stock(Curse.Cold, ShopSimulation.IsolationSlot); var victim = Stock(Curse.None, 0);
            game.CloseNight(); Assert.AreEqual(100, victim.condition);
            game = ShopSimulation.NewRun(12345); Stock(Curse.Hunger, ShopSimulation.IsolationSlot);
            game.CloseNight(); Assert.AreEqual(12, game.State.report.curseLoss);
        }
        [Test] public void WrappingSuppressesEveryCurse()
        {
            foreach (var curse in new[] { Curse.Cold, Curse.Whisper, Curse.Hunger })
            {
                game = ShopSimulation.NewRun(12345);
                var item = Stock(curse, 0); var neighbor = Stock(Curse.None, 1);
                game.Wrap(item.id); game.CloseNight();
                Assert.AreEqual(0, game.State.report.curseLoss); Assert.AreEqual(100, neighbor.condition); Assert.IsFalse(game.State.distortion);
            }
        }
        [Test] public void SalesHappenBeforeCursesAndPaymentsAreIdempotent()
        {
            var item = Stock(Curse.Hunger, 0); game.ListItem(item.id, 10, true);
            var before = game.State.cash; var due = game.State.due;
            game.CloseNight();
            Assert.AreEqual(before + 10 - due, game.State.cash); Assert.AreEqual(0, game.State.report.curseLoss);
            game = Reload(); var after = game.State.cash;
            game.CloseNight(); game.PayExpenses(); Assert.AreEqual(after, game.State.cash); Assert.AreEqual(1, game.State.completedNights);
        }
        [Test] public void NightlyCurseDamageNeverExceedsBudget()
        {
            for (var i = 0; i < 8; i++) Stock(i % 2 == 0 ? Curse.Cold : Curse.Hunger, i);
            var budget = game.LossBudget; game.CloseNight();
            Assert.LessOrEqual(game.State.report.damage + game.State.report.curseLoss, budget);
        }
        [Test] public void SettlementAllowsLiquidationBeforeBankruptcy()
        {
            game.State.cash = 0; var item = Stock(Curse.None, 0);
            game.CloseNight(); Assert.AreEqual(NightPhase.Settlement, game.State.phase);
            game.Liquidate(item.id); game.PayExpenses();
            Assert.AreEqual(NightPhase.Results, game.State.phase);
            Assert.AreEqual(45 - game.State.due, game.State.cash);
            game.ConfirmBankruptcy(); Assert.AreEqual(NightPhase.Results, game.State.phase);
        }
        [Test] public void BankruptcyDoesNotChargeTheDebtAgain()
        {
            game.State.cash = 0; game.CloseNight(); game.ConfirmBankruptcy();
            Assert.AreEqual(NightPhase.Bankrupt, game.State.phase);
            game.NextNight(); Assert.AreEqual(1, game.State.night);
            Assert.AreEqual(0, game.State.cash); Assert.AreEqual(0, game.State.completedNights);
        }
        [Test] public void ReloadPreservesSalesAndAllConsequences()
        {
            for (var i = 0; i < 4; i++) { var item = Stock((Curse)i, i); game.ListItem(item.id, 95, true); }
            var restored = Reload(); game.CloseNight(); restored.CloseNight();
            Assert.AreEqual(JsonUtility.ToJson(game.State), JsonUtility.ToJson(restored.State));
        }
        [Test] public void FullStorageRejectsPurchaseWithoutCharging()
        {
            for (var i = 0; i < 8; i++) Stock(Curse.None, i); Stock(Curse.None, ShopSimulation.IsolationSlot);
            var cash = game.State.cash; game.Buy(); Assert.AreEqual(cash, game.State.cash); Assert.AreEqual(0, game.State.customerIndex);
        }
        [Test] public void MoveAndDisposeRespectPhaseAndResources()
        {
            var item = Stock(Curse.Cold, 0); var actions = game.State.actions;
            game.Move(item.id, 1); Assert.AreEqual(actions - 1, game.State.actions);
            game.Dispose(item.id); game.Dispose(item.id); Assert.IsEmpty(game.State.stock);
            Assert.AreEqual(220, game.State.cash);
        }
        [Test] public void DifficultyAndExpensesPlateauWithoutAnEndNight()
        {
            game.State.night = 10000; game.State.cash = 100000;
            game.CloseNight(); game.NextNight();
            Assert.AreEqual(10001, game.State.night); Assert.LessOrEqual(game.Difficulty, 1.8);
            Assert.LessOrEqual(game.State.due, 49); Assert.AreEqual(NightPhase.Reception, game.State.phase);
        }
        [Test] public void ImprovementsHaveFiniteLevelsAndNewRunsResetThem()
        {
            game.State.cash = 10000;
            foreach (Upgrade type in Enum.GetValues(typeof(Upgrade)))
            { game.BuyUpgrade(type); game.BuyUpgrade(type); var cash = game.State.cash; game.BuyUpgrade(type); Assert.AreEqual(cash, game.State.cash); }
            Assert.AreEqual(16, game.Capacity); Assert.AreEqual(14, game.MaxActions);
            Assert.AreEqual(0, ShopSimulation.NewRun(1).State.shelves);
        }
        [Test] public void AtomicSaveRestoresBackupWhenPrimaryIsCorrupt()
        {
            var folder = Path.Combine(Application.dataPath, "../Temp/EndlessTests", Guid.NewGuid().ToString("N"));
            try
            {
                var storage = new RunStorage(folder); storage.SaveRun(game.State);
                game.Reject(); storage.SaveRun(game.State);
                File.WriteAllText(Path.Combine(folder, "endless-run.json"), "corrupt");
                var restored = storage.LoadRun(); Assert.IsNotNull(restored); Assert.AreEqual(0, restored.customerIndex);
                StringAssert.Contains("backup", storage.Notice);
                storage.SaveRun(restored); Assert.IsNotNull(storage.LoadRun());
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
        [Test] public void ValidationRejectsDuplicateSlotsAndUnsupportedVersions()
        {
            Assert.IsTrue(RunStorage.Validate(game.State)); Stock(Curse.None, 0); Stock(Curse.None, 0);
            Assert.IsFalse(RunStorage.Validate(game.State)); game.State.stock.Clear(); game.State.version = 999;
            Assert.IsFalse(RunStorage.Validate(game.State));
        }
        [Test] public void HundredsOfNightsRemainPlayableWithAConservativeStrategy()
        {
            foreach (var seed in new[] { 1, 12345, 67890 })
            {
                game = ShopSimulation.NewRun(seed);
                for (var night = 0; night < 200; night++)
                {
                    while (game.Customer != null)
                    {
                        var customer = game.Customer;
                        if (customer.item.suspicious || game.State.cash < 70 || game.State.stock.Count >= 8) { game.Reject(); continue; }
                        game.Diagnose(customer.item.id, Diagnostic.Loupe); game.UseArgument();
                        game.Offer(ShopSimulation.Value(customer.item) * 60 / 100);
                        if (!customer.accepted) { game.Reject(); continue; }
                        game.Buy();
                        var item = game.State.stock.Last(); game.ListItem(item.id, ShopSimulation.Value(item) * 88 / 100, true);
                    }
                    Assert.IsTrue(RunStorage.Validate(game.State), $"Seed {seed}, night {night}");
                    game.CloseNight();
                    if (game.State.phase == NightPhase.Settlement)
                    {
                        foreach (var item in game.State.stock.ToArray()) { game.Liquidate(item.id); if (game.State.cash >= game.State.due) break; }
                        game.PayExpenses();
                    }
                    Assert.AreEqual(NightPhase.Results, game.State.phase, $"Seed {seed}, night {night}, cash {game.State.cash}");
                    game = Reload(); game.NextNight();
                }
                Assert.AreEqual(200, game.State.completedNights);
            }
        }
    }
}
