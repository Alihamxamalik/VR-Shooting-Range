using UnityEditor;

namespace RangeControl.Editor
{
    /// <summary>
    /// One menu entry that takes a fresh clone to a playable range: project settings, art, data, prefabs, scene.
    /// Each step is also on the Tools/Range Control menu by itself.
    /// </summary>
    public static class RangeBuildMenu
    {
        [MenuItem("Tools/Range Control/Build Everything", priority = 1)]
        public static void BuildEverything()
        {
            ProjectSetup.Configure();
            ArtBuilder.Build();
            DataBuilder.Build();
            PrefabBuilder.Build();
            SceneBuilder.Build();
        }
    }
}
