using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pawnshop.Dialogue
{
    public enum ValueKind { Boolean, Integer, Text }
    public enum Comparison { Equal, NotEqual, Greater, GreaterOrEqual, Less, LessOrEqual }
    public enum Mutation { Set, Add, Subtract }
    public enum StepKind { Start, Line, Choice, Condition, SetVariable, Command, End }

    [Serializable]
    public struct DialogueValue
    {
        public ValueKind kind;
        public bool boolean;
        public int integer;
        public string text;
        public static DialogueValue Bool(bool value) => new DialogueValue { kind = ValueKind.Boolean, boolean = value };
        public static DialogueValue Int(int value) => new DialogueValue { kind = ValueKind.Integer, integer = value };
        public static DialogueValue Text(string value) => new DialogueValue { kind = ValueKind.Text, text = value ?? "" };
        public override string ToString() => kind == ValueKind.Boolean ? boolean.ToString() : kind == ValueKind.Integer ? integer.ToString() : text ?? "";
    }

    [Serializable]
    public sealed class VariableDefinition
    {
        public string name;
        public DialogueValue initialValue;
    }

    [Serializable]
    public sealed class DialogueStep
    {
        public StepKind kind;
        public string speaker;
        public string speakerKey;
        public string textKey;
        [TextArea] public string text;
        public string variable;
        public DialogueValue value;
        public Comparison comparison;
        public Mutation mutation;
        public string command;
        public string argument;
        public int next = -1;
        public int otherwise = -1;
        public string[] answerKeys = Array.Empty<string>();
        public string[] answers = Array.Empty<string>();
        public int[] destinations = Array.Empty<int>();
    }


    public sealed class DialogueAsset : ScriptableObject
    {
        public int entry = -1;
        public List<VariableDefinition> variables = new List<VariableDefinition>();
        public List<DialogueStep> steps = new List<DialogueStep>();
        public List<string> errors = new List<string>();
        public bool IsValid => errors.Count == 0 && entry >= 0 && entry < steps.Count;
    }
}
