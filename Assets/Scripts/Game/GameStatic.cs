using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class GameStatic
{
    public static readonly int MinPlayerNum = 2;
    public static readonly int MaxPlayerNum = 4;

    public static readonly ushort NgoDefaultPort = 7777;
    public static readonly ushort AiDefaultPort = 9999;

    public static readonly string LocalIp = "127.0.0.1";

    public static readonly int CardNumPerDisk = 4;

    public static readonly float TokenGoToAreaAnimInterval = 0.5f;

    public static readonly int NonePlayerSeatId = -1;
    public static readonly int MidTableRowId = -1;
    public static readonly int LoseAreaRowId = -1;

    public static readonly int NullPieceId = -1;

    public static string DefaultPlayerName
    {
        get
        {
            //获取当前时间戳精确到毫秒
            long timestamp = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
            return timestamp.ToString();
        }
    }
    public static readonly string DefaultAvatarId = "默认头像";
    public static readonly bool DefaultEnableRuntimeLog = false;

#if UNITY_EDITOR
    public static readonly string DefaultRuntimeLogPath = Application.dataPath + "/../Bin/logs";
#else
    public static readonly string DefaultRuntimeLogPath = Path.GetDirectoryName(Application.dataPath);
#endif
}
