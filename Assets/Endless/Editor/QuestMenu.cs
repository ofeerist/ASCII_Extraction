using UnityEditor;
namespace Pawnshop.Endless.Editor
{
    public static class QuestMenu
    {
        [MenuItem("Tools/Pawnshop/Open Quest Mode")]
        public static void Open() => HouseSceneBuilder.Open();
    }
}
