using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace Pawnshop.Dialogue.Editor
{
    public sealed class DialoguePreviewWindow : EditorWindow
    {
        [SerializeField] DialogueAsset source;
        DialogueAsset snapshot;
        DialogueSession session;
        readonly Dictionary<string, DialogueValue> globals = new Dictionary<string, DialogueValue>();
        readonly List<string> history = new List<string>();
        Vector2 scroll;
        string error;

        [MenuItem("Window/Pawnshop/Dialogue Preview")]
        public static void Open()
        {
            var window = GetWindow<DialoguePreviewWindow>("Dialogue Preview");
            var selected = AssetDatabase.LoadAssetAtPath<DialogueAsset>(AssetDatabase.GetAssetPath(Selection.activeObject));
            if (selected != null) { window.source = selected; window.Prepare(); }
        }
        void OnEnable() => minSize = new Vector2(480, 480);
        void OnDisable() { if (snapshot != null) DestroyImmediate(snapshot); }
        void OnGUI()
        {
            EditorGUILayout.LabelField("Dialogue Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select a .dialogue asset. Preparing saves the graph. Global variables persist between conversations; local variables reset.", MessageType.Info);
            EditorGUI.BeginChangeCheck();
            source = (DialogueAsset)EditorGUILayout.ObjectField("Graph", source, typeof(DialogueAsset), false);
            if (EditorGUI.EndChangeCheck()) { session = null; history.Clear(); error = null; }
            if (GUILayout.Button("Use Project Selection"))
            {
                source = AssetDatabase.LoadAssetAtPath<DialogueAsset>(AssetDatabase.GetAssetPath(Selection.activeObject));
                session = null; history.Clear(); error = null;
            }
            using (new EditorGUI.DisabledScope(source == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Prepare / Restart")) Prepare();
                if (GUILayout.Button("Reset Run")) { globals.Clear(); Prepare(); }
                EditorGUILayout.EndHorizontal();
            }
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (session == null) return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Variables (editable before and during playback)", EditorStyles.boldLabel);
            foreach (var variable in snapshot.variables)
            {
                var value = session.Get(variable.name);
                EditorGUI.BeginChangeCheck();
                switch (value.kind)
                {
                    case ValueKind.Boolean: value.boolean = EditorGUILayout.Toggle(variable.name, value.boolean); break;
                    case ValueKind.Integer: value.integer = EditorGUILayout.IntField(variable.name, value.integer); break;
                    case ValueKind.Text: value.text = EditorGUILayout.TextField(variable.name, value.text); break;
                }
                if (EditorGUI.EndChangeCheck()) session.Set(variable.name, value);
            }
            EditorGUILayout.Space();
            if (history.Count == 0 && !session.Finished && session.Current == null)
            {
                if (GUILayout.Button("Start Conversation")) { session.Start(); Record(); }
            }
            else if (session.Finished)
            {
                EditorGUILayout.HelpBox(session.Error ?? "Conversation finished.", session.Error == null ? MessageType.Info : MessageType.Error);
            }
            else if (session.Current != null)
            {
                var step = session.Current;
                if (step.kind == StepKind.Choice)
                {
                    for (var i = 0; i < step.answers.Length; i++)
                        if (GUILayout.Button(step.answers[i]))
                        { history.Add("You: " + step.answers[i]); session.Choose(i); Record(); break; }
                }
                else if (step.kind == StepKind.Command)
                {
                    EditorGUILayout.HelpBox($"Command: {step.command} ({step.argument}). Preview does not execute gameplay actions.", MessageType.Info);
                    if (GUILayout.Button("Simulate Command Completion")) { session.Continue(); Record(); }
                }
                else if (GUILayout.Button("Continue")) { session.Continue(); Record(); }
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var line in history) EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }
        void Prepare()
        {
            session = null; history.Clear(); error = null;
            if (snapshot != null) DestroyImmediate(snapshot);
            if (source == null) return;
            try
            {
                var path = AssetDatabase.GetAssetPath(source);
                var graph = GraphDatabase.LoadGraph<DialogueGraph>(path);
                GraphDatabase.SaveGraphIfDirty(graph);
                snapshot = DialogueCompiler.Compile(graph);
                if (!snapshot.IsValid) { error = string.Join("\n", snapshot.errors); return; }
                session = new DialogueSession(snapshot, globals);
            }
            catch (Exception e) { error = e.Message; }
        }
        void Record()
        {
            var step = session.Current;
            if (step?.kind == StepKind.Line) history.Add($"{step.speaker}: {step.text}");
            else if (step?.kind == StepKind.Command) history.Add($"[Command {step.command}: {step.argument}]");
            else if (step?.kind == StepKind.Choice) history.Add("[Choose an answer]");
            Repaint();
        }
    }
}
