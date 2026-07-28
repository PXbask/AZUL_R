using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 状态机的待机状态
/// </summary>
public class IdleFsmState : FsmState<BoardGameController>
{
    public class StateData
    {
        public string Timestamp;
    }

    private StateData m_Data;
    public override void OnEnter(FsmMgr<BoardGameController> fsm, object data = null)
    {
        base.OnEnter(fsm, data);

        if (data is StateData idleData)
        {
            m_Data = idleData;
            fsm.Owner.GameUID.Value = m_Data.Timestamp;
        }
        else
        {
            Debug.LogError("IdleFsmState: Invalid data passed to OnEnter. Expected StateData.");
        }
    }

    public override void OnUpdate(FsmMgr<BoardGameController> fsm)
    {
        base.OnUpdate(fsm);
    }
}
