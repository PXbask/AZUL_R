using AZUL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardGameHuman : BoardGamePlayer
{
    public override PlayerType PlayerType => PlayerType.Human;

    public BoardGameHuman(GameTable table, PlayerController controller, PlayerBoard board) : base(table, controller, board)
    {
    }
}
