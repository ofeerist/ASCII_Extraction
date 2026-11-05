namespace Pawnshop.Dialogue
{
    public interface IDialogueTextResolver
    {
        string Resolve(string key, string englishFallback);
    }

    public static class DialogueLocalization
    {
        public static string Resolve(IDialogueTextResolver resolver, string key, string englishFallback)
        {
            if (resolver == null || string.IsNullOrWhiteSpace(key)) return englishFallback ?? "";
            return resolver.Resolve(key, englishFallback ?? "") ?? englishFallback ?? "";
        }

        public static string GetText(this DialogueStep step, IDialogueTextResolver resolver = null)
            => Resolve(resolver, step.textKey, step.text);

        public static string GetSpeaker(this DialogueStep step, IDialogueTextResolver resolver = null)
            => Resolve(resolver, step.speakerKey, step.speaker);

        public static string GetAnswer(this DialogueStep step, int index, IDialogueTextResolver resolver = null)
            => Resolve(resolver, index < step.answerKeys.Length ? step.answerKeys[index] : null, step.answers[index]);
    }
}
