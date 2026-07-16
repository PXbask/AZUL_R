using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct GameResultNtf : INetworkSerializable
{
    public int[] WinnerSeatIds;
    public BoardGamePlayerData[] PlayerDataList;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref WinnerSeatIds);
        serializer.SerializeValue(ref PlayerDataList);
    }
}
