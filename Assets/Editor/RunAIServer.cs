using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class RunAIServer
{
    private const string MenuPath = "Game/▶ Run AI Server";

    [MenuItem(MenuPath)]
    private static void RunAIServerFunc()
    {
        string batPath = "C:\\Users\\PXbask\\OneDrive\\桌面\\Server\\start_server.bat";
        PXEditorUtility.RunBatFile(batPath);
    }
}
