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
}
