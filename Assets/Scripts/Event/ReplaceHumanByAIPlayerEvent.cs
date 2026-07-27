using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 真人离开游戏，AI接管真人的座位事件
/// </summary>
public class ReplaceHumanByAIPlayerEvent : EventBase
{
    public int HumanClientId;
    public int AIClientId;
    public int SeatId;
}
