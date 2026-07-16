using AZUL;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameStepSettleFsmState : FsmState<BoardGameController>
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

        Debug.Log("进入了GameStepSettleFsmState");
    }

    public override void OnUpdate(FsmMgr<BoardGameController> fsm)
    {
        base.OnUpdate(fsm);

        if (!m_Flag)
        {
            m_Flag = true;
            Debug.Log($"--TotalPlayerNum={GameMgr.Instance.LobbyConfig.TotalPlayerNum}");
            //获取各玩家分数
            Dictionary<int, int> playerScores = new Dictionary<int, int>();
            for (int i = 0; i < GameMgr.Instance.LobbyConfig.TotalPlayerNum; i++)
            {
                var player = m_Owner.GetBoardGamePlayerBySeatId(i);
                var board = player.PlayerBoard;
                if (board == null)
                {
                    Debug.LogError($"PlayerBoard is null for camp: {i}");
                    return;
                }
                playerScores[i] = board.Score;
            }

            //结算阶段，先将手动区的满行移动到颜色区，然后计算分数
            MoveFilledRowInManualAreaToColoredArea();
            //再将减分区的棋子放回棋袋
            MoveLoseAreaTokensToBag();

            //此处应有加分动画
            for (int i = 0; i < GameMgr.Instance.LobbyConfig.TotalPlayerNum; i++)
            {
                var player = m_Owner.GetBoardGamePlayerBySeatId(i);
                var board = player.PlayerBoard;
                if (board == null)
                {
                    Debug.LogError($"PlayerBoard is null for camp: {i}");
                    return;
                }
                int fromScore = playerScores[i];
                int toScore = board.Score;
                if (toScore > fromScore)
                {
                    board.PlayAddScoreAnim(fromScore, toScore);
                }
            }

            if (NetworkManager.Singleton.IsHost)
            {
                bool matchGameOverCondition = BoardGameUtility.ExistColoredAreaRowFullFilled();
                if (matchGameOverCondition)
                {
                    fsm.ChangeState<FinalSettleFsmState>();
                }
                else
                {
                    //重新发牌
                    fsm.ChangeState<DealCardsFsmState>(false);
                }
            }
        }
    }

    private void MoveLoseAreaTokensToBag()
    {
        for (int i = 0; i < GameMgr.Instance.LobbyConfig.TotalPlayerNum; i++)
        {
            var player = m_Owner.GetBoardGamePlayerBySeatId(i);
            var board = player.PlayerBoard;
            if (board == null)
            {
                Debug.LogError($"PlayerBoard is null for camp: {i}");
                return;
            }
            //然后计算减分区
            var loseAreas = BoardGameUtility.GetAllFilledAreaInLoseArea(board);
            for (int j = 0; j < loseAreas.Count; j++)
            {
                var loseArea = loseAreas[j];
                if (loseArea.Token != null)
                {
                    BoardGameUtility.PlayerAddScore(board, loseArea.LosePoint);

                    //如果是首位token，放回中间区域，否则放入弃牌区
                    if (((NormalPieceToken)loseArea.Token).PieceData.PieceTokenType == (int)PieceColorType.SpecialToken)
                    {
                        //谁拥有首位token，下一回合就是谁的先手
                        m_Owner.FirstPlayerSeatId = i;
                        m_Owner.RoundNum = 0;

                        var midArea = BoardGameUtility.GetEmptyTokenAreaInMidArea();
                        if (midArea != null)
                        {
                            midArea.PlaceToken(loseArea.Token);
                            loseArea.RemoveToken();
                        }
                    }
                    else
                    {
                        if (loseArea.Token is NormalPieceToken pieceToken)
                            LosePiece(pieceToken);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 结算阶段，先将手动区的满行移动到颜色区，然后计算分数
    /// </summary>
    private void MoveFilledRowInManualAreaToColoredArea()
    {
        for (int i = 0; i < GameMgr.Instance.LobbyConfig.TotalPlayerNum; i++)
        {
            var player = m_Owner.GetBoardGamePlayerBySeatId(i);
            var board = player.PlayerBoard;
            if (board == null)
            {
                Debug.LogError($"PlayerBoard is null for camp: {i}");
                return;
            }

            var pieceRows = BoardGameUtility.GetFilledRowInManualArea(board);
            foreach (var row in pieceRows)
            {
                var firstItem = row[0];
                var data = firstItem.GetPositionData();
                var targetArea = BoardGameUtility.GetColoredTileInColoredArea(board, data.Row, ((NormalPieceToken)firstItem.Token).PieceData.PieceTokenType);

                //表现：将第一个token放入对应颜色区，其余进入弃牌区
                if (firstItem.Token != null)
                {
                    targetArea.PlaceToken(firstItem.Token);
                    for (int j = 1; j < row.Count; j++)
                    {
                        if (row[j].Token is NormalPieceToken pieceToken)
                            LosePiece(pieceToken);
                    }
                }

                //计算分数
                int stepScore = BoardGameUtility.CalculateScorePieceMoveToColoredArea(board, targetArea);
                BoardGameUtility.PlayerAddScore(board, stepScore);
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
}
