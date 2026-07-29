using AZUL;
using cfg;
using cfg.AZUL;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// 本局游戏的唯一标识符
    /// </summary>
    public ObservableProperty<string> GameUID { get; private set; } = new ObservableProperty<string>(string.Empty);

    /// <summary>
    /// 本回合的首位玩家座位号
    /// </summary>
    public int FirstPlayerSeatId { get; set; } = -1;

    /// <summary>
    /// 当前回合的步骤数, 后续考虑删除
    /// </summary>
    public int StepNumThisRound { get; set; } = 0;

    /// <summary>
    /// 当前回合数
    /// </summary>
    public ObservableProperty<int> RoundIndex { get; private set; } = new ObservableProperty<int>(0);
    
    /// <summary>
    /// 当前步骤数
    /// </summary>
    public ObservableProperty<int> StepIndex { get; private set; } = new ObservableProperty<int>(0);

    public int CurrentPlayerSeatId { get; private set; }

    private void Start()
    {
        EventMgr.Instance?.Subscribe(NoneArgEventEnum.ClearSceneObjectEvent, OnClearScene);
        EventMgr.Instance?.Subscribe<ReceiveMessageEvent<ReplaceHumanByAIPlayerNtf>>(OnReplaceHumanByAIPlayer);
    }

    private void OnDestroy()
    {
        GameFsm?.OnDestroy();
        GameFsm = null;

        EventMgr.Instance?.Unsubscribe(NoneArgEventEnum.ClearSceneObjectEvent, OnClearScene);
        EventMgr.Instance?.Unsubscribe<ReceiveMessageEvent<ReplaceHumanByAIPlayerNtf>>(OnReplaceHumanByAIPlayer);
    }

    private void OnReplaceHumanByAIPlayer(ReceiveMessageEvent<ReplaceHumanByAIPlayerNtf> e)
    {
        if(e.Message == null)
        {
            Debug.LogError("OnReplaceHumanByAIPlayer: message is null");
            return;
        }
        var seatId = e.Message.SeatId;
        var humanPlayer = GetBoardGamePlayerBySeatId(seatId);

        //寻找旧的玩家物体和数据
        if (humanPlayer != null)
        {
            //移除原有玩家的游戏物体(已经因为断线自动Despawn了)

            //创建新的AI玩家物体, 只有host会执行
            SpawnPlayerControllerObject(e.Message.AiClientId);

            var playerBoard = humanPlayer.PlayerBoard;
            //移除原有玩家数据
            BoardGamePlayerDic.Remove(seatId);
            //创建AI玩家
            MakeBoardGamePlayer(e.Message.AiClientId, seatId, playerBoard);

            //发送事件
            EventMgr.Instance.Trigger(new ReplaceHumanByAIPlayerCompleteEvent
            {
                HumanClientId = e.Message.HumanClientId,
                AIClientId = e.Message.AiClientId,
                SeatId = seatId
            });
        }
    }

    private void OnClearScene()
    {
        ClearAllTokenBoards();
        ClearAllPieceTokens();
    }

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
            item.Init();
        }

        ResetRound();
        SpawnAllGameSectors();
    }

    public void ResetRound()
    {
        FirstPlayerSeatId = -1;
        StepNumThisRound = 0;

        RoundIndex.Value = 0;
        StepIndex.Value = 0;
    }

    public void GameReset()
    {
        //不回收板块，因为板块是固定的，只有Token会被清除
        ClearAllPieceTokens();

        ResetRound();
        ProcessEnterIdleFsm();

        ResetAllPlayerControllerTrans();
    }

    /// <summary>
    /// 开始倒计时，延迟进入选择首位玩家状态
    /// </summary>
    public void StartSelectFirstPlayerAfterS(float seconds)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            DOVirtual.DelayedCall(seconds, () =>
            {
                UIMgr.Instance.ShowBoardcastPopup("游戏马上开始");
                HostChangeState(FsmStateType.SelectFirstPlayer);
            });
        }
    }

    public void ProcessEnterIdleFsm()
    {
        //计算当前时间的时间戳
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var stateData = new IdleFsmState.StateData { Timestamp = timestamp.ToString() };
        string json = LitJson.JsonMapper.ToJson(stateData);
        HostChangeState(FsmStateType.Idle, json);
    }

    /// <summary>
    /// 清除所有Token
    /// </summary>
    private void ClearAllPieceTokens()
    {
        for (int i = 0; i < BoardGamePlayerDic.Count; i++)
        {
            var player = BoardGamePlayerDic[i];
            player.PlayerBoard.ResetBoard();
        }

        for (int i = 0; i < FactoryDiskDic.Count; i++)
        {
            var item = FactoryDiskDic[i];
            for (int j = 0; j < item.PlaceTokenAreas.Count; j++)
            {
                var area = item.GetArea(j);
                area.ResetObject();
            }
        }

        for (int i = 0; i < MidTablePlaceAreas.Count; i++)
        {
            var area = MidTablePlaceAreas[i];
            area.ResetObject();
        }
    }

    /// <summary>
    /// 清除所有游戏板块
    /// </summary>
    private void ClearAllTokenBoards()
    {
        for (int i = 0; i < BoardGamePlayerDic.Count; i++)
        {
            var player = BoardGamePlayerDic[i];
            player.PlayerBoard.Recycle();
        }

        for (int i = 0; i < FactoryDiskDic.Count; i++)
        {
            var item = FactoryDiskDic[i];
            item.Recycle();
        }
    }

    private void InitGameFsm()
    {
        GameFsm = new FsmMgr<BoardGameController>(this);
        GameFsm.AddState(FsmStateType.Idle, new IdleFsmState());
        GameFsm.AddState(FsmStateType.SelectFirstPlayer, new SelectFirstPlayerFsmState());
        GameFsm.AddState(FsmStateType.DealCards, new DealCardsFsmState());
        GameFsm.AddState(FsmStateType.PlayerTurn, new PlayerTurnFsmState());
        GameFsm.AddState(FsmStateType.GameStepSettle, new GameStepSettleFsmState());
        GameFsm.AddState(FsmStateType.FinalSettle, new FinalSettleFsmState());
        GameFsm.AddState(FsmStateType.SettlePanel, new SettlePanelFsmState());
    }

    private void InitCenterTable()
    {
        MidTablePlaceAreas.Clear();

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

    public void HostChangeState(FsmStateType stateType, object data = null)
    {
        GameFsm.HostChangeState(stateType, data);
    }

    private void Update()
    {
        GameFsm?.Update();
    }

    public Transform GetSeatTransBySeatId(int gameId)
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

    public void MakeBoardGamePlayer(int clientId, int seatId, PlayerBoard board)
    {
        var playerData = PlayerMgr.Instance.GetPlayerDataBySeatId(seatId);  
        if(GameTableDic.TryGetValue(seatId, out var table))
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
            }
        }
        else
        {
            Debug.LogError($"cannot find table with gameid:{playerData.SeatId}");
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
                int val = tmpList[j];
                factoryData[i * cols + j] = val;
            }
        }

        DealCardsNtf ntf = new DealCardsNtf();
        ntf.FactoryDiskCards.Add(factoryData);
        ntf.Column = cols;
        ntf.Reset = reset;
        NetworkMgr.Instance.SendMessageToAllClients(MessageId.DealCardsNtf, ntf);
    }

    /// <summary>
    /// 从 RemainPieceIds 中随机取出指定数量的元素，并从原列表中移除
    /// </summary>
    /// <param name="count">要取出的元素数量</param>
    /// <returns>随机抽取的子列表</returns>
    private List<int> TakeRandomPieces(int count)
    {
        List<int> result = new List<int>();
        TakeRandomPiecesInternal(count, 0, result);

        return result;
    }

    private void TakeRandomPiecesInternal(int count, int startIndex, List<int> result)
    {
        if(m_RemainPieceIds.Count == 0 && m_LostPieceIds.Count == 0)
        {
            for (int i = startIndex; i < count; i++)
            {
                result.Add(GameStatic.NullPieceId);
            }
            return;
        }

        // 如果请求数量超过剩余数量，则只取剩余的全部
        int neededCount = count - startIndex;
        int actualCount = Mathf.Min(neededCount, m_RemainPieceIds.Count);
        // 随机抽取 n 次
        for (int i = 0; i < actualCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, m_RemainPieceIds.Count);
            result.Add(m_RemainPieceIds[randomIndex]);
            m_RemainPieceIds.RemoveAt(randomIndex);
        }
        if (neededCount > actualCount)
        {
            //需要从弃牌区中补充
            PutAllLoseToRemain();
            //补充剩余数量
            TakeRandomPiecesInternal(count, startIndex + actualCount, result);  
        }
    }

    private void PutAllLoseToRemain()
    {
        m_RemainPieceIds.AddRange(m_LostPieceIds);
        m_LostPieceIds.Clear();
        Debug.Log("PutAllLoseToRemain: 将弃牌区的棋子放回剩余棋子区");
    }

    public void SpawnFirstToken()
    {
        //生成首位token
        SpawnNormalPieceToArea(0, BoardGameUtility.GetEmptyTokenAreaInMidArea());
    }

    public void SpawnFactoryDiskPieceTokens(int[] factoryData, int cols)
    {
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
                ++StepNumThisRound;
                int currentSeat = GetCurrentSeatIdByRoundNum(StepNumThisRound);
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
            Debug.Log("主机检测到中间区域和工厂圆盘都为空，进入结算阶段");
            GameFsm.HostChangeState(FsmStateType.GameStepSettle);
            return;
        }

        ++StepNumThisRound;
        int currentSeat = GetCurrentSeatIdByRoundNum(StepNumThisRound);

        ChangePlayerTurnNtf ntf = new ChangePlayerTurnNtf();
        ntf.CurrentPlayerSeatId = currentSeat;
        NetworkMgr.Instance.SendMessageToAllClients(MessageId.ChangePlayerTurnNtf, ntf);
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

        var stateData = new PlayerTurnFsmState.StateData { SeatId = seatId };
        string json = LitJson.JsonMapper.ToJson(stateData);
        GameFsm.HostChangeState(FsmStateType.PlayerTurn, json);

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
        if(pieceId != GameStatic.NullPieceId)
        {
            NormalPieceToken token = PoolMgr.Instance.Spawn<NormalPieceToken>();
            token.Init(pieceId);
            token.transform.position = PieceBagTrans.position;
            area.PlaceToken(token);
        }
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

    public BoardGamePlayerData[] GetAllPlayerData()
    {
        List<BoardGamePlayerData> playerDataList = new List<BoardGamePlayerData>();
        foreach (var player in BoardGamePlayerDic)
        {
            playerDataList.Add(player.Value.GetPlayerData());
        }
        return playerDataList.ToArray();
    }

    private void ResetAllPlayerControllerTrans()
    {
        //玩家物体回到对应位置
        if (NetworkManager.Singleton.IsHost)
        {
            var players = PlayerMgr.Instance.GetAllPlayers();
            foreach (var player in players)
            {
                var pc = PlayerController.GetHuman((ulong)player.ClientId);
                if (pc)
                {
                    int seatId = PlayerMgr.Instance.GetSeatIdByClientId(player.ClientId);
                    var seatTrans = GetSeatTransBySeatId(seatId);
                    pc.transform.SetPositionAndRotation(seatTrans.position, seatTrans.rotation);
                }
            }
        }
    }

    /// <summary>
    /// 生成代表玩家的游戏物体
    /// </summary>
    /// <param name="clientId"></param>
    public void SpawnPlayerControllerObject(int clientId)
    {
        if (!NetworkManager.Singleton.IsHost) return;

        int seatId = PlayerMgr.Instance.GetSeatIdByClientId(clientId);
        var seatTrans = GetSeatTransBySeatId(seatId);
        var obj = NgoMgr.Instance.SpawnFromPool<PlayerController>(
            clientId >= 0 ? (ulong)clientId : NetworkManager.Singleton.LocalClientId,
            seatTrans.position,
            seatTrans.rotation);

        var pc = obj.GetComponent<PlayerController>();
        var v = PlayerMgr.Instance.GetPlayerDataBySeatId(seatId);
        pc.PlayerData.Value = v;
    }

    /// <summary>
    /// 生成桌游用到的所有的游戏板块
    /// </summary>
    public void SpawnAllGameSectors()
    {
        //工厂版块
        SpawnAllFactoryDisks();
        //玩家个人版图
        SpawnAllPlayerBoards();
    }

    private void SpawnAllPlayerBoards()
    {
        foreach (var player in PlayerMgr.Instance.GetAllPlayers())
        {
            var clientId = player.ClientId;
            int seatId = PlayerMgr.Instance.GetSeatIdByClientId(clientId);
            var boardTrans = GetBoardTransByGameId(seatId);
            var board = PoolMgr.Instance.Spawn<PlayerBoard>(boardTrans);
            board.Init(seatId);
            board.transform.SetPositionAndRotation(boardTrans.position, boardTrans.rotation);

            MakeBoardGamePlayer(clientId, seatId, board);
        }
    }

    private void SpawnAllFactoryDisks()
    {
        int totalNum = GameMgr.Instance.LobbyConfig.TotalPlayerNum;
        var diskNum = GetFactoryDisksByPlayerNum(totalNum);
        var diskTrans = GetDiskTrans();
        for (int i = 0; i < diskNum; i++)
        {
            var disk = PoolMgr.Instance.Spawn<FactoryDisk>(diskTrans);
            disk.Init(i);
            var degreePiece = 360f / diskNum;
            Vector3 pos = new
                (0.35f * Mathf.Cos(Mathf.Deg2Rad * i * degreePiece),
                0,
                0.35f * Mathf.Sin(Mathf.Deg2Rad * i * degreePiece));
            disk.transform.localPosition = pos;

            AddFactoryDisk(i, disk);
        }
    }

    private int GetFactoryDisksByPlayerNum(int num)
    {
        return 2 * num + 1;
    }

    public TableData GetTableData(int playerSeatId)
    {
        TableData resData = new TableData();
        resData.totalPlayerCount = GameMgr.Instance.LobbyConfig.TotalPlayerNum;
        resData.factories = GetFactoriesData();
        resData.center = GetCenterData();

        var player = GetBoardGamePlayerBySeatId(playerSeatId);

        resData.me = player.PlayerBoard.GetPlayerBoardData();
        resData.opponents = new List<PlayerBoardData>();

        for(int i = 1; i < GameMgr.Instance.LobbyConfig.TotalPlayerNum; i++)
        {
            int seatId = (i + playerSeatId) % GameMgr.Instance.LobbyConfig.TotalPlayerNum;
            var opponent = GetBoardGamePlayerBySeatId(seatId);
            resData.opponents.Add(opponent.PlayerBoard.GetPlayerBoardData());
        }

        resData.remainTokens = GetTokenInfoInBag();
        resData.loseTokens = GetTokenInfoInLose();
        return resData;
    }

    /// <summary>
    /// 获取弃牌区内的棋子数量信息
    /// </summary>
    private List<TokenNumberData> GetTokenInfoInLose()
    {
        var dic = new Dictionary<PieceColorType, int>();

        var tableData = DataMgr.Instance.Table.TbPiece;
        foreach (var id in m_LostPieceIds)
        {
            var data = tableData.Get(id);
            if (dic.ContainsKey((PieceColorType)data.PieceTokenType))
            {
                dic[(PieceColorType)data.PieceTokenType]++;
            }
            else
            {
                dic[(PieceColorType)data.PieceTokenType] = 1;
            }
        }

        var res = new List<TokenNumberData>();
        foreach (var kvp in dic)
        {
            res.Add(new TokenNumberData { color = kvp.Key, number = kvp.Value });
        }

        return res;
    }

    /// <summary>
    /// 获取游戏盒内的棋子数量信息
    /// </summary>
    private List<TokenNumberData> GetTokenInfoInBag()
    {
        var dic = new Dictionary<PieceColorType, int>();

        var tableData = DataMgr.Instance.Table.TbPiece;
        foreach (var id in m_RemainPieceIds)
        {
            var data = tableData.Get(id);
            if (dic.ContainsKey((PieceColorType)data.PieceTokenType))
            {
                dic[(PieceColorType)data.PieceTokenType]++;
            }
            else
            {
                dic[(PieceColorType)data.PieceTokenType] = 1;
            }
        }

        var res = new List<TokenNumberData>();
        foreach (var kvp in dic)
        {
            res.Add(new TokenNumberData { color = kvp.Key, number = kvp.Value });
        }

        return res;
    }

    private List<PlaceTokenAreaData> GetCenterData()
    {
        List<PlaceTokenAreaData> centerData = new List<PlaceTokenAreaData>();
        foreach (var area in MidTablePlaceAreas.Values)
        {
            PlaceTokenAreaData data = BoardGameUtility.GetPlaceTokenAreaData(area);
            centerData.Add(data);
        }
        return centerData;
    }

    private List<List<PlaceTokenAreaData>> GetFactoriesData()
    {
        List<List<PlaceTokenAreaData>> factoriesData = new List<List<PlaceTokenAreaData>>();
        foreach (var factoryDisk in FactoryDiskDic.Values)
        {
            List<PlaceTokenAreaData> factoryData = new List<PlaceTokenAreaData>();
            foreach (var area in factoryDisk.PlaceTokenAreas)
            {
                PlaceTokenAreaData data = BoardGameUtility.GetPlaceTokenAreaData(area);
                factoryData.Add(data);
            }
            factoriesData.Add(factoryData);
        }
        return factoriesData;
    }

    public void OnDealCardsNtf(DealCardsNtf ntf)
    {
        if(ntf == null)
        {
            Debug.LogError("OnDealCardsNtf: message is null");
            return;
        }

        if (ntf.Reset)
        {
            //为每位玩家生成首位token
            SpawnFirstToken();
            //为每位玩家生成分数token
            SpawnScorePieceToken();
        }

        SpawnFactoryDiskPieceTokens(ntf.FactoryDiskCards.ToArray(), ntf.Column);
    }
}
