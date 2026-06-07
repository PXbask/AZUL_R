using AZUL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardGameAi : BoardGamePlayer
{
    public override PlayerType PlayerType => PlayerType.AI;

    public BoardGameAi(GameTable table, PlayerController controller, PlayerBoard board) : base(table, controller, board)
    {
    }
}
