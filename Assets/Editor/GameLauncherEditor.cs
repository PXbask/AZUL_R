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

        string batPath = "C:\\Users\\PXbask\\OneDrive\\桌面\\Server\\start_server.bat";
        RunBatFile(batPath);
    }

    [MenuItem(MenuPath, true)]
    private static bool LaunchGameValidate()
    {
        // Play 模式中禁用该菜单项
        return !EditorApplication.isPlaying;
    }

    private static void RunBatFile(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            Debug.LogError($"[GameLauncherEditor] .bat file not found at path: {path}");
            return;
        }
        //如果该文件运行中，先杀掉该进程
        var processName = System.IO.Path.GetFileNameWithoutExtension(path);
        var runningProcesses = System.Diagnostics.Process.GetProcessesByName(processName);
        foreach (var process in runningProcesses)
        {
            try
            {
                process.Kill();
                Debug.Log($"[GameLauncherEditor] Killed running process: {process.ProcessName} (ID: {process.Id})");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameLauncherEditor] Failed to kill process: {process.ProcessName} (ID: {process.Id}). Exception: {ex.Message}");
            }
        }
        try
        {
            System.Diagnostics.Process.Start(path);
            Debug.Log($"[GameLauncherEditor] Launched .bat file: {path}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameLauncherEditor] Failed to launch .bat file: {path}. Exception: {ex.Message}");
        }
    }
}
#endif
