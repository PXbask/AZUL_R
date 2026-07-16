using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SettlePanelFsmState : FsmState<BoardGameController>
{
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
        if(!NetworkManager.Singleton.IsHost)
        {
            return;
        }

        if (!m_Flag)
        {
            m_Flag = true;

            GameResultNtf ntf;
            var winnerList = m_Owner.GetWinner();
            List<int> winnerSeatIds = new List<int>();
            foreach (var winner in winnerList)
            {
                winnerSeatIds.Add(winner.SeatId);
            }
            ntf.WinnerSeatIds = winnerSeatIds.ToArray();
            ntf.PlayerDataList = m_Owner.GetAllPlayerData();

            NgoMgr.Instance.ShowSettlePanelClientRpc(ntf);
        }
    }

    public override void OnLeave(FsmMgr<BoardGameController> fsm)
    {
        base.OnLeave(fsm);
    }
}
