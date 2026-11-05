using System.Collections.Generic;
using Pawnshop.Dialogue;

namespace Pawnshop.Endless
{
    public static class VisitorConversation
    {
        public static DialogueSession Restore(DialogueAsset asset, CustomerState customer)
        {
            customer.conversationActions ??= new List<int>();
            var session = new DialogueSession(asset);
            session.Start();
            foreach (var action in customer.conversationActions)
            {
                if (!CanAdvance(session, action)) break;
                if (action < 0) session.Continue(); else session.Choose(action);
            }
            return session;
        }
        static bool CanAdvance(DialogueSession session, int action) => !session.Finished &&
            ((action == -1 && session.Current?.kind == StepKind.Line) ||
             (action >= 0 && session.Current?.kind == StepKind.Choice && action < session.Current.destinations.Length));
        public static string Advance(DialogueAsset asset, CustomerState customer, int action)
        {
            var session = Restore(asset, customer);
            if (!CanAdvance(session, action) || customer.conversationActions.Count >= 64) return "The conversation has ended.";
            customer.conversationActions.Add(action);
            return "A customer's story is not verified evidence. Use diagnostics to check the item.";
        }
    }
}
