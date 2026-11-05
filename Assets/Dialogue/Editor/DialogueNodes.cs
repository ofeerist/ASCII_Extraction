using System;
using Unity.GraphToolkit.Editor;

namespace Pawnshop.Dialogue.Editor
{
    [Serializable]
    public abstract class DialogueNode : Node
    {
        public T Option<T>(string name)
        {
            var option = GetNodeOptionByName(name);
            return option != null && option.TryGetValue<T>(out var value) ? value : default;
        }
        protected static void Input(IPortDefinitionContext context) => context.AddInputPort("In").WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        protected static void Output(IPortDefinitionContext context, string name = "Next") => context.AddOutputPort(name).WithConnectorUI(PortConnectorUI.Arrowhead).Build();
    }
    [Serializable]
    public sealed class StartNode : DialogueNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context) => Output(context);
    }
    [Serializable]
    public sealed class LineNode : DialogueNode
    {
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<string>("SpeakerKey").WithDisplayName("Speaker localization key").ShowInInspectorOnly();
            context.AddOption<string>("TextKey").WithDisplayName("Text localization key").ShowInInspectorOnly();
            context.AddOption<string>("Speaker").WithDisplayName("Speaker").WithDefaultValue("Customer");
            context.AddOption<string>("Text").WithDisplayName("Text").WithDefaultValue("Good evening.");
        }
        protected override void OnDefinePorts(IPortDefinitionContext context) { Input(context); Output(context); }
    }
    [Serializable]
    public sealed class ChoiceNode : DialogueNode
    {
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<int>("Count").WithDisplayName("Answers (2-6)").WithDefaultValue(2).Delayed();

            for (var i = 0; i < 6; i++)
            {
                context.AddOption<string>($"AnswerKey{i}").WithDisplayName($"Answer {i + 1} localization key").ShowInInspectorOnly();
                context.AddOption<string>($"Answer{i}").WithDisplayName($"Answer {i + 1}").WithDefaultValue($"Option {i + 1}");
            }
        }
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            Input(context);
            for (var i = 0; i < Math.Clamp(Option<int>("Count"), 2, 6); i++) Output(context, $"Answer{i}");
        }
    }
    [Serializable]
    public abstract class VariableNode : DialogueNode
    {
        protected void VariableOptions(IOptionDefinitionContext context)
        {
            context.AddOption<string>("Variable").WithDisplayName("Blackboard variable").WithDefaultValue("local.defectFound");
            context.AddOption<ValueKind>("Type").WithDisplayName("Value type");
            context.AddOption<bool>("Boolean").WithDisplayName("Boolean value");
            context.AddOption<int>("Integer").WithDisplayName("Integer value");
            context.AddOption<string>("Text").WithDisplayName("Text value");
        }
        public DialogueValue Value => Option<ValueKind>("Type") switch
        {
            ValueKind.Boolean => DialogueValue.Bool(Option<bool>("Boolean")),
            ValueKind.Integer => DialogueValue.Int(Option<int>("Integer")),
            _ => DialogueValue.Text(Option<string>("Text"))
        };
    }
    [Serializable]
    public sealed class ConditionNode : VariableNode
    {
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            VariableOptions(context);
            context.AddOption<Comparison>("Comparison").WithDisplayName("Comparison");
        }
        protected override void OnDefinePorts(IPortDefinitionContext context) { Input(context); Output(context, "True"); Output(context, "False"); }
    }
    [Serializable]
    public sealed class SetVariableNode : VariableNode
    {
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            VariableOptions(context);
            context.AddOption<Mutation>("Mutation").WithDisplayName("Operation");
        }
        protected override void OnDefinePorts(IPortDefinitionContext context) { Input(context); Output(context); }
    }
    [Serializable]
    public sealed class CommandNode : DialogueNode
    {
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<string>("Command").WithDisplayName("Command").WithDefaultValue("OpenBargain");
            context.AddOption<string>("Argument").WithDisplayName("Argument");
        }
        protected override void OnDefinePorts(IPortDefinitionContext context) { Input(context); Output(context); }
    }
    [Serializable]
    public sealed class EndNode : DialogueNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context) => Input(context);
    }
}
