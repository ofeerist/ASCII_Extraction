using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Pawnshop.Dialogue.Tests
{
    public class DialogueSessionTests
    {
        DialogueAsset asset;
        [SetUp] public void Setup() { asset = ScriptableObject.CreateInstance<DialogueAsset>(); asset.entry = 0; }
        [TearDown] public void Teardown() => UnityEngine.Object.DestroyImmediate(asset);
        void Variable(string name, DialogueValue value) => asset.variables.Add(new VariableDefinition { name = name, initialValue = value });

        [Test]
        public void ChoiceAndConditionUseUpdatedValueAndWaitForInput()
        {
            Variable("local.defect", DialogueValue.Bool(false));
            asset.steps.AddRange(new[] {
                new DialogueStep { kind = StepKind.Line, text = "Watch", next = 1 },
                new DialogueStep { kind = StepKind.Choice, answers = new[] { "Inspect", "Leave" }, destinations = new[] { 2, 5 } },
                new DialogueStep { kind = StepKind.SetVariable, variable = "local.defect", value = DialogueValue.Bool(true), next = 3 },
                new DialogueStep { kind = StepKind.Condition, variable = "local.defect", value = DialogueValue.Bool(true), next = 4, otherwise = 5 },
                new DialogueStep { kind = StepKind.Line, text = "Defect found", next = 5 },
                new DialogueStep { kind = StepKind.End }
            });
            var session = new DialogueSession(asset);
            session.Start(); Assert.AreEqual("Watch", session.Current.text);
            session.Continue(); Assert.AreEqual(StepKind.Choice, session.Current.kind);
            Assert.Throws<InvalidOperationException>(() => session.Continue());
            Assert.Throws<ArgumentOutOfRangeException>(() => session.Choose(2));
            session.Choose(0); Assert.AreEqual("Defect found", session.Current.text);
            session.Continue(); Assert.IsTrue(session.Finished); Assert.IsNull(session.Error);
        }
        [Test]
        public void NewClientResetsLocalsButPreservesRunVariables()
        {
            Variable("global.reputation", DialogueValue.Int(0)); Variable("local.defect", DialogueValue.Bool(false));
            asset.steps.Add(new DialogueStep { kind = StepKind.End });
            var globals = new Dictionary<string, DialogueValue>();
            var first = new DialogueSession(asset, globals);
            first.Set("global.reputation", DialogueValue.Int(3)); first.Set("local.defect", DialogueValue.Bool(true));
            var second = new DialogueSession(asset, globals);
            Assert.AreEqual(3, second.Get("global.reputation").integer); Assert.IsFalse(second.Get("local.defect").boolean);
        }
        [Test]
        public void ActionCycleStopsWithError()
        {
            asset.steps.Add(new DialogueStep { kind = StepKind.Start, next = 0 });
            var session = new DialogueSession(asset); session.Start();
            Assert.IsTrue(session.Finished); StringAssert.Contains("1024", session.Error);
        }
        [Test]
        public void CommandsWaitForExplicitCompletionAndDoNotRepeat()
        {
            asset.steps.Add(new DialogueStep { kind = StepKind.Command, command = "OpenBargain", next = 1 });
            asset.steps.Add(new DialogueStep { kind = StepKind.End });
            var session = new DialogueSession(asset); session.Start();
            Assert.AreEqual("OpenBargain", session.Current.command); Assert.IsFalse(session.Finished);
            session.Continue(); Assert.IsTrue(session.Finished);
            Assert.Throws<InvalidOperationException>(() => session.Continue());
        }
        [Test]
        public void IntegerOverflowFailsWithoutChangingMoney()
        {
            Variable("global.cash", DialogueValue.Int(int.MaxValue));
            asset.steps.Add(new DialogueStep { kind = StepKind.SetVariable, variable = "global.cash", mutation = Mutation.Add, value = DialogueValue.Int(1), next = 1 });
            asset.steps.Add(new DialogueStep { kind = StepKind.End });
            var session = new DialogueSession(asset); session.Start();
            Assert.IsNotNull(session.Error); Assert.AreEqual(int.MaxValue, session.Get("global.cash").integer);
        }
        [Test]
        public void MissingConnectionFailsInsteadOfFinishingSilently()
        {
            asset.steps.Add(new DialogueStep { kind = StepKind.Start });
            var session = new DialogueSession(asset); session.Start(); Assert.IsNotNull(session.Error);
        }
        [Test]
        [TestCase(0, "eighty")]
        [TestCase(1, "One hundred coins")]
        public void SavedSampleImportsAndRunsBothBranches(int answer, string expected)
        {
            const string path = "Assets/Dialogue/Samples/WatchCustomer.dialogue";
            UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
            var sample = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueAsset>(path);
            Assert.IsNotNull(sample);
            Assert.IsTrue(sample.IsValid, string.Join("; ", sample.errors));
            var session = new DialogueSession(sample);
            session.Start(); session.Continue(); session.Choose(answer);
            StringAssert.Contains(expected, session.Current.text);
            session.Continue(); Assert.AreEqual("OpenBargain", session.Current.command);
            session.Continue(); Assert.IsTrue(session.Finished); Assert.IsNull(session.Error);
        }
        [Test]
        public void PlayerDoesNotDispatchCommandTwiceWhenUiAdvancesSynchronously()
        {
            asset.steps.Add(new DialogueStep { kind = StepKind.Line, next = 1 });
            asset.steps.Add(new DialogueStep { kind = StepKind.Command, command = "OpenBargain", next = 2 });
            asset.steps.Add(new DialogueStep { kind = StepKind.End });
            var owner = new GameObject("Dialogue test");
            try
            {
                var player = owner.AddComponent<DialoguePlayer>();
                var calls = 0;
                player.onChanged.AddListener(() => { if (player.Session.Current.kind == StepKind.Line) player.Continue(); });
                player.onCommand.AddListener((_, __) => calls++);
                player.Begin(asset);
                Assert.AreEqual(1, calls);
                Assert.AreEqual(StepKind.Command, player.Session.Current.kind);
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }
        [Test]
        public void IncorrectTypesAndUnknownVariablesAreRejected()
        {
            Variable("local.defect", DialogueValue.Bool(false)); asset.steps.Add(new DialogueStep { kind = StepKind.End });
            var session = new DialogueSession(asset);
            Assert.Throws<ArgumentException>(() => session.Set("local.defect", DialogueValue.Int(1)));
            Assert.Throws<ArgumentException>(() => session.Get("typo"));
            Assert.Throws<ArgumentException>(() => DialogueSession.Evaluate(DialogueValue.Bool(true), Comparison.Greater, DialogueValue.Bool(false)));
        }
    }
}
