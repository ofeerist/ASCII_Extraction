using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace Pawnshop.Dialogue.Editor
{
    [Serializable, Graph(Extension)]
    public sealed class DialogueGraph : Graph
    {
        public const string Extension = "dialogue";
        [MenuItem("Assets/Create/Pawnshop/Dialogue Graph")]
        static void Create() => GraphDatabase.PromptInProjectBrowserToCreateNewAsset<DialogueGraph>("New Dialogue");

        public override void OnGraphChanged(GraphLogger logger)
        {
            base.OnGraphChanged(logger);
            var result = DialogueCompiler.Compile(this);
            foreach (var error in result.errors) logger.LogError(error, this);
            UnityEngine.Object.DestroyImmediate(result);
        }
    }
}
