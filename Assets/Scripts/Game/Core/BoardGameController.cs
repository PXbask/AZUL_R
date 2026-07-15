using AZUL;
using cfg.AZUL;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BoardGameController : MonoBehaviour
{
    [SerializeField]
    private List<GameTable> GameTables = new List<GameTable>();

    [SerializeField]
    private Transform DiskTrans;

    [SerializeField]
    private Transform CenterTrans;

    [SerializeField]
    public Transform PieceBagTrans;

    [SerializeField]
    private Dictionary<int, GameTable> GameTableDic = new Dictionary<int, GameTable>();

    [SerializeField]
    public Dictionary<int, FactoryDisk> FactoryDiskDic = new Dictionary<int, FactoryDisk>();

    public Dictionary<int, NormalPlaceTokenArea> MidTablePlaceAreas = new Dictionary<int, NormalPlaceTokenArea>();

    private Dictionary<int, BoardGamePlayer> BoardGamePlayerDic = new();

    private FsmMgr<BoardGameController> GameFsm = null;

    /// <summary>
    /// 剩余的棋子Id
    /// </summary>
    private List<int> m_RemainPieceIds = new List<int>();
    public List<int> RemainPieceIDs => m_RemainPieceIds;

    /// <summary>
    /// 放入弃牌区的棋子Id
    /// </summary>
    private List<int> m_LostPieceIds = new List<int>();
    public List<int> LostPieceIDs => m_LostPieceIds;

    public int FirstPlayerSeatId { get; set; } = -1;

    public int RoundNum { get; set; } = 0;

    public int CurrentPlayerSeatId { get; private set; }

    public void Init()
    {
        GameTableDic.Clear();
        BoardGamePlayerDic.Clear();
        FactoryDiskDic.Clear();

        InitGameFsm();
        InitCenterTable();

        foreach (var item in GameTables)
        {
            GameTableDic.Add(item.GameId, item);
        }

        ResetGame();
    }

    public void ResetGame()
    {
        FirstPlayerSeatId = -1;
        RoundNum = 0;

        //m_RemainPieceIds.Clear();
        //m_LostPieceIds.Clear();
    }

    private void InitGameFsm()
    {
        GameFsm = new FsmMgr<BoardGameController>(this);
        GameFsm.AddState<IdleFsmState>();
        GameFsm.AddState<SelectFirstPlayerFsmState>();
        GameFsm.AddState<DealCardsFsmState>();
        GameFsm.AddState<PlayerTurnFsmState>();
        GameFsm.AddState<GameStepSettleFsmState>();
        GameFsm.AddState<FinalSettleFsmState>();
        GameFsm.AddState<SettlePanelFsmState>();

        GameFsm.ChangeState<IdleFsmState>();
    }

    private void InitCenterTable()
    {
        for(int i = 0; i < CenterTrans.childCount; i++)
        {
            var trans = CenterTrans.GetChild(i);
            var area = trans.GetComponent<NormalPlaceTokenArea>();
            if (area == null)
            {
                Debug.LogError("no NormalPlaceTokenArea component in object!");
                continue;
            }
            area.Init(0, i, PlaceTokenPositionGroup.MidTable, GameStatic.NonePlayerSeatId);
            MidTablePlaceAreas.Add(i, area);
        }
    }

    public void ChangeState<T>(object data) where T : FsmState<BoardGameController>
    {
        GameFsm.ChangeState<T>(data);
    }

    private void Update()
    {
        GameFsm?.Update();
    }

    private void OnDestroy()
    {
        GameFsm?.OnDestroy();
        GameFsm = null;
    }

    public Transform GetSeatTransByGameId(int gameId)
    {
        if (GameTableDic.TryGetValue(gameId, out GameTable v))
        {
            return v.SeatTrans;
        }
        else
        {
            Debug.LogError($"BoardGameController GetSeatTransByGameId error, gameId: {gameId}");
            return null;
        }
    }

    public Transform GetBoardTransByGameId(int gameId)
    {
        if (GameTableDic.TryGetValue(gameId, out GameTable v))
        {
            return v.BoardTrans;
        }
        else
        {
            Debug.LogError($"BoardGameController GetBoardTransByGameId error, gameId: {gameId}");
            return null;
        }
    }

    public Transform GetDiskTrans()
    {
        return DiskTrans;
    }

    public void MakeBoardGamePlayer(int clientId, int gameId, PlayerBoard board)
    {
        var playerData = PlayerMgr.Instance.GetPlayerDataByGameId(gameId);  
        if(GameTableDic.TryGetValue(gameId, out var table))
        {
            BoardGamePlayer player = null;
            if(playerData.PlayerType == PlayerType.Human)
            {
                player = new BoardGameHuman(clientId, table, board);
            }
            else if(playerData.PlayerType == PlayerType.AI)
            {
                player = new BoardGameAi(clientId, table, board);
            }

            if (BoardGamePlayerDic.ContainsKey(player.SeatId))
            {
                Debug.LogError($"same seatid player created. seatid:{player.SeatId}");
            }
            else
            {
                BoardGamePlayerDic[player.SeatId] = player;
                if(BoardGamePlayerDic.Count == GameMgr.Instance.LobbyConfig.TotalPlayerNum)
                {
                    //三秒后开始对局
                    NgoMgr.Instance.ShowPopupContentClientRpc("游戏马上开始");
                    EventMgr.Instance.Trigger(new StartGameFsmEvent { Interval = 3f });
                }
            }
        }
        else
        {
            Debug.LogError($"cannot find table with gameid:{playerData.GameId}");
        }
    }

    public void AddFactoryDisk(int index, FactoryDisk disk)
    {
        if (!FactoryDiskDic.ContainsKey(index))
        {
            FactoryDiskDic[index] = disk;
        }
        else
        {
            Debug.LogError("same index factory disk added. index:" + index);
        }
    }

    public BoardGamePlayer GetBoardGamePlayerBySeatId(int seatId)
    {
        if(BoardGamePlayerDic.TryGetValue(seatId, out var player))
        {
            return player;
        }
        else
        {
            Debug.LogError($"cannot find player with seatid:{seatId}");
            return null;
        }
    }

    /// <summary>
    /// 发牌
    /// </summary>
    public void DealCards(bool reset)
    {
        if (reset)
        {
            m_LostPieceIds.Clear();
            m_RemainPieceIds.Clear();

            var tableList = DataMgr.Instance.Table.TbPiece.DataList;
            for (int i = 0; i < tableList.Count; i++)
            {
                m_RemainPieceIds.Add(tableList[i].Id);
            }

            //移除首位token
            m_RemainPieceIds.Remove(0);
            NgoMgr.Instance.SpawnFirstTokenClientRpc();

            //为每位玩家生成分数token
            NgoMgr.Instance.SpawnScorePieceTokenClientRpc();
        }

        //计算工厂圆盘的牌
        int rows = FactoryDiskDic.Count;
        int cols = GameStatic.CardNumPerDisk;
        int[] factoryData = new int[rows * cols];
        List<int> tmpList = null;
        for(int i = 0; i < rows; i++)
        {
            tmpList = TakeRandomPieces(cols);
            for(int j = 0; j < tmpList.Count; j++)
            {
                // 如果 tmpList 可能小于 cols，使用 -1 作为空槽
                int val = (j < tmpList.Count) ? tmpList[j] : -1;
                factoryData[i * cols + j] = val;
            }
        }
        NgoMgr.Instance.SpawnFactoryDiskPieceTokensClientRpc(factoryData, cols);
    }

    /// <summary>
    /// 从 RemainPieceIds 中随机取出指定数量的元素，并从原列表中移除
    /// </summary>
    /// <param name="count">要取出的元素数量</param>
    /// <returns>随机抽取的子列表</returns>
    private List<int> TakeRandomPieces(int count)
    {
        // 如果请求数量超过剩余数量，则只取剩余的全部
        int actualCount = Mathf.Min(count, m_RemainPieceIds.Count);

        List<int> result = new List<int>(actualCount);

        // 随机抽取 n 次
        for (int i = 0; i < actualCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, m_RemainPieceIds.Count);
            result.Add(m_RemainPieceIds[randomIndex]);
            m_RemainPieceIds.RemoveAt(randomIndex);
        }

        return result;
    }

    public void SpawnFirstToken()
    {
        //生成首位token
        SpawnNormalPieceToArea(0, BoardGameUtility.GetEmptyTokenAreaInMidArea());
    }

    public void SpawnFactoryDiskPieceTokens(int[] factoryData, int cols)
    {
        //因为主机已经变为DealCardsFsmState状态，所以客户端需要自己切换状态
        if (!NetworkManager.Singleton.IsHost)
        {
            GameFsm.ChangeState<DealCardsFsmState>();
        }
        if (factoryData == null)
        {
            Debug.LogError("SpawnAllPieceTokens: flatFactoryData is null");
            return;
        }
        int rows = (cols > 0) ? (factoryData.Length / cols) : 0;

        for (int i = 0; i < rows; i++)
        {
            var disk = FactoryDiskDic[i];
            for (int j = 0; j < cols; j++)
            {
                int pieceId = factoryData[i * cols + j];
                if (pieceId >= 0)
                {
                    IPlaceTokenArea area = disk.GetArea(j);
                    SpawnNormalPieceToArea(pieceId, area);
                }
            }
        }

        if (NetworkManager.Singleton.IsHost)
        {
            DOVirtual.DelayedCall(GameStatic.TokenGoToAreaAnimInterval, () =>
            {
                ++RoundNum;
                int currentSeat = GetCurrentSeatIdByRoundNum(RoundNum);
                EventMgr.Instance.Trigger(new DealCardCompleteEvent { SeatId = currentSeat });
            });
        }
    }

    public void SpawnScorePieceToken()
    {
        for (int i = 0; i < GameMgr.Instance.LobbyConfig.TotalPlayerNum; i++)
        {
            var player = GetBoardGamePlayerBySeatId(i);
            var board = player.PlayerBoard;

            var scoreToken = PoolMgr.Instance.Spawn<ScorePieceToken>();
            board.BindScoreToken(scoreToken);
        }
    }

    public void OnPlayerTurnComplete()
    {
        if(!NetworkManager.Singleton.IsHost)
            return;

        if (MidFactoryAreaEmpty())
        {
            NgoMgr.Instance.ChangeStepSettleStateClientRpc();
            return;
        }

        ++RoundNum;
        int currentSeat = GetCurrentSeatIdByRoundNum(RoundNum);
        NgoMgr.Instance.SetCurrentPlayerTurnClientRpc(currentSeat);
    }

    private bool MidFactoryAreaEmpty()
    {
        return BoardGameUtility.FactorysEmpty() && BoardGameUtility.MidTableEmpty();
    }

    private int GetCurrentSeatIdByRoundNum(int roundNum)
    {
        return (FirstPlayerSeatId + roundNum - 1) % GameMgr.Instance.LobbyConfig.TotalPlayerNum;
    }

    public void SetCurrentPlayerTurn(int seatId)
    {
        Debug.Log($"SetCurrentPlayerTurn: seatId={seatId}");

        GameFsm.ChangeState<PlayerTurnFsmState>(seatId);
        CurrentPlayerSeatId = seatId;
    }

    public void FinalSettlement()
    {
        for(int i = 0; i< GameMgr.Instance.LobbyConfig.TotalPlayerNum; i++)
        {
            var player = GetBoardGamePlayerBySeatId(i);
            var board = player.PlayerBoard;
            if (board == null)
            {
                Debug.LogError($"PlayerBoard is null for camp: {i}");
                return;
            }

            int fromScore, toScore;

            fromScore = board.Score;
            var score = BoardGameUtility.CalcualteFinalScoreGened(board);
            BoardGameUtility.PlayerAddScore(board, score);
            toScore = board.Score;
            board.PlayAddScoreAnim(fromScore, toScore);
        }
    }

    private void SpawnNormalPieceToArea(int pieceId, IPlaceTokenArea area)
    {
        NormalPieceToken token = PoolMgr.Instance.Spawn<NormalPieceToken>();
        token.Init(pieceId);
        token.transform.position = PieceBagTrans.position;
        area.PlaceToken(token);
    }

    public void AddLosePiece(int pieceId)
    {
        m_LostPieceIds.Add(pieceId);
    }

    public void AddRemainPiece(int pieceId)
    {
        m_RemainPieceIds.Add(pieceId);
    }

    public List<BoardGamePlayer> GetWinner()
    {
        int maxScore = 0;
        List<BoardGamePlayer> winners = new List<BoardGamePlayer>();
        winners.Clear();
        //寻找最高分
        foreach (var player in BoardGamePlayerDic)
        {
            if(player.Value.PlayerBoard.Score > maxScore)
            {
                maxScore = player.Value.PlayerBoard.Score;
            }
        }
        //寻找最高分的玩家
        List<BoardGamePlayer> maxScorePlayers = new List<BoardGamePlayer>();
        foreach (var player in BoardGamePlayerDic)
        {
            if (player.Value.PlayerBoard.Score == maxScore)
            {
                maxScorePlayers.Add(player.Value);
            }
        }
        //进一步判断最多水平列
        Dictionary<BoardGamePlayer, int> maxFilledRowNum = new Dictionary<BoardGamePlayer, int>();
        int maxRowNum = 0;
        foreach (var player in maxScorePlayers)
        {
            int filledRowNum = BoardGameUtility.GetColoredAreaRowFullFilledNum(player.PlayerBoard);
            maxFilledRowNum[player] = filledRowNum;
            if (filledRowNum > maxRowNum)
            {
                maxRowNum = filledRowNum;
            }
        }
        //寻找最多水平列的玩家
        foreach (var kvp in maxFilledRowNum)
        {
            if (kvp.Value == maxRowNum)
            {
                winners.Add(kvp.Key);
            }
        }
        return winners;
    }
}
