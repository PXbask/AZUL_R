using cfg;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataMgr : MonoSingleton<DataMgr>
{
    public Tables Table { get; private set; }
    private void Start()
    {
        Table = new Tables(LoadJson);
    }

    /// <summary>
    /// 从 StreamingAssets 中读取指定文件名的 JSON，返回 JArray
    /// </summary>
    private JArray LoadJson(string fileName)
    {
        string path = Application.streamingAssetsPath + "/GameCfg/" + fileName + ".json";
        if (!File.Exists(path))
        {
            Debug.LogError($"[DataMgr] JSON 文件不存在: {path}");
            return new JArray();
        }
        string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
        return JArray.Parse(json);
    }
}
