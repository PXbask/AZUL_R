using AZUL;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TableData
{
    /// <summary>
    /// 当前游戏的总玩家数
    /// </summary>
    public int totalPlayerCount;

    /// <summary>
    /// 工厂圆盘信息
    /// </summary>
    public List<List<PlaceTokenAreaData>> factories;

    /// <summary>
    /// 中央区域信息
    /// </summary>
    public List<PlaceTokenAreaData> center;

    /// <summary>
    /// 当前要行动的那个人他所有版图信息
    /// </summary>
    public PlayerBoardData me;

    /// <summary>
    /// 其他人的版图信息
    /// </summary>
    public List<PlayerBoardData> opponents;

    /// <summary>
    /// 游戏盒内的剩余砖块信息
    /// </summary>
    public List<TokenNumberData> remainTokens;

    /// <summary>
    /// 弃牌区的砖块信息
    /// </summary>
    public List<TokenNumberData> loseTokens;
}

/// <summary>
/// 游戏盒内棋子信息
/// </summary>
[SerializeField]
public class TokenNumberData
{
    /// <summary>
    /// 棋子颜色
    /// </summary>
    public PieceColorType color;

    /// <summary>
    /// 剩余数量
    /// </summary>
    public int number;
}

[Serializable]
public class PlayerBoardData
{
    /// <summary>
    /// 玩家当前分数
    /// </summary>
    public int score;

    /// <summary>
    /// 玩家座位号
    /// </summary>
    public int seatId;

    /// <summary>
    /// 玩家客户端ID
    /// </summary>
    public int clientId;

    /// <summary>
    /// 花砖列信息，顺序从上到下，从右到左
    /// </summary>
    public List<List<PlaceTokenAreaData>> manualAreas;

    /// <summary>
    /// 砖墙信息，顺序从上到下，从左到右
    /// </summary>
    public List<List<PlaceTokenAreaData>> coloredAreas;

    /// <summary>
    /// 地板列信息，从左到右
    /// </summary>
    public List<PlaceTokenAreaData> loseAreas;
}

[Serializable]
public class PlaceTokenAreaData
{
    /// <summary>
    /// 该区域是否为空
    /// </summary>
    public bool empty;

    /// <summary>
    /// 如果不为空，该区域的颜色
    /// </summary>
    public PieceColorType color;
}
