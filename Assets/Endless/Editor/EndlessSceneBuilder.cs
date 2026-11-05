using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Pawnshop.Endless.Editor
{
    public static class EndlessSceneBuilder
    {
        public const string ScenePath = "Assets/Endless/EndlessShop.unity";
        [MenuItem("Tools/Pawnshop/Open Endless Mode")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Create();
            EditorSceneManager.OpenScene(AssetDatabase.LoadAssetAtPath<SceneAsset>(HouseSceneBuilder.ScenePath) != null ? HouseSceneBuilder.ScenePath : ScenePath);
        }
        public static string Create()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return ScenePath;
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var cameraObject = new GameObject("Shop Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.04f, 0.06f, 0.06f); camera.orthographic = true;
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1280, 720);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            AssetDatabase.CreateAsset(settings, "Assets/Endless/UI/EndlessPanel.asset");
            var host = new GameObject("Endless Shop");
            var document = host.AddComponent<UIDocument>(); document.panelSettings = settings;
            var game = host.AddComponent<EndlessGame>();
            var serialized = new SerializedObject(game);
            serialized.FindProperty("theme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Endless/UI/Endless.uss");
            serialized.FindProperty("terminalFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/Endless/UI/Fonts/NotoSansMono-Regular.ttf");
            var dialogues = EndlessDialogueBuilder.Create();
            var references = serialized.FindProperty("visitorDialogues");
            references.arraySize = dialogues.Length;
            for (var i = 0; i < dialogues.Length; i++) references.GetArrayElementAtIndex(i).objectReferenceValue = dialogues[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(x => x.path != ScenePath)).ToArray();
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.SaveAssets();
            return ScenePath;
        }
    }
}
