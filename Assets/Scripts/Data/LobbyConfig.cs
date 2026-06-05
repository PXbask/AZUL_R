using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public struct LobbyConfig : INetworkSerializable
{
    public ushort AiPort;
    public ushort PlayerPort;
    public int TotalPlayerNum;
    public int PlayerNum;
    public int AiNum;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref AiPort);
        serializer.SerializeValue(ref PlayerPort);
        serializer.SerializeValue(ref TotalPlayerNum);
        serializer.SerializeValue(ref PlayerNum);
        serializer.SerializeValue(ref AiNum);
    }
}
