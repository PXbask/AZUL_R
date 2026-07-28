using AZUL;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

public class PlayerTurnFsmState : FsmState<BoardGameController>
{
    public class StateData
    {
        public int SeatId;
    }

    private int m_SeatId;
    private int m_MySeatId;
    private bool m_IsSendOperation;
    private StateData m_Data;

    private NormalPieceToken m_SelectedPieceToken;
    private BoardGameController m_Owner;

    public override void OnEnter(FsmMgr<BoardGameController> fsm, object data)
    {
        base.OnEnter(fsm, data);
        m_Owner = fsm.Owner;
        m_IsSendOperation = false;
        m_SelectedPieceToken = null;

        EventMgr.Instance.Subscribe<ReceiveAIServerMsgEvent>(OnReceiveAIServerMsgEvent);
        EventMgr.Instance.Subscribe<PlayerDoActionEvent>(OnPlayerDoAction);
        EventMgr.Instance.Subscribe<ReplaceHumanByAIPlayerCompleteEvent>(OnReplaceHumanByAIPlayerComplete);

        AnalysisSeatIdFromData(data);
        SetGameRoundStepIndex();

        m_MySeatId = PlayerMgr.Instance.GetSeatIdByClientId((int)NetworkManager.Singleton.LocalClientId);
        ShowStepTips();

        if (NetworkManager.Singleton.IsHost)
        {
            if(PlayerMgr.Instance.GetPlayerDataBySeatId(m_SeatId).PlayerType == PlayerType.AI)
            {
                TrySendCurrentBoardInfoToAIServerOnce(m_SeatId);
            }
        }
    }

    private void AnalysisSeatIdFromData(object data)
    {
        if (data == null)
        {
            Debug.LogError("PlayerTurnFsmState OnEnter data is null!");
            return;
        }
        if (data is StateData turnData)
        {
            m_Data = turnData;
            m_SeatId = turnData.SeatId;
        }
        else
        {
            Debug.LogError($"PlayerTurnFsmState OnEnter data is not of type StateData! Actual type: {data.GetType()}");
        }
    }

    private void SetGameRoundStepIndex()
    {
        ++(m_Owner.StepIndex.Value);

        //如果是第一轮，确保回合数从1开始
        if (m_Owner.RoundIndex.Value == 0)
        {
            m_Owner.RoundIndex.Value = 1;
        }
    }

    private void ShowStepTips()
    {
        if (m_MySeatId == m_SeatId)
        {
            UIMgr.Instance.ShowDefaultPopup("现在是你的回合");
        }
        else
        {
            var player = m_Owner.GetBoardGamePlayerBySeatId(m_SeatId);
            UIMgr.Instance.ShowDefaultPopup($"现在是{player.PlayerName}的回合");
        }
    }

    public override void OnUpdate(FsmMgr<BoardGameController> fsm)
    {
        base.OnUpdate(fsm);

        if (Input.GetMouseButtonDown(0))
        {
            if (m_MySeatId != m_SeatId)
            {
                UIMgr.Instance.ShowDefaultPopup("当前不是你的回合");
                return;
            }
            else
            {
                HandleMouseClick();
            }
        }
    }

    public override void OnLeave(FsmMgr<BoardGameController> fsm)
    {
        base.OnLeave(fsm);
        EventMgr.Instance?.Unsubscribe<PlayerDoActionEvent>(OnPlayerDoAction);
        EventMgr.Instance?.Unsubscribe<ReceiveAIServerMsgEvent>(OnReceiveAIServerMsgEvent);
        EventMgr.Instance?.Unsubscribe<ReplaceHumanByAIPlayerCompleteEvent>(OnReplaceHumanByAIPlayerComplete);
        ClearSelectedPieceTokenWithAnim();
    }

    private void OnReceiveAIServerMsgEvent(ReceiveAIServerMsgEvent e)
    {
        if (!NetworkManager.Singleton.IsHost) return;

        if (e.AIAction.SeatId != m_SeatId)
        {
            Debug.LogWarning($"Received AI action for seat {e.AIAction.SeatId}, but current turn is for seat {m_SeatId}. Ignoring action.");
            return;
        }

        NgoMgr.Instance.ClientDoActionServerRpc(e.AIAction);
    }

    private void OnReplaceHumanByAIPlayerComplete(ReplaceHumanByAIPlayerCompleteEvent e)
    {
        if (e.SeatId != m_SeatId) return;

        if (NetworkManager.Singleton.IsHost)
        {
            if (PlayerMgr.Instance.GetPlayerDataBySeatId(m_SeatId).PlayerType == PlayerType.AI)
            {
                TrySendCurrentBoardInfoToAIServerOnce(m_SeatId);
            }
        }
    }

    private void OnPlayerDoAction(PlayerDoActionEvent e)
    {
        var action = e.Data;
        if (action.SeatId == m_SeatId)
        {
            ExecuteAIAction(action);
        }
        else
        {
            Debug.LogError($"Received action for seat {action.SeatId}, but current turn is for seat {m_SeatId}. Ignoring action.");
        }
    }

    private void HandleMouseClick()
    {
        Camera cam = PlayerController.GetHuman(NetworkManager.Singleton.LocalClientId).GetPlayerCamera();
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
                    UIMgr.Instance.ShowPopup(UIStatic.PopupPanelName, "无效的放置区域");
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
            UIMgr.Instance.ShowDefaultPopup("不能放在对手的区域");
            ClearSelectedPieceTokenWithAnim();
            return;
        }
        //如果目的地不是手动区域或地板区域，就不能放了；
        if (posData.PositionGroup != PlaceTokenPositionGroup.Manual && posData.PositionGroup != PlaceTokenPositionGroup.Lose)
        {
            Debug.Log($"Cannot place piece on this area. Position group: {posData.PositionGroup}");
            UIMgr.Instance.ShowDefaultPopup("不能放在此区域");
            ClearSelectedPieceTokenWithAnim();
            return;
        }

        var player = m_Owner.GetBoardGamePlayerBySeatId(m_MySeatId);
        var playerBoard = player.PlayerBoard;
        //目标位置是手动区域
        if (posData.PositionGroup == PlaceTokenPositionGroup.Manual)
        {
            //如果是手动区域并且对应的颜色区有这个颜色的棋子了，就不能放了；
            if (player == null)
            {
                Debug.LogError($"Player with seat ID {m_MySeatId} not found.");
                ClearSelectedPieceTokenWithAnim();
                return;
            }
            if (BoardGameUtility.PlayerBoardHasColorInColoredAreaInRow(playerBoard, posData.Row, (PieceColorType)pieceToken.PieceData.PieceTokenType))
            {
                Debug.Log("Cannot place piece in manual area because the colored area in the same row already has a piece of the same color.");
                UIMgr.Instance.ShowDefaultPopup("本行该颜色花砖已完成");
                ClearSelectedPieceTokenWithAnim();
                return;
            }

            //如果是手动区域并且放置区不是这个颜色的棋子，就不能放了
            if (BoardGameUtility.PlayerBoardDiffColorInManualAreaInRow(playerBoard, posData.Row, (PieceColorType)pieceToken.PieceData.PieceTokenType))
            {
                Debug.Log("Cannot place piece in manual area because the manual area in the same row has a piece of a different color.");
                UIMgr.Instance.ShowDefaultPopup("本行已有不同颜色的花砖");
                ClearSelectedPieceTokenWithAnim();
                return;
            }

            var data = pieceToken.OwnerPlaceTokenArea.GetPositionData();
            var sourceId = 0;
            if (data.PositionGroup == PlaceTokenPositionGroup.Factory)
            {
                sourceId = BoardGameUtility.GetFactoryIdByTokenArea(pieceToken.OwnerPlaceTokenArea as NormalPlaceTokenArea);
            }
            else if (data.PositionGroup == PlaceTokenPositionGroup.MidTable)
            {
                sourceId = GameStatic.MidTableRowId;
            }

            //可以放置了
            TrySendOperationToServerOnce(
                player.ClientId,
                player.SeatId,
                sourceId,
                (PieceColorType)pieceToken.PieceData.PieceTokenType,
                posData.Row);
        }
        //目标位置是减分区域
        else
        {
            var data = pieceToken.OwnerPlaceTokenArea.GetPositionData();
            var sourceId = 0;
            if (data.PositionGroup == PlaceTokenPositionGroup.Factory)
            {
                sourceId = BoardGameUtility.GetFactoryIdByTokenArea(pieceToken.OwnerPlaceTokenArea as NormalPlaceTokenArea);
            }
            if (data.PositionGroup == PlaceTokenPositionGroup.MidTable)
            {
                sourceId = GameStatic.MidTableRowId;
            }

            //可以放置了
            TrySendOperationToServerOnce(
                player.ClientId,
                player.SeatId,
                sourceId,
                (PieceColorType)pieceToken.PieceData.PieceTokenType,
                GameStatic.LoseAreaRowId);
        }
    }

    /// <summary>
    /// 尝试将操作发送到服务器, 保证只发送一次操作
    /// </summary>
    private void TrySendOperationToServerOnce(int clientId, int seatId, int factoryId, PieceColorType colorType, int row)
    {
        if (!m_IsSendOperation)
        {
            m_IsSendOperation = true;
            NgoMgr.Instance.ClientDoActionServerRpc(new PlayerActionData
            {
                ClientId = clientId,
                SeatId = seatId,
                FactoryId = factoryId,
                ColorType = colorType,
                Row = row
            });
        }
        else
        {
            UIMgr.Instance.ShowDefaultPopup("操作已发送，请等待回合结束");
        }
    }

    private void TrySendCurrentBoardInfoToAIServerOnce(int seatId)
    {
        if (!m_IsSendOperation)
        {
            m_IsSendOperation = true;
            BoardGameMgr.Instance.SendCurrentBoardInfoToAIServer(seatId);
        }
    }

    public void ExecuteAIAction(PlayerActionData action)
    {
        //写入操作日志
        WriteToOpertaionLog(action);

        PieceColorType colorType = action.ColorType;
        var allSameColorTokens = new List<NormalPieceToken>();

        if (action.FactoryId == -1)
        {
            //说明来源是中间区域
            var firstToken = BoardGameUtility.GetFirstTokenInMidArea();
            var player = m_Owner.GetBoardGamePlayerBySeatId(action.SeatId);
            if (firstToken != null)
            {
                //需要把首位token放入减分区
                MoveFirstTokenToSub(firstToken, action);
            }

            allSameColorTokens = BoardGameUtility.GetAllColorTypeTokenInMidTable(colorType);

            if (action.Row == GameStatic.LoseAreaRowId)
            {
                //说明目的地是弃牌区
                var loseAreas = BoardGameUtility.GetEmptyAreaInSubArea(player.PlayerBoard);
                MovePieceToSubLoseArea(allSameColorTokens, loseAreas);
            }
            else
            {
                //说明目的地是花砖区行
                var leftAreas = BoardGameUtility.GetEmptyTokenAreaInManualAreaInRow(player.PlayerBoard, action.Row);
                var loseAreas = BoardGameUtility.GetEmptyAreaInSubArea(player.PlayerBoard);
                MovePieceListToManualSubLoseArea(allSameColorTokens, leftAreas, loseAreas);
            }
        }
        else
        {
            //说明来源是工厂圆盘
            var player = m_Owner.GetBoardGamePlayerBySeatId(action.SeatId);
            allSameColorTokens = BoardGameUtility.GetAllColorTypeTokenInFactory(colorType, action.FactoryId, out var remainTokens);
            if (action.Row == GameStatic.LoseAreaRowId)
            {
                //说明目的地是弃牌区
                var loseAreas = BoardGameUtility.GetEmptyAreaInSubArea(player.PlayerBoard);
                MovePieceToSubLoseArea(allSameColorTokens, loseAreas);
                //将工厂圆盘内剩余token放入中间区域
                int remainCount = remainTokens.Count;
                var midList = BoardGameUtility.GetEmptyTokenAreaInMidArea(remainCount);
                for (int i = 0; i < remainCount; i++)
                {
                    midList[i].PlaceToken(remainTokens[i]);
                }
            }
            else
            {
                //说明目的地是花砖区行
                var leftAreas = BoardGameUtility.GetEmptyTokenAreaInManualAreaInRow(player.PlayerBoard, action.Row);
                var loseAreas = BoardGameUtility.GetEmptyAreaInSubArea(player.PlayerBoard);
                MovePieceListToManualSubLoseArea(allSameColorTokens, leftAreas, loseAreas);
                //将工厂圆盘内剩余token放入中间区域
                int remainCount = remainTokens.Count;
                var midList = BoardGameUtility.GetEmptyTokenAreaInMidArea(remainCount);
                for (int i = 0; i < remainCount; i++)
                {
                    midList[i].PlaceToken(remainTokens[i]);
                }
            }
        }

        if (NetworkManager.Singleton.IsHost)
        {
            DOVirtual.DelayedCall(GameStatic.TokenGoToAreaAnimInterval + 0.5f, () =>
            {
                m_Owner.OnPlayerTurnComplete();
            });
        }
        ClearSelectedPieceToken();
    }

    /// <summary>
    /// 写入操作日志
    /// </summary>
    private void WriteToOpertaionLog(PlayerActionData action)
    {
        if (!NetworkManager.Singleton.IsHost) return;
        if (!DataMgr.Instance.LocalStorage.EnableRuntimeLog.Value) return;
        string path = DataMgr.Instance.LocalStorage.RuntimeLogPath.Value;
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Invalid runtime log path.");
            return;
        }

        string localName = DataMgr.Instance.LocalStorage.Name.Value;
        string gameId = m_Owner.GameUID.Value;
        string writePath = DataMgr.Instance.LocalStorage.RuntimeLogPath.Value + $"/{localName}/{gameId}.log";
        //获取writePath的目录
        string directory = System.IO.Path.GetDirectoryName(writePath);
        //如果目录不存在就创建目录
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        //如果文件不存在就创建文件
        if (!System.IO.File.Exists(writePath))
        {
            System.IO.File.Create(writePath).Dispose();
        }

        RuntimeLogData logData = new RuntimeLogData
        {
            gameId = gameId,
            stepIndex = m_Owner.StepIndex.Value,
            roundIndex = m_Owner.RoundIndex.Value,
            clientId = action.ClientId,
            seatId = action.SeatId,
            request = BoardGameMgr.Instance.GameController.GetTableData(action.SeatId),
            response = action,
            error = ""
        };
        //将logData序列化为json字符串
        string jsonData = JsonUtility.ToJson(logData, true);
        //将json字符串写入文件, 开头换行
        System.IO.File.AppendAllText(writePath, jsonData + "\n");
    }

    /// <summary>
    /// 清除当前选中的棋子
    /// </summary>
    public void ClearSelectedPieceToken()
    {
        if (m_SelectedPieceToken != null)
        {
            Debug.Log("Cleared selected piece.");
            m_SelectedPieceToken = null;
        }
    }

    private void MovePieceListToManualSubLoseArea(List<NormalPieceToken> allSameColorTokens, List<NormalPlaceTokenArea> remainManualAreas, List<LosePlaceTokenArea> remainLoseAreas)
    {
        //如果手动区域可以容纳所有相同颜色棋子，就放到手动区域；
        if (allSameColorTokens.Count <= remainManualAreas.Count)
        {
            for (int i = 0; i < allSameColorTokens.Count; i++)
            {
                remainManualAreas[i].PlaceToken(allSameColorTokens[i]);
            }
        }
        //如果不可以容纳但手动区域和减分区域加起来可以容纳，就先放满手动区域再放减分区域；
        else if (allSameColorTokens.Count > remainManualAreas.Count && allSameColorTokens.Count <= remainManualAreas.Count + remainLoseAreas.Count)
        {

            for (int i = 0; i < remainManualAreas.Count; i++)
            {
                remainManualAreas[i].PlaceToken(allSameColorTokens[i]);
            }
            for (int i = remainManualAreas.Count; i < allSameColorTokens.Count; i++)
            {
                remainLoseAreas[i - remainManualAreas.Count].PlaceToken(allSameColorTokens[i]);
            }
        }
        //如果连减分区域也放不下了，就先放满手动区域和减分区域，剩余的放入弃牌区
        else
        {
            for (int i = 0; i < remainManualAreas.Count; i++)
            {
                remainManualAreas[i].PlaceToken(allSameColorTokens[i]);
            }
            for (int i = remainManualAreas.Count; i < remainManualAreas.Count + remainLoseAreas.Count; i++)
            {
                remainLoseAreas[i - remainManualAreas.Count].PlaceToken(allSameColorTokens[i]);
            }
            //剩余的放入弃牌区
            for (int i = remainManualAreas.Count + remainLoseAreas.Count; i < allSameColorTokens.Count; i++)
            {
                LosePiece(allSameColorTokens[i]);
            }
        }
    }

    public void MoveFirstTokenToSub(NormalPieceToken token, PlayerActionData action)
    {
        var player = m_Owner.GetBoardGamePlayerBySeatId(action.SeatId);
        var subAreas = BoardGameUtility.GetEmptyAreaInSubArea(player.PlayerBoard);
        if (subAreas.Count == 0)
        {
            var area = BoardGameUtility.GetLastAreaInSubArea(player.PlayerBoard);
            if (area != null && area.Token != null)
            {
                if (area.Token is NormalPieceToken pieceToken)
                {
                    LosePiece(pieceToken);
                    area.PlaceToken(token);
                }
                else
                {
                    Debug.LogError("The token in the last sub area is not a NormalPieceToken.");
                }
            }
        }
        else
        {
            MovePieceToSubLoseArea(new List<NormalPieceToken>() { token }, subAreas);
        }
    }

    private void MovePieceToSubLoseArea(List<NormalPieceToken> allSameColorTokens, List<LosePlaceTokenArea> remainSubAreas)
    {
        //如果减分区域可以容纳所有相同颜色棋子，就放到减分区域；
        if (allSameColorTokens.Count <= remainSubAreas.Count)
        {
            for (int i = 0; i < allSameColorTokens.Count; i++)
            {
                remainSubAreas[i].PlaceToken(allSameColorTokens[i]);
            }
        }
        //如果连减分区域放不下了，就先放满减分区域，剩余的放入弃牌区
        else
        {
            for (int i = 0; i < remainSubAreas.Count; i++)
            {
                remainSubAreas[i].PlaceToken(allSameColorTokens[i]);
            }
            //剩余的放入弃牌区
            for (int i = remainSubAreas.Count; i < allSameColorTokens.Count; i++)
            {
                LosePiece(allSameColorTokens[i]);
            }
        }
    }

    private void LosePiece(NormalPieceToken pieceToken)
    {
        pieceToken.Interactable = false;
        if (pieceToken.OwnerPlaceTokenArea != null)
        {
            pieceToken.OwnerPlaceTokenArea.RemoveToken();
            pieceToken.OwnerPlaceTokenArea = null;
        }

        pieceToken.Transform.DOMove(m_Owner.PieceBagTrans.position, GameStatic.TokenGoToAreaAnimInterval).SetEase(Ease.InBack).OnComplete(() =>
        {
            pieceToken.Recycle();
        });

        m_Owner.AddLosePiece(pieceToken.PieceData.Id);
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
