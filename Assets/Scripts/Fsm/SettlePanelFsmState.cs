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

            GameResultNtf ntf = new GameResultNtf();
            var winnerList = m_Owner.GetWinner();
            List<int> winnerSeatIds = new List<int>();
            foreach (var winner in winnerList)
            {
                winnerSeatIds.Add(winner.SeatId);
            }
            ntf.WinnerIds.AddRange(winnerSeatIds);
            var lst = m_Owner.GetAllPlayerData();
            foreach (var item in lst)
            {
                ntf.PlayerDatas.Add(NetworkUtility.MakeNetBoardGamePlayerData(item));
            }

            NetworkMgr.Instance.SendMessageToAllClient(MessageId.GameResultNtf, ntf);
        }
    }

    public override void OnLeave(FsmMgr<BoardGameController> fsm)
    {
        base.OnLeave(fsm);
    }
}
