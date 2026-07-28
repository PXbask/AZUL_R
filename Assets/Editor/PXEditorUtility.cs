using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class PXEditorUtility
{
    public static void RunBatFile(string path)
    {
        string fullPath = System.IO.Path.GetFullPath(path);
        if (!System.IO.File.Exists(fullPath))
        {
            Debug.LogError($"[GameLauncherEditor] .bat file not found at path: {fullPath}");
            return;
        }
        //如果该文件运行中，先杀掉该进程
        var processName = System.IO.Path.GetFileNameWithoutExtension(fullPath);
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
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                // ✅ /c 执行完自动关闭，路径加引号防止空格问题
                Arguments = $"/c \"{fullPath}\"",
                // bat 文件所在目录作为工作目录，确保 bat 内的相对路径正确
                WorkingDirectory = System.IO.Path.GetDirectoryName(fullPath),
                UseShellExecute = true,
            };

            System.Diagnostics.Process.Start(psi);
            Debug.Log($"[PXEditorUtility] Launched: {fullPath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PXEditorUtility] Failed to launch {fullPath}: {ex.Message}");
        }
    }
}
