using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对象池配置项
/// </summary>
[System.Serializable]
public class PoolConfigEntry
{
    [Tooltip("对象池 Key，用于 Spawn/Recycle 时索引")]
    public string keyName;

    [Tooltip("对应的预制体")]
    public GameObject prefab;

    [Tooltip("池内最大缓存数量，超出后多余对象将被直接销毁")]
    public int maxSize = 20;
}

/// <summary>
/// 对象池配置文件（ScriptableObject）
/// 在 Project 窗口右键 -> Create -> Pool -> PoolConfig 创建资产
/// </summary>
[CreateAssetMenu(menuName = "Pool/PoolConfig", fileName = "PoolConfig")]
public class PoolConfig : ScriptableObject
{
    public List<PoolConfigEntry> entries = new List<PoolConfigEntry>();
}
