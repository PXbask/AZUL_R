using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 玩家本地的信息数据
/// </summary>
public struct PlayerLocalInfoData : INetworkSerializable
{
    public FixedString64Bytes Name;
    public FixedString64Bytes AvatarId;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref AvatarId);
    }

    public override string ToString()
    {
        return $"{{ Name = {Name}, AvatarId = {AvatarId} }}";
    }
}
