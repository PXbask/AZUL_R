using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 状态机的待机状态
/// </summary>
public class IdleFsmState : FsmState<BoardGameController>
{
    private float m_Timer;
    private bool m_HasTimer;
    private bool m_Flag;
    public override void OnEnter(FsmMgr<BoardGameController> fsm, object data = null)
    {
        base.OnEnter(fsm, data);
        m_Timer = 0;
        m_HasTimer = false;
        m_Flag = false;
        EventMgr.Instance.Subscribe<StartGameFsmEvent>(OnRecvStartGameFsm);

        Debug.Log("进入了IdleFsmState状态");
    }

    public override void OnLeave(FsmMgr<BoardGameController> fsm)
    {
        EventMgr.Instance.Unsubscribe<StartGameFsmEvent>(OnRecvStartGameFsm);
    }

    private void OnRecvStartGameFsm(StartGameFsmEvent e)
    {
        m_Timer = e.Interval;
        m_HasTimer = true;
    }

    public override void OnUpdate(FsmMgr<BoardGameController> fsm)
    {
        base.OnUpdate(fsm);
        // 在待机状态下，可以监听一些事件，或者进行一些待机逻辑
        if (!m_HasTimer) return;
        m_Timer -= Time.deltaTime;
        if(m_Timer <= 0)
        {
            if (!m_Flag)
            {
                m_Flag = true;
                fsm.ChangeState<SelectFirstPlayerFsmState>();
            }
        }
    }
}
