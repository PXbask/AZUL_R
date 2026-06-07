using AZUL;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardGameController : MonoBehaviour
{
    [SerializeField]
    private List<GameTable> GameTables = new List<GameTable>();

    [SerializeField]
    private Dictionary<int, GameTable> GameTableDic = new Dictionary<int, GameTable>();

    private Dictionary<int, BoardGamePlayer> BoardGamePlayerDic = new();

    private FsmMgr<BoardGameController> GameFsm = null;

    public int FirstPlayerSeatId { get; set; } = -1;

    public void Init()
    {
        GameTableDic.Clear();
        BoardGamePlayerDic.Clear();

        InitGameFsm();

        foreach (var item in GameTables)
        {
            GameTableDic.Add(item.GameId, item);
        }
    }

    private void InitGameFsm()
    {
        GameFsm = new FsmMgr<BoardGameController>(this);
        GameFsm.AddState<IdleFsmState>();
        GameFsm.AddState<SelectFirstPlayerFsmState>();

        GameFsm.ChangeState<IdleFsmState>();
    }

    private void Update()
    {
        GameFsm.Update();
    }

    private void OnDestroy()
    {
        GameFsm.OnDestroy();
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

    public void MakeBoardGamePlayer(PlayerController controller, PlayerBoard board)
    {
        var playerData = controller.PlayerData.Value;
        if(GameTableDic.TryGetValue(playerData.GameId, out var table))
        {
            BoardGamePlayer player = null;
            if(playerData.PlayerType == PlayerType.Human)
            {
                player = new BoardGameHuman(table, controller, board);
            }
            else if(playerData.PlayerType == PlayerType.AI)
            {
                player = new BoardGameAi(table, controller, board);
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
}
