using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BoardGamePlayer : IBoardGamePlayer
{
    protected int m_SeatId;
    public int SeatId => m_SeatId;

    public virtual PlayerType PlayerType => PlayerType.None;

    public GameTable GameTable { get; set; }
}
