using NUnit.Framework;
namespace Pawnshop.Dialogue.Tests
{
    public class DialogueLocalizationTests
    {
        sealed class Resolver : IDialogueTextResolver
        {
            public string Resolve(string key, string englishFallback) => key == "greeting" ? "Translated greeting" : null;
        }
        [Test]
        public void ResolvesTextAndFallsBackForMissingEntries()
        {
            var step = new DialogueStep { text = "Good evening.", textKey = "greeting", speaker = "Visitor", speakerKey = "unknown" };
            Assert.AreEqual("Translated greeting", step.GetText(new Resolver()));
            Assert.AreEqual("Good evening.", step.GetText());
            Assert.AreEqual("Visitor", step.GetSpeaker(new Resolver()));
        }
        [Test]
        public void OlderGraphsWithoutAnswerKeysUseEnglish()
        {
            var step = new DialogueStep { answers = new[] { "Inspect" } };
            Assert.AreEqual("Inspect", step.GetAnswer(0, new Resolver()));
        }
        [Test]
        public void SourceTextEditsDoNotChangeTranslationIdentity()
        {
            var step = new DialogueStep { text = "Hello", textKey = "greeting" };
            step.text = "Good evening";
            Assert.AreEqual("Translated greeting", step.GetText(new Resolver()));
        }
    }
}
