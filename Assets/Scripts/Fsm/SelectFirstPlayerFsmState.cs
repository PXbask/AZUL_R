using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SelectFirstPlayerFsmState : FsmState<BoardGameController>
{
    private bool m_Flag;
    public override void OnEnter(FsmMgr<BoardGameController> fsm, object data = null)
    {
        base.OnEnter(fsm, data);
        Debug.Log("进入了SelectFirstPlayerFsmState状态");

        m_Flag = false;
    }

    public override void OnUpdate(FsmMgr<BoardGameController> fsm)
    {
        base.OnUpdate(fsm);
        if (!NetworkManager.Singleton.IsHost) return;

        if (!m_Flag)
        {
            m_Flag = true;
            DecideFirstPlayer(fsm);
        }
    }

    private void DecideFirstPlayer(FsmMgr<BoardGameController> fsm)
    {
        int totalPlayerNum = GameMgr.Instance.LobbyConfig.TotalPlayerNum;
        int seatId = Random.Range(0, totalPlayerNum - 1);

        var owner = fsm.Owner;
        var player = owner.GetBoardGamePlayerBySeatId(seatId);
        if(player != null)
        {
            owner.FirstPlayerSeatId = seatId;
            owner.RoundNum = 0;
            NgoMgr.Instance.ShowPopupContentClientRpc($"选择的首位玩家:{player.PlayerName}");
        }
        //发牌
        fsm.ChangeState<DealCardsFsmState>(true);
    }
}
