using AZUL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardGameHuman : BoardGamePlayer
{
    public override PlayerType PlayerType => PlayerType.Human;

    public BoardGameHuman(int clientId, GameTable table, PlayerBoard board) : base(clientId, table, board)
    {
    }
}
