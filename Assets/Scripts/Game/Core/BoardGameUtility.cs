using AZUL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using static Unity.VisualScripting.Dependencies.Sqlite.SQLite3;

public static class BoardGameUtility
{
    /// <summary>
    /// 检查玩家的某一行的彩色区域是否有指定颜色的棋子
    /// </summary>
    public static bool PlayerBoardHasColorInColoredAreaInRow(PlayerBoard boardGame, int row, PieceColorType color)
    {
        var areaList = boardGame.RightPlaceTokenAreas[row];
        for (int column = 0; column < areaList.Count; column++)
        {
            if (areaList[column].ColorType == color)
            {
                return !areaList[column].IsEmpty();
            }
        }
        //Debug.LogError($"Color {color} not found in the colored area of row {row} on the player's board.");
        return false;
    }

    /// <summary>
    /// 检查玩家的某一行的手动区域是否是不同颜色的棋子行
    /// </summary>
    public static bool PlayerBoardDiffColorInManualAreaInRow(PlayerBoard playerBoard, int row, PieceColorType pieceTokenType)
    {
        var areaList = playerBoard.LeftPlaceTokenAreas[row];
        var firstArea = areaList[0];
        if (!firstArea.IsEmpty())
        {
            var token = firstArea.Token as NormalPieceToken;
            return token.PieceData.PieceTokenType != (int)pieceTokenType;
        }
        return false;
    }

    /// <summary>
    /// 获取桌子中心牌区的首位tokrn
    /// </summary>
    public static NormalPieceToken GetFirstTokenInMidArea()
    {
        var dic = BoardGameMgr.Instance.GameController.MidTablePlaceAreas;
        foreach (var pair in dic)
        {
            if (!pair.Value.IsEmpty())
            {
                var token = pair.Value.Token as NormalPieceToken;
                if (token == null)
                {
                    Debug.LogError("中间区域内存在非棋子Token，数据异常。");
                    continue;
                }
                if (token.PieceData.PieceTokenType == (int)PieceColorType.SpecialToken)
                {
                    return token;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 获取指定行的减分区域空间的所有空闲TokenArea
    /// </summary>
    public static List<LosePlaceTokenArea> GetEmptyTokenAreaInLoseArea(PlayerBoard board)
    {
        var result = new List<LosePlaceTokenArea>();
        foreach (var area in board.LosePlaceTokenAreas)
        {
            if (area.IsEmpty())
            {
                result.Add(area);
            }
        }
        return result;
    }

    /// <summary>
    /// 获取减分区的最后一个区域
    /// </summary>
    public static LosePlaceTokenArea GetLastAreaInLoseArea(PlayerBoard playerBoard)
    {
        return playerBoard.LosePlaceTokenAreas[^1];
    }

    /// <summary>
    /// 获取与指定PieceToken颜色相同的所有PieceToken，这些PieceToken必须位于中部区域内。
    /// </summary>
    public static List<NormalPieceToken> GetAllColorTypeTokenInMidTable(PieceColorType colorType)
    {
        var result = new List<NormalPieceToken>();
        foreach (var pair in BoardGameMgr.Instance.GameController.MidTablePlaceAreas)
        {
            var area = pair.Value;
            if (!area.IsEmpty())
            {
                var token = area.Token as NormalPieceToken;
                if (token == null)
                {
                    Debug.LogError("中间区域内存在非棋子Token，数据异常。");
                    continue;
                }
                if (token.PieceData.PieceTokenType == (int)colorType)
                {
                    result.Add(token);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// 获取指定行的手动区域空间的所有TokenArea
    /// </summary>
    public static List<NormalPlaceTokenArea> GetEmptyTokenAreaInManualAreaInRow(PlayerBoard playerBoard, int row)
    {
        var result = new List<NormalPlaceTokenArea>();
        var areaList = playerBoard.LeftPlaceTokenAreas[row];
        for (int column = 0; column < areaList.Count; column++)
        {
            if (areaList[column].IsEmpty())
            {
                result.Add(areaList[column]);
            }
        }
        return result;
    }

    /// <summary>
    /// 获取与指定PieceToken颜色相同的所有PieceToken，这些PieceToken必须位于工厂区域内。
    /// </summary>
    public static List<NormalPieceToken> GetAllColorTypeTokenInFactory(PieceColorType colorType, int factoryId, out List<NormalPieceToken> remainTokens)
    {
        var result = new List<NormalPieceToken>();
        remainTokens = new List<NormalPieceToken>();
        if (factoryId >= 0 && factoryId < BoardGameMgr.Instance.GameController.FactoryDiskDic.Count)
        {
            var factory = BoardGameMgr.Instance.GameController.FactoryDiskDic[factoryId];
            foreach (var area in factory.PlaceTokenAreas)
            {
                if (!area.IsEmpty())
                {
                    var token = area.Token as NormalPieceToken;
                    if (token == null)
                    {
                        Debug.LogError("工厂区域内存在非棋子Token，数据异常。");
                        continue;
                    }
                    if (token.PieceData.PieceTokenType == (int)colorType)
                    {
                        result.Add(token);
                    }
                    else
                    {
                        remainTokens.Add(token);
                    }
                }
            }
            return result;
        }
        Debug.LogError("棋子不在工厂区域内，无法获取相同颜色的棋子。");
        return null;
    }

    /// <summary>
    /// 获取中间区域count个空闲位置
    /// </summary>
    public static List<NormalPlaceTokenArea> GetEmptyTokenAreaInMidArea(int count)
    {
        var result = new List<NormalPlaceTokenArea>(count);
        var areaList = BoardGameMgr.Instance.GameController.MidTablePlaceAreas;
        int addedCount = 0;
        for (int column = 0; column < areaList.Count; column++)
        {
            if (areaList[column].IsEmpty())
            {
                result.Add(areaList[column]);
                addedCount++;
            }
            if (addedCount == count)
            {
                break;
            }
        }
        return result;
    }

    public static bool FactorysEmpty()
    {
        for (int i = 0; i < BoardGameMgr.Instance.GameController.FactoryDiskDic.Count; i++)
        {
            var factory = BoardGameMgr.Instance.GameController.FactoryDiskDic[i];
            for (int j = 0; j < factory.PlaceTokenAreas.Count; j++)
            {
                if (!factory.PlaceTokenAreas[j].IsEmpty())
                {
                    return false;
                }
            }
        }
        return true;
    }

    public static bool MidTableEmpty()
    {
        var areaList = BoardGameMgr.Instance.GameController.MidTablePlaceAreas;
        for (int i = 0; i < areaList.Count; i++)
        {
            if (!areaList[i].IsEmpty())
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 获取手动区域所有填满的行
    /// </summary>
    public static List<List<NormalPlaceTokenArea>> GetFilledRowInManualArea(PlayerBoard board)
    {
        var result = new List<List<NormalPlaceTokenArea>>();
        foreach (var row in board.LeftPlaceTokenAreas)
        {
            bool isFilled = true;
            foreach (var area in row)
            {
                if (area.IsEmpty())
                {
                    isFilled = false;
                    break;
                }
            }
            if (isFilled)
            {
                result.Add(row);
            }
        }
        return result;
    }

    /// <summary>
    /// 获取颜色区某一行的带颜色放置区
    /// </summary>
    public static ColoredPlaceTokenArea GetColoredTileInColoredArea(PlayerBoard board, int row, int pieceTokenType)
    {
        foreach (var area in board.RightPlaceTokenAreas[row])
        {
            if ((int)area.ColorType == pieceTokenType)
            {
                return area;
            }
        }
        Debug.LogError("找不到对应的颜色");
        return null;
    }

    public static int CalculateScorePieceMoveToColoredArea(PlayerBoard playerBoard, ColoredPlaceTokenArea coloredPlaceTokenArea)
    {
        int continuousRow = 0;
        int continuousCol = 0;
        var data = coloredPlaceTokenArea.GetPositionData();
        //先计算竖排,计算上下组成的最大的连续棋子数
        for (int i = data.Row - 1; i >= 0; i--)
        {
            var area = GetColoredTokenAreaByPostion(playerBoard, i, data.Column);
            if (area.IsEmpty())
            {
                break;
            }
            continuousRow++;
        }
        for (int i = data.Row; i < playerBoard.RightPlaceTokenAreas.Count; i++)
        {
            var area = GetColoredTokenAreaByPostion(playerBoard, i, data.Column);
            if (area.IsEmpty())
            {
                break;
            }
            continuousRow++;
        }

        //再计算横排
        for (int i = data.Column - 1; i >= 0; i--)
        {
            var area = GetColoredTokenAreaByPostion(playerBoard, data.Row, i);
            if (area.IsEmpty())
            {
                break;
            }
            continuousCol++;
        }
        for (int i = data.Column; i < playerBoard.RightPlaceTokenAreas[data.Row].Count; i++)
        {
            var area = GetColoredTokenAreaByPostion(playerBoard, data.Row, i);
            if (area.IsEmpty())
            {
                break;
            }
            continuousCol++;
        }

        //如果是单独的一个棋子，则得1分
        if (continuousCol == 1)
        {
            return continuousRow;
        }
        if (continuousRow == 1)
        {
            return continuousCol;
        }

        return continuousRow + continuousCol;
    }

    /// <summary>
    /// 根据行列获取颜色区的放置区，如果行列越界则返回null
    /// </summary>
    private static ColoredPlaceTokenArea GetColoredTokenAreaByPostion(PlayerBoard playerBoard, int row, int column)
    {
        if (row < 0 || row >= playerBoard.RightPlaceTokenAreas.Count) return null;
        if (column < 0 || column >= playerBoard.RightPlaceTokenAreas[row].Count) return null;
        return playerBoard.RightPlaceTokenAreas[row][column];
    }

    public static void PlayerAddScore(PlayerBoard playerBoard, int score)
    {
        if (playerBoard == null) return;
        if (playerBoard.Score + score < 0)
        {
            playerBoard.Score = 0;
        }
        playerBoard.Score = playerBoard.Score + score;
    }

    /// <summary>
    /// 获取某玩家的减分区所有已放置棋子的区域
    /// </summary>
    public static List<LosePlaceTokenArea> GetAllFilledAreaInLoseArea(PlayerBoard board)
    {
        var result = new List<LosePlaceTokenArea>();
        foreach (var area in board.LosePlaceTokenAreas)
        {
            if (!area.IsEmpty())
            {
                result.Add(area);
            }
        }
        return result;
    }

    public static bool ExistColoredAreaRowFullFilled()
    {
        for (int i = 0; i < GameMgr.Instance.LobbyConfig.TotalPlayerNum; i++)
        {
            var playerBoard = BoardGameMgr.Instance.GameController.GetBoardGamePlayerBySeatId(i).PlayerBoard;
            if(BoardGameUtility.ExistColoredAreaRowFullFilled(playerBoard))
            {
                return true;
            }
        }
        return false;
    }

    private static bool ExistColoredAreaRowFullFilled(PlayerBoard playerBoard)
    {
        for (int i = 0; i < playerBoard.RightPlaceTokenAreas.Count; i++)
        {
            bool isFullFilled = true;
            foreach (var area in playerBoard.RightPlaceTokenAreas[i].Areas)
            {
                if (area.IsEmpty())
                {
                    isFullFilled = false;
                    break;
                }
            }
            if (isFullFilled)
            {
                return true;
            }
        }
        return false;
    }
}
