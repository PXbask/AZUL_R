using AZUL;
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

    public static PlayerType MakePlayerType(NetPlayerType netPlayerType)
    {
        int num = (int)netPlayerType;
        return (PlayerType)num;
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

    public static NetPlayerLobbyData MakeNetPlayerLobbyData(PlayerLobbyData playerLobbyData)
    {
        return new NetPlayerLobbyData
        {
            PlayerType = MakeNetPlayerType(playerLobbyData.PlayerType),
            ClientId = playerLobbyData.ClientId,
            SeatId = playerLobbyData.SeatId,
            PlayerName = playerLobbyData.Name.ToString(),
            AvatarId = playerLobbyData.AvatarId.ToString(),
            IsReady = playerLobbyData.IsReady,
        };
    }

    public static PlayerLobbyData MakePlayerLobbyData(NetPlayerLobbyData netPlayerLobbyData)
    {
        return new PlayerLobbyData
        {
            PlayerType = MakePlayerType(netPlayerLobbyData.PlayerType),
            ClientId = netPlayerLobbyData.ClientId,
            SeatId = netPlayerLobbyData.SeatId,
            Name = netPlayerLobbyData.PlayerName,
            AvatarId = netPlayerLobbyData.AvatarId,
            IsReady = netPlayerLobbyData.IsReady,
        };
    }

    public static NetLobbyConfigData MakeNetLobbyConfig(LobbyConfig lobbyConfig)
    {
        return new NetLobbyConfigData
        {
            AiPort = lobbyConfig.AiPort,
            PlayerPort = lobbyConfig.PlayerPort,
            TotalPlayerNum = lobbyConfig.TotalPlayerNum,
            HumanPlayerNum = lobbyConfig.HumanPlayerNum,
            AiPlayerNum = lobbyConfig.AiNum,
        };
    }

    public static LobbyConfig MakeLobbyConfig(NetLobbyConfigData netLobbyConfig)
    {
        return new LobbyConfig
        {
            AiPort = (ushort)netLobbyConfig.AiPort,
            PlayerPort = (ushort)netLobbyConfig.PlayerPort,
            TotalPlayerNum = netLobbyConfig.TotalPlayerNum,
            HumanPlayerNum = netLobbyConfig.HumanPlayerNum,
            AiNum = netLobbyConfig.AiPlayerNum,
        };
    }

    public static NetPieceColorType MakeNetPieceColorType(PieceColorType pieceColorType)
    {
        int num = (int)pieceColorType;
        return (NetPieceColorType)num;
    }

    public static PieceColorType MakePieceColorType(NetPieceColorType netPieceColorType)
    {
        int num = (int)netPieceColorType;
        return (PieceColorType)num;
    }

    public static NetPlayerActionData MakeNetPlayerActionData(PlayerActionData playerActionData)
    {
        return new NetPlayerActionData
        {
            ClientId = playerActionData.ClientId,
            SeatId = playerActionData.SeatId,
            FactoryId = playerActionData.FactoryId,
            ColorType = MakeNetPieceColorType(playerActionData.ColorType),
            Row = playerActionData.Row,
        };
    }

    public static PlayerActionData MakePlayerActionData(NetPlayerActionData netPlayerActionData)
    {
        return new PlayerActionData
        {
            ClientId = netPlayerActionData.ClientId,
            SeatId = netPlayerActionData.SeatId,
            FactoryId = netPlayerActionData.FactoryId,
            ColorType = MakePieceColorType(netPlayerActionData.ColorType),
            Row = netPlayerActionData.Row,
        };
    }
}
