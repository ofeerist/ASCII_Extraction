using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Pawnshop.Dialogue.Tests
{
    public class GraphFramingTests
    {
        const string Sample = "Assets/Dialogue/Samples/WatchCustomer.dialogue";

        static EditorWindow GraphWindow() => Resources.FindObjectsOfTypeAll<EditorWindow>()
            .First(w => w.GetType().FullName == "Unity.GraphToolkit.Editor.Implementation.GraphViewEditorWindowImp");
        static VisualElement GraphView(EditorWindow window) => window.rootVisualElement.Query<VisualElement>().ToList()
            .First(v => v.GetType().GetMethod("CalculateRectToFitAll") != null);
        static void Frame(VisualElement view) => view.GetType().GetMethod("CalculateRectToFitAll").Invoke(view, null);

        [UnityTest]
        public IEnumerator FrameAllIgnoresDetachedNodeAndClearsScratchList()
        {
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<DialogueAsset>(Sample));
            yield return null;
            yield return null;
            var view = GraphView(GraphWindow());
            var container = (VisualElement)view.GetType().GetProperty("ContentViewContainer").GetValue(view);
            var node = container.Query<VisualElement>().ToList().First(v =>
                v.GetType().GetProperty("Model")?.GetValue(v)?.GetType().Name == "UserNodeModelImp" && v.parent != null);
            var parent = node.parent;
            var index = parent.IndexOf(node);
            node.RemoveFromHierarchy();
            try
            {
                Assert.Throws<ArgumentNullException>(() => node.parent.ChangeCoordinatesTo(container, node.layout));
                Assert.DoesNotThrow(() => Frame(view));
                var ownerType = view.GetType().GetMethod("CalculateRectToFitAll").DeclaringType;
                var scratch = (IList)ownerType.GetField("k_CalculateRectToFitAllAllUIs", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                Assert.AreEqual(0, scratch.Count);
            }
            finally { parent.Insert(index, node); }
            Assert.DoesNotThrow(() => Frame(view));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NodeTextInputsReserveReadableWidth()
        {
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<DialogueAsset>(Sample));
            yield return null;
            yield return null;
            yield return null;
            var fields = GraphView(GraphWindow()).Query<TextField>().ToList().Where(f =>
            {
                for (var parent = f.parent; parent != null; parent = parent.parent)
                    if (parent.ClassListContains("ge-node__node-options")) return true;
                return false;
            }).ToList();
            Assert.IsNotEmpty(fields);
            foreach (var field in fields)
            {
                var input = field.Q(className: "unity-base-field__input");
                Assert.IsNotNull(input);
                Assert.GreaterOrEqual(input.layout.width, 220f, field.label);
            }
        }

        [UnityTest]
        public IEnumerator ReopeningGraphCanFrameAll()
        {
            for (var i = 0; i < 3; i++)
            {
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<DialogueAsset>(Sample));
                yield return null;
                yield return null;
                Assert.DoesNotThrow(() => Frame(GraphView(GraphWindow())));
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
