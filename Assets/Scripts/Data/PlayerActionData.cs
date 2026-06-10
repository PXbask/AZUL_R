using AZUL;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public struct PlayerActionData : INetworkSerializable
{
    public int ClientId;
    public int SeatId;
    public int FactoryId;
    public PieceColorType ColorType;
    public int Row;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref SeatId);
        serializer.SerializeValue(ref FactoryId);
        serializer.SerializeValue(ref ColorType);
        serializer.SerializeValue(ref Row);
    }

    public override string ToString()
    {
        return $"ClientId: {ClientId}, SeatId: {SeatId}, FactoryId: {FactoryId}, ColorType: {ColorType}, Row: {Row}";
    }
}
