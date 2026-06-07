using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBoardGamePlayer
{
    /// <summary>
    /// 座位号
    /// </summary>
    int SeatId { get; }

    /// <summary>
    /// 玩家类型
    /// </summary>
    PlayerType PlayerType { get; }
}
