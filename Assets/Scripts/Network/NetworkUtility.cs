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
}
