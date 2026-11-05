using System;
using UnityEngine;
using UnityEngine.Events;

namespace Pawnshop.Dialogue
{

    public sealed class DialoguePlayer : MonoBehaviour
    {
        [SerializeField] DialogueAsset dialogue;
        public UnityEvent onChanged = new UnityEvent();
        public UnityEvent onFinished = new UnityEvent();
        public UnityEvent<string, string> onCommand = new UnityEvent<string, string>();
        public UnityEvent<string> onError = new UnityEvent<string>();
        public DialogueSession Session { get; private set; }
        int publishVersion;
        readonly System.Collections.Generic.Dictionary<string, DialogueValue> globals = new System.Collections.Generic.Dictionary<string, DialogueValue>();

        public void Begin() => Begin(dialogue);
        public void Begin(DialogueAsset asset)
        {
            if (Session != null && !Session.Finished) throw new InvalidOperationException("Finish the current conversation first.");
            Session = new DialogueSession(asset, globals);
            Session.Start(); Publish();
        }
        public void Continue() { Session.Continue(); Publish(); }
        public void Choose(int index) { Session.Choose(index); Publish(); }
        public void ResetRun() { publishVersion++; Session = null; globals.Clear(); }
        void Publish()
        {
            var version = ++publishVersion;
            if (Session.Error != null) { onError.Invoke(Session.Error); return; }
            if (Session.Finished) { onFinished.Invoke(); return; }
            onChanged.Invoke();

            if (version != publishVersion) return;
            if (Session.Current?.kind == StepKind.Command)
                onCommand.Invoke(Session.Current.command, Session.Current.argument);
        }
    }
}
