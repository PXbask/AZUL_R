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
}
