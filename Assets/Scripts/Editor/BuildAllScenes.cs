using UnityEditor;

namespace Game.EditorTools
{
    /// <summary>
    /// Rebuilds both MainMenu and TestPlayground scenes in one step.
    /// </summary>
    public static class BuildAllScenes
    {
        [MenuItem("Tools/Game/Rebuild All Scenes (Menu + Playground)")]
        public static void RebuildAll()
        {
            BuildMainMenuScene.Build();
            BuildTestScene.Build();
        }
    }
}
