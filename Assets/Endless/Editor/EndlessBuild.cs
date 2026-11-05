using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Pawnshop.Endless.Editor
{
    public static class EndlessBuild
    {
        [Serializable] sealed class BuildSummary
        {
            public string result;
            public string target;
            public string path;
            public int errors;
            public int warnings;
            public double seconds;
        }
        [MenuItem("Tools/Pawnshop/Build Endless for Linux")]
        public static void Linux() => Build(BuildTarget.StandaloneLinux64);
        [MenuItem("Tools/Pawnshop/Build Endless for Windows")]
        public static void Windows() => Build(BuildTarget.StandaloneWindows64);
        public static string Build(BuildTarget target)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before building.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target)) throw new InvalidOperationException("Install the target platform module first.");
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(HouseSceneBuilder.ScenePath) != null ? HouseSceneBuilder.ScenePath : EndlessSceneBuilder.Create();
            var folder = target == BuildTarget.StandaloneWindows64 ? "Windows" : "Linux";
            var path = "Builds/Endless/" + folder + "/PawnshopOfTheDamned" + (folder == "Windows" ? ".exe" : ".x86_64");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { scene }, locationPathName = path, target = target, options = BuildOptions.None });
            var summary = report.summary;
            var result = new BuildSummary { result = summary.result.ToString(), target = target.ToString(), path = path, errors = (int)summary.totalErrors, warnings = (int)summary.totalWarnings, seconds = summary.totalTime.TotalSeconds };
            var json = JsonUtility.ToJson(result, true);
            File.WriteAllText("Temp/endless-build-" + folder.ToLowerInvariant() + ".json", json);
            if (summary.result == BuildResult.Succeeded)
            {
                File.Copy(scene == HouseSceneBuilder.ScenePath ? "Assets/Endless/HOUSE_README.txt" : "Assets/Endless/README.txt", Path.Combine(Path.GetDirectoryName(path), "README.txt"), true);
                File.Copy("Assets/Endless/QUEST_README.txt", Path.Combine(Path.GetDirectoryName(path), "QUEST_README.txt"), true);
                File.Copy("Assets/Endless/UI/Fonts/LICENSE.txt", Path.Combine(Path.GetDirectoryName(path), "FONT-LICENSE.txt"), true);
            }
            if (summary.result != BuildResult.Succeeded) Debug.LogError(json);
            return json;
        }
    }
}
