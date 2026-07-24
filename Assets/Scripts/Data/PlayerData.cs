using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum PlayerType
{
    None,
    Human,
    AI,
}

public struct PlayerData : INetworkSerializable
{
    public PlayerType PlayerType;
    public int ClientId;
    public int GameId;
    public FixedString64Bytes Name;
    public bool IsReady;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PlayerType);
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref GameId);
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref IsReady);
    }

    public static bool operator ==(PlayerData a, PlayerData b)
    {
        return a.PlayerType == b.PlayerType
            && a.ClientId == b.ClientId
            && a.GameId == b.GameId
            && a.Name == b.Name
            && a.IsReady == b.IsReady;
    }

    public static bool operator !=(PlayerData a, PlayerData b)
    {
        return !(a == b);
    }

    public override bool Equals(object obj)
    {
        return obj is PlayerData other && this == other;
    }

    public override int GetHashCode()
    {
        return System.HashCode.Combine(PlayerType, ClientId, GameId, Name.GetHashCode(), IsReady);
    }

    public override string ToString()
    {
        return $"PlayerData {{ PlayerType = {PlayerType}, ClientId = {ClientId}, GameId = {GameId}, Name = {Name}, IsReady = {IsReady} }}";
    }
}
