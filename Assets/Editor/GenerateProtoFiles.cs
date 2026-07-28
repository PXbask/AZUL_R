using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GenerateProtoFiles
{
    private const string MenuPath = "Game/▶ Generate Proto Files";

    [MenuItem(MenuPath)]
    private static void GenerateProtoFilesFunc()
    {
        string batPath = ".\\Protocol\\gproto.bat";
        PXEditorUtility.RunBatFile(batPath);
    }
}
