#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GameLauncherEditor
{
    private const string MenuPath = "Game/▶ Launch Game (GameLauncher)";

    [MenuItem(MenuPath)]
    private static void LaunchGame()
    {
        // 找到 GameLauncher 场景资产
        string[] guids = AssetDatabase.FindAssets($"t:Scene {SceneStatic.GameLauncherSceneName}");
        if (guids.Length == 0)
        {
            Debug.LogError($"[GameLauncherEditor] Scene '{SceneStatic.GameLauncherSceneName}' not found in project.");
            return;
        }

        string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);

        // 若当前有未保存修改，提示保存
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // 切换到 GameLauncher 场景
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 进入 Play 模式
        EditorApplication.isPlaying = true;

        Debug.Log($"[GameLauncherEditor] Opened '{scenePath}' and started Play mode.");
    }

    [MenuItem(MenuPath, true)]
    private static bool LaunchGameValidate()
    {
        // Play 模式中禁用该菜单项
        return !EditorApplication.isPlaying;
    }
}
#endif
