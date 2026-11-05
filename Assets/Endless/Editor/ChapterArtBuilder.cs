using UnityEditor;
using UnityEngine;
namespace Pawnshop.Endless.Editor
{
    public static class ChapterArtBuilder
    {
        [MenuItem("Tools/Pawnshop/Prepare Chapter Art")]
        public static void Build()
        {
            const string path="Assets/Endless/Resources/ChapterArt.asset";
            var art=AssetDatabase.LoadAssetAtPath<ChapterArt>(path);
            if(art==null){art=ScriptableObject.CreateInstance<ChapterArt>();AssetDatabase.CreateAsset(art,path);}
            string[] sources={
                "Assets/Scenes/ZNS3D/Prefabs/clock_wall.prefab",
                "Assets/Snowforge_Necklace/Necklace/Prefabs/Necklace.prefab",
                "Assets/Scenes/ZNS3D/Prefabs/book_1.prefab",
                "Assets/Medieval_Gold/1_coin.FBX",
                "",
                "Assets/URP GanzSe Free Character Accessories/Prefabs/FCA_Ring_Type1_Shape1_Color1.prefab",
                "",
                "Assets/Scenes/ZNS3D/Prefabs/book_4.prefab",
                "",
                "Assets/Medieval_Gold/1_coin.FBX"
            };
            art.props=new GameObject[10];
            for(int i=0;i<sources.Length;i++)if(sources[i]!="") art.props[i]=AssetDatabase.LoadAssetAtPath<GameObject>(sources[i]);
            EditorUtility.SetDirty(art);AssetDatabase.SaveAssets();
        }
    }
}
