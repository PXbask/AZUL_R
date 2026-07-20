using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DealCardsFsmState : FsmState<BoardGameController>
{
    private bool m_Flag;

    private bool m_FirstDealCard;

    public override void OnEnter(FsmMgr<BoardGameController> fsm, object data = null)
    {
        base.OnEnter(fsm, data);

        EventMgr.Instance?.Subscribe<DealCardCompleteEvent>(OnDealCardComplete);

        m_Flag = false;
        if(data != null)
            m_FirstDealCard = (int)data != 0;
        else
            m_FirstDealCard = true;
        Debug.Log($"进入了DealCardsFsmState状态--FirstDealCard={m_FirstDealCard}");
    }

    public override void OnUpdate(FsmMgr<BoardGameController> fsm)
    {
        base.OnUpdate(fsm);
        if (!NetworkManager.Singleton.IsHost) return;

        if (!m_Flag)
        {
            m_Flag = true;

            fsm.Owner.DealCards(m_FirstDealCard);
        }
    }

    public override void OnLeave(FsmMgr<BoardGameController> fsm)
    {
        base.OnLeave(fsm);

        EventMgr.Instance?.Unsubscribe<DealCardCompleteEvent>(OnDealCardComplete);
    }

    private void OnDealCardComplete(DealCardCompleteEvent e)
    {
        if(!NetworkManager.Singleton.IsHost) return;

        NgoMgr.Instance.SetCurrentPlayerTurnClientRpc(e.SeatId);
    }
}
