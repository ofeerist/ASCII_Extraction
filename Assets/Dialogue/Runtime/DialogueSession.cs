using System;
using System.Collections.Generic;

namespace Pawnshop.Dialogue
{

    public sealed class DialogueSession
    {
        readonly DialogueAsset asset;
        readonly IDictionary<string, DialogueValue> globals;
        readonly Dictionary<string, DialogueValue> locals = new Dictionary<string, DialogueValue>();
        readonly Dictionary<string, ValueKind> schema = new Dictionary<string, ValueKind>();
        int cursor;
        bool started;
        public DialogueStep Current { get; private set; }
        public bool Finished { get; private set; }
        public string Error { get; private set; }
        public int CurrentIndex => cursor;

        public DialogueSession(DialogueAsset asset, IDictionary<string, DialogueValue> globals = null)
        {
            this.asset = asset ?? throw new ArgumentNullException(nameof(asset));
            if (!asset.IsValid) throw new ArgumentException("Dialogue has validation errors.", nameof(asset));
            this.globals = globals ?? new Dictionary<string, DialogueValue>();
            foreach (var definition in asset.variables)
            {
                schema.Add(definition.name, definition.initialValue.kind);
                var store = Store(definition.name);
                if (!store.TryGetValue(definition.name, out var existing)) store.Add(definition.name, definition.initialValue);
                else if (existing.kind != definition.initialValue.kind)
                    throw new InvalidOperationException($"Variable '{definition.name}' changed type between graphs.");
            }
            cursor = asset.entry;
        }

        IDictionary<string, DialogueValue> Store(string name) => name.StartsWith("global.", StringComparison.Ordinal) ? globals : locals;
        public DialogueValue Get(string name)
        {
            if (!schema.ContainsKey(name)) throw new ArgumentException($"Unknown variable: {name}");
            return Store(name)[name];
        }
        public void Set(string name, DialogueValue value)
        {
            if (!schema.TryGetValue(name, out var kind) || kind != value.kind)
                throw new ArgumentException($"Unknown variable or incorrect type: {name}");
            Store(name)[name] = value;
        }
        public void Start()
        {
            if (started) throw new InvalidOperationException("This conversation has already started.");
            started = true;
            Pump();
        }
        public void Continue()
        {
            if (Finished || Current == null || (Current.kind != StepKind.Line && Current.kind != StepKind.Command))
                throw new InvalidOperationException("The dialogue is not waiting for Continue.");
            cursor = Current.next;
            Pump();
        }
        public void Choose(int answer)
        {
            if (Finished || Current?.kind != StepKind.Choice) throw new InvalidOperationException("The dialogue is not waiting for a choice.");
            if (answer < 0 || answer >= Current.destinations.Length) throw new ArgumentOutOfRangeException(nameof(answer));
            cursor = Current.destinations[answer];
            Pump();
        }
        void Pump()
        {
            Current = null;
            try
            {

                for (var budget = 0; budget < 1024; budget++)
                {
                    if (cursor < 0 || cursor >= asset.steps.Count) throw new InvalidOperationException("Missing dialogue connection.");
                    var step = asset.steps[cursor];
                    switch (step.kind)
                    {
                        case StepKind.Start: cursor = step.next; break;
                        case StepKind.Line:
                        case StepKind.Choice:
                        case StepKind.Command: Current = step; return;
                        case StepKind.End: Finished = true; return;
                        case StepKind.Condition:
                            cursor = Evaluate(Get(step.variable), step.comparison, step.value) ? step.next : step.otherwise;
                            break;
                        case StepKind.SetVariable:
                            var value = step.value;
                            if (step.mutation != Mutation.Set)
                            {
                                var previous = Get(step.variable);
                                if (previous.kind != ValueKind.Integer || value.kind != ValueKind.Integer)
                                    throw new InvalidOperationException("Only integers support Add/Subtract.");
                                value = DialogueValue.Int(step.mutation == Mutation.Add
                                    ? checked(previous.integer + value.integer) : checked(previous.integer - value.integer));
                            }
                            Set(step.variable, value);
                            cursor = step.next;
                            break;
                        default: throw new InvalidOperationException("Unsupported dialogue step.");
                    }
                }
                throw new InvalidOperationException("Dialogue exceeded 1024 automatic steps. Check for a cycle without a line or choice.");
            }
            catch (Exception e)
            {
                Error = e.Message;
                Finished = true;
            }
        }
        public static bool Evaluate(DialogueValue left, Comparison comparison, DialogueValue right)
        {
            if (left.kind != right.kind) throw new ArgumentException("Comparison types differ.");
            if (left.kind != ValueKind.Integer && comparison != Comparison.Equal && comparison != Comparison.NotEqual)
                throw new ArgumentException("Ordered comparisons require integers.");
            var order = left.kind == ValueKind.Integer ? left.integer.CompareTo(right.integer)
                : left.kind == ValueKind.Boolean ? left.boolean.CompareTo(right.boolean)
                : string.Compare(left.text ?? "", right.text ?? "", StringComparison.Ordinal);
            switch (comparison)
            {
                case Comparison.Equal: return order == 0;
                case Comparison.NotEqual: return order != 0;
                case Comparison.Greater: return order > 0;
                case Comparison.GreaterOrEqual: return order >= 0;
                case Comparison.Less: return order < 0;
                case Comparison.LessOrEqual: return order <= 0;
                default: throw new ArgumentOutOfRangeException(nameof(comparison));
            }
        }
    }
}
