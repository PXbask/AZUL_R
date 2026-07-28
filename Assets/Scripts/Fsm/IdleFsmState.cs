using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct IdleFsmStateData : INetworkSerializable
{
    public long Timestamp;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Timestamp);
    }
}

/// <summary>
/// 状态机的待机状态
/// </summary>
public class IdleFsmState : FsmState<BoardGameController>
{
    private IdleFsmStateData m_Data;
    public override void OnEnter(FsmMgr<BoardGameController> fsm, object data = null)
    {
        base.OnEnter(fsm, data);

        if (data is IdleFsmStateData idleData)
        {
            m_Data = idleData;
            fsm.Owner.GameUID.Value = m_Data.Timestamp;
        }
        else
        {
            Debug.LogError("IdleFsmState: Invalid data passed to OnEnter. Expected IdleFsmStateData.");
        }
    }

    public override void OnUpdate(FsmMgr<BoardGameController> fsm)
    {
        base.OnUpdate(fsm);
    }
}
