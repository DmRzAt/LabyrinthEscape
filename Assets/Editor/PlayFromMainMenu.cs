using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PlayFromMainMenu
{
    const string MenuScenePath = "Assets/Scenes/MainMenuScene.unity";

    static PlayFromMainMenu()
    {
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath);
        if (scene != null)
            EditorSceneManager.playModeStartScene = scene;
    }
}
