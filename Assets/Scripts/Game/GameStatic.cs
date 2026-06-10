using System.Collections;
using System.Collections.Generic;
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
}
