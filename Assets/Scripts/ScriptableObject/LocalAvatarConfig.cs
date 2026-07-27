using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 本地头像配置项
/// </summary>
[System.Serializable]
public class LocalAvatarConfigEntry
{
    [Tooltip("对应的Id")]
    public string id;

    [Tooltip("头像图片")]
    public Sprite avatarSprite;
}

/// <summary>
/// 本地头像配置文件（ScriptableObject）
/// 在 Project 窗口右键 -> Create -> Cfg -> LocalAvatarConfig 创建资产
/// </summary>
[CreateAssetMenu(menuName = "Cfg/LocalAvatarConfig", fileName = "LocalAvatarConfig")]
public class LocalAvatarConfig : ScriptableObject
{
    public List<LocalAvatarConfigEntry> entries = new List<LocalAvatarConfigEntry>();
}
