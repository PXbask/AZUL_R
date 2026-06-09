using AZUL;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerTurnFsmState : FsmState<BoardGameController>
{
    private int m_SeatId;
    private int m_MySeatId;

    private NormalPieceToken m_SelectedPieceToken;

    public override void OnEnter(FsmMgr<BoardGameController> fsm, object data)
    {
        base.OnEnter(fsm, data);

        if(data != null)
            m_SeatId = (int)data;
        else
            Debug.LogError("PlayerTurnFsmState OnEnter data is null!");

        m_MySeatId = PlayerMgr.Instance.GetGameIdByClientId((int)NetworkManager.Singleton.LocalClientId);
        m_SelectedPieceToken = null;
    }

    public override void OnUpdate(FsmMgr<BoardGameController> fsm)
    {
        base.OnUpdate(fsm);

        if (m_MySeatId != m_SeatId) return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    private void HandleMouseClick()
    {
        Camera cam = PlayerController.Get(NetworkManager.Singleton.LocalClientId).GetPlayerCamera();
        if (cam == null)
        {
            Debug.LogWarning("Main Camera is null, cannot perform raycast.");
            return;
        }
        if (m_SelectedPieceToken == null)
        {
            // 从鼠标位置发射射线
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            // 进行射线检测
            if (Physics.Raycast(ray, out hit))
            {
                // 尝试获取点击物体上的 PieceToken 组件
                NormalPieceToken pieceToken = hit.collider.GetComponentInParent<NormalPieceToken>();

                if (pieceToken != null && pieceToken.Interactable && pieceToken.OwnerPlaceTokenArea != null
                    && (pieceToken.OwnerPlaceTokenArea.GetPositionData().PositionGroup == PlaceTokenPositionGroup.MidTable || pieceToken.OwnerPlaceTokenArea.GetPositionData().PositionGroup == PlaceTokenPositionGroup.Factory))
                {
                    // 找到了 PieceToken，保存到缓存中
                    SelectPieceToken(pieceToken);
                }
            }
        }
        else
        {
            // 已经有选中的棋子了，检测点击的区域是否是合法的放置区域
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            // 进行射线检测
            if (Physics.Raycast(ray, out hit))
            {
                // 尝试获取点击物体上的 PlaceTokenArea 组件（包括其子类）
                BasePlaceTokenArea placeArea = hit.collider.GetComponent<BasePlaceTokenArea>();

                if (placeArea != null)
                {
                    // 处理放置逻辑
                    OnPlaceTokenToArea(m_SelectedPieceToken, placeArea);
                }
                else
                {
                    // 点击的不是有效的放置区域，取消选中
                    Debug.Log("Invalid placement area. Deselecting piece.");
                    ClearSelectedPieceTokenWithAnim();
                }
            }
            else
            {
                // 没有点击到任何物体，取消选中
                ClearSelectedPieceTokenWithAnim();
            }
        }
    }

    private void OnPlaceTokenToArea(NormalPieceToken pieceToken, BasePlaceTokenArea targetArea)
    {
        //=========检查是否可以放置==========
        var posData = targetArea.GetPositionData();
        //如果目的地不是本轮玩家侧的区域，就不能放了；
        if (m_MySeatId != targetArea.SeatId)
        {
            Debug.Log($"Cannot place piece on opponent's area. Current player: {m_MySeatId}, Target area camp: {targetArea.SeatId}");
            ClearSelectedPieceTokenWithAnim();
            return;
        }
        //如果目的地不是手动区域或地板区域，就不能放了；
        if (posData.PositionGroup != PlaceTokenPositionGroup.Manual && posData.PositionGroup != PlaceTokenPositionGroup.Lose)
        {
            Debug.Log($"Cannot place piece on this area. Position group: {posData.PositionGroup}");
            ClearSelectedPieceTokenWithAnim();
            return;
        }

        //目标位置是手动区域
        if (posData.PositionGroup == PlaceTokenPositionGroup.Manual)
        {

        }
    }

    /// <summary>
    /// 选中对应棋子
    /// </summary>
    /// <param name="pieceToken"></param>
    private void SelectPieceToken(NormalPieceToken pieceToken)
    {
        m_SelectedPieceToken = pieceToken;
        pieceToken.PlaySelectAnim();
    }

    /// <summary>
    /// 清除当前选中的棋子,有动画
    /// </summary>
    public void ClearSelectedPieceTokenWithAnim()
    {
        if (m_SelectedPieceToken != null)
        {
            m_SelectedPieceToken.PlayDeselectAnim();
            Debug.Log("Cleared selected piece.");
            m_SelectedPieceToken = null;
        }
    }
}
