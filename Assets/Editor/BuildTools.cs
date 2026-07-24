using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BuildTools : MonoBehaviour
{
    [MenuItem("Build/Build Exe")]
    public static void BuildExe()
    {
        // 从 Build Settings 中读取已启用的场景
        var sceneList = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                sceneList.Add(scene.path);
        }

        if (sceneList.Count == 0)
        {
            Debug.LogError("Build Failed: No scenes found in Build Settings! Please add scenes via File > Build Settings.");
            return;
        }

        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        string outputDir = Application.dataPath + "/../Bin/" + timeStamp;

        BuildPlayerOptions opt = new BuildPlayerOptions();
        opt.scenes = sceneList.ToArray();
        opt.locationPathName = outputDir + "/test.exe";
        opt.target = BuildTarget.StandaloneWindows64;   
        opt.options = BuildOptions.None;

        BuildPipeline.BuildPlayer(opt);

        Debug.Log("Build App Done! Output: " + outputDir);
    }

    [MenuItem("Build/Quick Build")]
    public static void QuickBuildExeToLocalFolder()
    {
        // 从 Build Settings 中读取已启用的场景
        var sceneList = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                sceneList.Add(scene.path);
        }
        if (sceneList.Count == 0)
        {
            Debug.LogError("Build Failed: No scenes found in Build Settings! Please add scenes via File > Build Settings.");
            return;
        }

        string outputDir = Application.dataPath + "/../Bin/local";
        string outputExePath = outputDir + "/AZUL_R.exe";

        //删除文件夹下的所有文件
        if (System.IO.Directory.Exists(outputDir))
        {
            System.IO.Directory.Delete(outputDir, true);
            Debug.Log("Deleted existing output directory: " + outputDir);
        }
        System.IO.Directory.CreateDirectory(outputDir);
        Debug.Log("Created new output directory: " + outputDir);

        BuildPlayerOptions opt = new BuildPlayerOptions();
        opt.scenes = sceneList.ToArray();
        opt.locationPathName = outputExePath;
        opt.target = BuildTarget.StandaloneWindows64;
        opt.options = BuildOptions.None;
        BuildPipeline.BuildPlayer(opt);

        Debug.Log("Build App Done! Output: " + outputExePath);

        //定位到输出文件
        EditorUtility.RevealInFinder(outputExePath);
    }
}
