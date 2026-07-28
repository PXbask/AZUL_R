using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NetworkUtility
{
    public static NetPlayerType MakeNetPlayerType(PlayerType playerType)
    {
        int num = (int)playerType;

        return (NetPlayerType)num;
    }

    public static NetBoardGamePlayerData MakeNetBoardGamePlayerData(BoardGamePlayerData playerData)
    {
        return new NetBoardGamePlayerData
        {
            PlayerType = MakeNetPlayerType(playerData.PlayerType),
            PlayerClientId = playerData.ClientId,
            PlayerSeatId = playerData.SeatId,
            PlayerScore = playerData.Score,
            PlayerName = playerData.Name,
        };
    }

    public static NetFsmStateType MakeNetFsmStateType(FsmStateType fsmStateType)
    {
        int num = (int)fsmStateType;
        return (NetFsmStateType)num;
    }

    public static FsmStateType MakeFsmStateType(NetFsmStateType netFsmStateType)
    {
        int num = (int)netFsmStateType;
        return (FsmStateType)num;
    }
}
