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

    private bool m_Flag;
    private StateData m_Data;
    private BoardGameController m_Owner;

    public override void OnEnter(FsmMgr<BoardGameController> fsm, object data = null)
    {
        base.OnEnter(fsm, data);
        m_Flag = false;
        m_Owner = fsm.Owner;

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
        if (!m_Flag)
        {
            m_Flag  = true;

            if(NetworkManager.Singleton.IsHost)
            {
                m_Owner.StartSelectFirstPlayerAfterS(GameStatic.FsmIdleToSelectFirstInterval);
            }
        }
    }
}
