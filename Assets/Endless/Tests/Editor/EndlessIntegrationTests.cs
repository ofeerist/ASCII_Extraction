using System;
using System.IO;
using NUnit.Framework;
using Pawnshop.Dialogue;
using UnityEditor;
using UnityEngine;

namespace Pawnshop.Endless.Tests
{
    public sealed class EndlessIntegrationTests
    {
        [TestCase(CustomerKind.Desperate)]
        [TestCase(CustomerKind.Dealer)]
        [TestCase(CustomerKind.Collector)]
        [TestCase(CustomerKind.Stranger)]
        public void AuthoredConversationRestoresChoicesWithoutChangingEconomy(CustomerKind kind)
        {
            var asset = AssetDatabase.LoadAssetAtPath<DialogueAsset>($"Assets/Endless/Dialogues/{kind}.dialogue");
            Assert.IsNotNull(asset); Assert.IsTrue(asset.IsValid, string.Join("; ", asset.errors));
            foreach (var answer in new[] { 0, 1 })
            {
                var run = ShopSimulation.NewRun(12345).State;
                var customer = run.customers[0];
                var initialCash = run.cash;
                var session = VisitorConversation.Restore(asset, customer);
                Assert.AreEqual(StepKind.Line, session.Current.kind);
                Assert.IsNotEmpty(session.Current.textKey);
                VisitorConversation.Advance(asset, customer, -1);
                VisitorConversation.Advance(asset, customer, answer);
                run = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(run));
                customer = run.customers[0];
                session = VisitorConversation.Restore(asset, customer);
                Assert.AreEqual(StepKind.Line, session.Current.kind);
                StringAssert.EndsWith(answer == 0 ? ".origin" : ".reason", session.Current.textKey);
                VisitorConversation.Advance(asset, customer, -1);
                Assert.IsTrue(VisitorConversation.Restore(asset, customer).Finished);
                Assert.AreEqual(initialCash, run.cash);
                Assert.AreEqual(0, run.customerIndex);
            }
        }
        [TestCase(NightPhase.Reception)]
        [TestCase(NightPhase.Settlement)]
        [TestCase(NightPhase.Results)]
        [TestCase(NightPhase.Bankrupt)]
        public void DiskRoundTripPreservesEveryPhase(NightPhase phase)
        {
            var folder = Path.Combine(Application.dataPath, "../Temp/EndlessTests", Guid.NewGuid().ToString("N"));
            try
            {
                var simulation = ShopSimulation.NewRun(51);
                if (phase == NightPhase.Settlement || phase == NightPhase.Bankrupt) simulation.State.cash = 0;
                if (phase != NightPhase.Reception) simulation.CloseNight();
                if (phase == NightPhase.Bankrupt) simulation.ConfirmBankruptcy();
                Assert.AreEqual(phase, simulation.State.phase);
                var storage = new RunStorage(folder);
                storage.SaveRun(simulation.State);
                var restored = storage.LoadRun();
                Assert.AreEqual(JsonUtility.ToJson(simulation.State), JsonUtility.ToJson(restored));
                if (phase == NightPhase.Results || phase == NightPhase.Bankrupt)
                {
                    var cash = restored.cash;
                    new ShopSimulation(restored).PayExpenses();
                    Assert.AreEqual(cash, restored.cash);
                }
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
        [Test] public void RecordsAndDiscoveriesPersistWithoutGivingNewRunBonuses()
        {
            var folder = Path.Combine(Application.dataPath, "../Temp/EndlessTests", Guid.NewGuid().ToString("N"));
            try
            {
                var game = ShopSimulation.NewRun(51); game.State.discoveries.Add(Curse.Hunger); game.CloseNight();
                var profile = new PlayerProfile(); RunStorage.UpdateRecords(profile, game.State, game.Capital);
                var storage = new RunStorage(folder); storage.SaveProfile(profile);
                var restored = storage.LoadProfile(); Assert.AreEqual(1, restored.bestNights); Assert.Contains(Curse.Hunger, restored.discoveries);
                var fresh = ShopSimulation.NewRun(51); Assert.AreEqual(220, fresh.State.cash); Assert.AreEqual(0, fresh.State.expert);
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
    }
}
