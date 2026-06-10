using AZUL;
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
    }

    private void InitGameFsm()
    {
        GameFsm = new FsmMgr<BoardGameController>(this);
        GameFsm.AddState<IdleFsmState>();
        GameFsm.AddState<SelectFirstPlayerFsmState>();
        GameFsm.AddState<DealCardsFsmState>();
        GameFsm.AddState<PlayerTurnFsmState>();
        GameFsm.AddState<GameStepSettleFsmState>();

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

        NgoMgr.Instance.SpawnPieceTokensClientRpc(factoryData, cols);
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

    public void SpawnAllPieceTokens(int[] factoryData, int cols)
    {
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
            for (int j = 0; j < cols; j++)
            {
                int pieceId = factoryData[i * cols + j];
                if (pieceId >= 0)
                {
                    var disk = FactoryDiskDic[i];
                    //工厂圆盘上生成棋子
                    NormalPieceToken token = PoolMgr.Instance.Spawn<NormalPieceToken>();
                    token.Init(pieceId);
                    token.transform.position = PieceBagTrans.position;

                    //动画
                    IPlaceTokenArea area = disk.GetArea(j);
                    token.GotoArea(area);
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

    public void OnPlayerTurnComplete()
    {
        if (MidFactoryAreaEmpty())
        {
            GameFsm.ChangeState<GameStepSettleFsmState>();
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
        GameFsm.ChangeState<PlayerTurnFsmState>(seatId);
        CurrentPlayerSeatId = seatId;
    }

    public void AddLosePiece(int pieceId)
    {
        m_LostPieceIds.Add(pieceId);
    }

    public void AddRemainPiece(int pieceId)
    {
        m_RemainPieceIds.Add(pieceId);
    }
}
