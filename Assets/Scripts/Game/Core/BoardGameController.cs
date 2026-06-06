using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardGameController : MonoBehaviour
{
    [SerializeField]
    private List<GameTable> GameTables = new List<GameTable>();

    [SerializeField]
    private Dictionary<int, GameTable> GameTableDic = new Dictionary<int, GameTable>();

    public void Init()
    {
        GameTableDic.Clear();
        foreach (var item in GameTables)
        {
            GameTableDic.Add(item.GameId, item);
        }
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
}
