using AZUL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BoardGamePlayer : IBoardGamePlayer
{
    protected int m_SeatId;
    public int SeatId => m_SeatId;

    public virtual PlayerType PlayerType => PlayerType.None;

    public string PlayerName
    {
        get
        {
            if (PlayerController != null && PlayerController.PlayerData != null)
            {
                return PlayerController.PlayerData.Value.Name.ToString();
            }
            else
            {
                return $"Player_{SeatId}";
            }
        }
    }

    public GameTable GameTable { get; protected set; }

    public PlayerController PlayerController { get; protected set; }

    public PlayerBoard PlayerBoard { get; protected set; }

    public BoardGamePlayer(GameTable table, PlayerController controller, PlayerBoard board)
    {
        GameTable = table;
        PlayerController = controller;
        PlayerBoard = board;

        if(controller.PlayerData.Value.GameId != GameTable.GameId)
        {
            Debug.LogError("PlayerController's GameId does not match GameTable's GameId");
        }
        m_SeatId = GameTable.GameId;
    }
}
