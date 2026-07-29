using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum PlayerType
{
    None = 0,
    Human = 1,
    AI = 2,
}

public struct PlayerLobbyData : INetworkSerializable
{
    public PlayerType PlayerType;
    public int ClientId;
    public int SeatId;
    public FixedString64Bytes Name;
    public FixedString64Bytes AvatarId;
    public bool IsReady;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PlayerType);
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref SeatId);
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref AvatarId);
        serializer.SerializeValue(ref IsReady);
    }

    public static bool operator ==(PlayerLobbyData a, PlayerLobbyData b)
    {
        return a.PlayerType == b.PlayerType
            && a.ClientId == b.ClientId
            && a.SeatId == b.SeatId
            && a.Name == b.Name
            && a.IsReady == b.IsReady;
    }

    public static bool operator !=(PlayerLobbyData a, PlayerLobbyData b)
    {
        return !(a == b);
    }

    public override bool Equals(object obj)
    {
        return obj is PlayerLobbyData other && this == other;
    }

    public override int GetHashCode()
    {
        return System.HashCode.Combine(PlayerType, ClientId, SeatId, Name.GetHashCode(), IsReady);
    }

    public override string ToString()
    {
        return $"{{ PlayerType = {PlayerType}, ClientId = {ClientId}, GameId = {SeatId}, Name = {Name}, AvatarId = {AvatarId}, IsReady = {IsReady} }}";
    }
}
