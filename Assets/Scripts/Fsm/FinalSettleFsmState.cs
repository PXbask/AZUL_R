using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinalSettleFsmState : FsmState<BoardGameController>
{
    public class StateData
    {
    }

    private bool m_Flag;
    private BoardGameController m_Owner;

    public override void OnInit(FsmMgr<BoardGameController> fsm)
    {
        base.OnInit(fsm);

        m_Flag = false;
        m_Owner = fsm.Owner;
    }

    public override void OnEnter(FsmMgr<BoardGameController> fsm, object data = null)
    {
        base.OnEnter(fsm, data);

        m_Flag = false;
        m_Owner = fsm.Owner;
    }

    public override void OnUpdate(FsmMgr<BoardGameController> fsm)
    {
        base.OnUpdate(fsm);

        if (!m_Flag)
        {
            m_Flag = true;

            m_Owner.FinalSettlement();
            fsm.HostChangeState(FsmStateType.SettlePanel);
        }
    }
}
