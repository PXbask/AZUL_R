using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct BoardGamePlayerData : INetworkSerializable
{
    public PlayerType PlayerType;
    public int ClientId;
    public int SeatId;
    public int Score;
    public FixedString64Bytes Name;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PlayerType);
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref SeatId);
        serializer.SerializeValue(ref Score);
        serializer.SerializeValue(ref Name);
    }
}
