using System;
using System.Linq;
using System.Reflection;
using Pawnshop.Dialogue;
using Pawnshop.Dialogue.Editor;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace Pawnshop.Endless.Editor
{
    public static class EndlessDialogueBuilder
    {
        public static DialogueAsset[] Create()
        {
            const string folder = "Assets/Endless/Dialogues";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Endless", "Dialogues");
            var result = new DialogueAsset[4];
            var introductions = new[] {
                "There is a train at six. I have counted the money in my pocket three times. It never becomes enough.",
                "I buy rooms, not objects. Sometimes a room comes with things the inventory does not mention.",
                "I kept careful records. Dates, owners, auction prices. Then the handwriting in the last column stopped looking like mine.",
                "You opened the door before I knocked. That happens more often than you think."
            };
            var origins = new[] {
                "My landlord left it on the table when he disappeared. He owed me a deposit. That is the whole story I am willing to sell.",
                "An estate on the north bank. There were two signatures on the receipt, but only one person in the house.",
                "An auction with no catalogue. I was the only bidder. Someone behind the curtain kept raising the price anyway.",
                "A place that no longer has a street name. You would not find it by walking."
            };
            var reasons = new[] {
                "I can sleep on the train. If I stay here, I will spend another night listening to the walls. Check the object if you must.",
                "Turnover. Nothing sentimental about it. I recommend that you perform your own tests; my guarantee ends at this counter.",
                "I wanted to own beautiful things. I did not want them to recognize me. Your instruments will tell you more than my story can.",
                "You are better equipped to keep it than I am. Whether you should keep it is a different question."
            };
            for (var i = 0; i < result.Length; i++)
            {
                var kind = ((CustomerKind)i).ToString();
                var path = folder + "/" + kind + ".dialogue";
                result[i] = AssetDatabase.LoadAssetAtPath<DialogueAsset>(path);
                if (result[i] != null) continue;
                var graph = GraphDatabase.CreateGraph<DialogueGraph>(path);
                var model = typeof(Graph).GetField("m_Implementation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(graph);
                T Add<T>(float x, float y) where T : DialogueNode, new()
                {
                    var node = new T();
                    model.GetType().GetMethod("CreateNodeModel").Invoke(model, new object[] { node, new Vector2(x, y) });
                    return node;
                }
                void Set(DialogueNode node, string name, string value)
                {
                    var option = node.GetNodeOptionByName(name);
                    var port = option.GetType().GetProperty("PortModel").GetValue(option);
                    var constant = port.GetType().GetProperty("EmbeddedValue").GetValue(port);
                    constant.GetType().GetProperty("ObjectValue").SetValue(constant, value);
                }
                void Wire(DialogueNode from, string output, DialogueNode to)
                {
                    model.GetType().GetMethods().First(m => m.Name == "CreateWire" && m.GetParameters().Length == 3)
                        .Invoke(model, new object[] { to.GetInputPortByName("In"), from.GetOutputPortByName(output), default(Hash128) });
                }
                var prefix = "dialogue.endless." + kind.ToLowerInvariant() + ".";
                LineNode Line(float x, float y, string id, string text)
                {
                    var node = Add<LineNode>(x, y);
                    Set(node, "Speaker", kind); Set(node, "SpeakerKey", "characters." + kind.ToLowerInvariant() + ".name");
                    Set(node, "Text", text); Set(node, "TextKey", prefix + id);
                    return node;
                }
                var start = Add<StartNode>(0, 0);
                var introduction = Line(260, 0, "introduction", introductions[i]);
                var choice = Add<ChoiceNode>(780, 0);
                Set(choice, "Answer0", "Where did you get it?"); Set(choice, "AnswerKey0", "dialogue.endless.ask_origin");
                Set(choice, "Answer1", "Why sell it tonight?"); Set(choice, "AnswerKey1", "dialogue.endless.ask_reason");
                var origin = Line(1300, -200, "origin", origins[i]);
                var reason = Line(1300, 260, "reason", reasons[i]);
                var end = Add<EndNode>(1850, 0);
                Wire(start, "Next", introduction); Wire(introduction, "Next", choice);
                Wire(choice, "Answer0", origin); Wire(choice, "Answer1", reason);
                Wire(origin, "Next", end); Wire(reason, "Next", end);
                EditorUtility.SetDirty((UnityEngine.Object)model.GetType().GetProperty("GraphObject").GetValue(model));
                GraphDatabase.SaveGraphIfDirty(graph);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                result[i] = AssetDatabase.LoadAssetAtPath<DialogueAsset>(path);
                if (result[i] == null || !result[i].IsValid) throw new InvalidOperationException("Failed to compile " + path);
            }
            return result;
        }
    }
}
