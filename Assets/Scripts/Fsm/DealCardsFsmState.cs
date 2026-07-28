using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DealCardsFsmState : FsmState<BoardGameController>
{
    public class StateData
    {
        public bool FirstDealCard;
    }

    private bool m_Flag;

    private bool m_FirstDealCard;
    private StateData m_Data;

    public override void OnEnter(FsmMgr<BoardGameController> fsm, object data = null)
    {
        base.OnEnter(fsm, data);

        EventMgr.Instance?.Subscribe<DealCardCompleteEvent>(OnDealCardComplete);

        m_Flag = false;
        AnalyzeData(data);
        Debug.Log($"进入了DealCardsFsmState状态--FirstDealCard={m_FirstDealCard}");
    }

    private void AnalyzeData(object data)
    {
        if(data != null)
        {
            if (data is StateData dealData)
            {
                m_Data = dealData;
                m_FirstDealCard = dealData.FirstDealCard;
            }
            else
            {
                string typeName = data?.GetType().Name ?? "null";
                Debug.LogError($"DealCardsFsmState OnEnter data is not of type StateData! Actual type: {typeName}");
                m_FirstDealCard = false;
            }
        }
        else
        {
            m_FirstDealCard = false;
        }
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
