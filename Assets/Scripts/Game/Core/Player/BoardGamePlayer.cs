using AZUL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BoardGamePlayer : IBoardGamePlayer
{
    protected int m_SeatId;
    public int SeatId => m_SeatId;

    protected int m_ClientId;
    public int ClientId => m_ClientId;

    public virtual PlayerType PlayerType => PlayerType.None;

    public string PlayerName
    {
        get
        {
            var data = PlayerMgr.Instance.GetPlayerDataByClientId(m_ClientId);
            if (data != default)
            {
                return data.Name.ToString();
            }
            else
            {
                return $"Player_{SeatId}";
            }
        }
    }

    public GameTable GameTable { get; protected set; }

    public PlayerBoard PlayerBoard { get; protected set; }

    public BoardGamePlayer(int clientId, GameTable table, PlayerBoard board)
    {
        m_ClientId = clientId;
        GameTable = table;
        PlayerBoard = board;
        m_SeatId = GameTable.GameId;
    }
}
