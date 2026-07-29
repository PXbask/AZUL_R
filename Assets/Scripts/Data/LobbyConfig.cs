using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.Netcode;
using UnityEngine;

public struct LobbyConfig : INetworkSerializable
{
    public ushort AiPort;
    public ushort PlayerPort;
    public int TotalPlayerNum;
    public int HumanPlayerNum;
    public int AiNum;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref AiPort);
        serializer.SerializeValue(ref PlayerPort);
        serializer.SerializeValue(ref TotalPlayerNum);
        serializer.SerializeValue(ref HumanPlayerNum);
        serializer.SerializeValue(ref AiNum);
    }
}
