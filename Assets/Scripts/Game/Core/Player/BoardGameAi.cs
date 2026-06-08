using AZUL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardGameAi : BoardGamePlayer
{
    public override PlayerType PlayerType => PlayerType.AI;

    public BoardGameAi(int clientId, GameTable table, PlayerBoard board) : base(clientId, table, board)
    {
    }
}
