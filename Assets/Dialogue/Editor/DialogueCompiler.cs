using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Pawnshop.Dialogue.Editor
{
    public static class DialogueCompiler
    {
        public static DialogueAsset Compile(DialogueGraph graph)
        {
            var result = ScriptableObject.CreateInstance<DialogueAsset>();
            var nodes = graph.GetNodes().OfType<DialogueNode>().ToList();
            var variables = new Dictionary<string, VariableDefinition>(StringComparer.Ordinal);
            foreach (var variable in graph.GetVariables())
            {
                var definition = new VariableDefinition { name = variable.name };
                if (string.IsNullOrWhiteSpace(variable.name) || variables.ContainsKey(variable.name))
                { result.errors.Add($"Empty or duplicate variable name: {variable.name}"); continue; }
                if (variable.dataType == typeof(bool)) { variable.TryGetDefaultValue<bool>(out var value); definition.initialValue = DialogueValue.Bool(value); }
                else if (variable.dataType == typeof(int)) { variable.TryGetDefaultValue<int>(out var value); definition.initialValue = DialogueValue.Int(value); }
                else if (variable.dataType == typeof(string)) { variable.TryGetDefaultValue<string>(out var value); definition.initialValue = DialogueValue.Text(value); }
                else { result.errors.Add($"Variable '{variable.name}': only Boolean, Integer and String are supported."); continue; }
                variables.Add(variable.name, definition);
                result.variables.Add(definition);
            }
            var starts = nodes.OfType<StartNode>().ToList();
            if (starts.Count != 1) result.errors.Add("The graph must contain exactly one Start Node.");
            else result.entry = nodes.IndexOf(starts[0]);
            if (!nodes.OfType<EndNode>().Any()) result.errors.Add("Add an End Node.");
            foreach (var other in graph.GetNodes().Where(n => !(n is DialogueNode) && !(n is IVariableNode)))
                result.errors.Add($"Unsupported node: {other.GetType().Name}. Use Dialogue nodes.");

            int Target(DialogueNode node, string portName)
            {
                var port = node.GetOutputPortByName(portName);
                var connections = new List<IPort>();
                port?.GetConnectedPorts(connections);
                if (connections.Count != 1)
                {
                    result.errors.Add($"{node.GetType().Name} [{nodes.IndexOf(node)}], {portName}: exactly one connection is required.");
                    return -1;
                }
                var target = connections[0].GetNode() as DialogueNode;
                var index = target == null ? -1 : nodes.IndexOf(target);
                if (index < 0) result.errors.Add($"{portName}: the connection must lead to a dialogue node.");
                return index;
            }
            foreach (var node in nodes)
            {
                var step = new DialogueStep();
                switch (node)
                {
                    case StartNode _: step.kind = StepKind.Start; step.next = Target(node, "Next"); break;
                    case LineNode _:
                        step.kind = StepKind.Line;
                        step.speakerKey = node.Option<string>("SpeakerKey"); step.textKey = node.Option<string>("TextKey");
                        step.speaker = node.Option<string>("Speaker"); step.text = node.Option<string>("Text");
                        if (string.IsNullOrWhiteSpace(step.text)) result.errors.Add("Line Node: line text is empty.");
                        step.next = Target(node, "Next"); break;
                    case ChoiceNode _:
                        step.kind = StepKind.Choice;
                        var count = node.Option<int>("Count");
                        if (count < 2 || count > 6) result.errors.Add("Choice Node: answer count must be between 2 and 6.");
                        count = Math.Clamp(count, 2, 6);
                        step.answerKeys = new string[count]; step.answers = new string[count]; step.destinations = new int[count];
                        for (var i = 0; i < count; i++)
                        {
                            step.answerKeys[i] = node.Option<string>($"AnswerKey{i}");
                            step.answers[i] = node.Option<string>($"Answer{i}");
                            if (string.IsNullOrWhiteSpace(step.answers[i])) result.errors.Add($"Choice Node: answer {i + 1} is empty.");
                            step.destinations[i] = Target(node, $"Answer{i}");
                        }
                        break;
                    case ConditionNode condition:
                        step.kind = StepKind.Condition; step.variable = node.Option<string>("Variable"); step.value = condition.Value;
                        step.comparison = node.Option<Comparison>("Comparison");
                        if (step.value.kind != ValueKind.Integer && step.comparison != Comparison.Equal && step.comparison != Comparison.NotEqual)
                            result.errors.Add("Condition Node: ordered comparisons require Integer.");
                        step.next = Target(node, "True"); step.otherwise = Target(node, "False"); break;
                    case SetVariableNode action:
                        step.kind = StepKind.SetVariable; step.variable = node.Option<string>("Variable"); step.value = action.Value;
                        step.mutation = node.Option<Mutation>("Mutation");
                        if (step.mutation != Mutation.Set && step.value.kind != ValueKind.Integer)
                            result.errors.Add("Set Variable Node: Add/Subtract require Integer.");
                        step.next = Target(node, "Next"); break;
                    case CommandNode _:
                        step.kind = StepKind.Command; step.command = node.Option<string>("Command"); step.argument = node.Option<string>("Argument");
                        if (string.IsNullOrWhiteSpace(step.command)) result.errors.Add("Command Node: enter a command.");
                        step.next = Target(node, "Next"); break;
                    case EndNode _: step.kind = StepKind.End; break;
                }
                if (node is VariableNode)
                {
                    if (string.IsNullOrWhiteSpace(step.variable) || !variables.TryGetValue(step.variable, out var definition))
                        result.errors.Add($"{node.GetType().Name}: variable '{step.variable}' is not declared in the Blackboard.");
                    else if (definition.initialValue.kind != step.value.kind)
                        result.errors.Add($"{node.GetType().Name}: value type does not match '{step.variable}'.");
                }
                result.steps.Add(step);
            }
            return result;
        }
    }

    [ScriptedImporter(2, DialogueGraph.Extension)]
    public sealed class DialogueImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            var graph = GraphDatabase.LoadGraphForImporter<DialogueGraph>(context.assetPath);
            if (graph == null) { context.LogImportError("Could not load the dialogue graph."); return; }
            var result = DialogueCompiler.Compile(graph);
            result.name = System.IO.Path.GetFileNameWithoutExtension(context.assetPath);
            context.AddObjectToAsset("Dialogue", result);
            context.SetMainObject(result);

        }
    }
}
