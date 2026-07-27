using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 真人离开游戏，AI接管真人的座位完成事件
/// </summary>
public class ReplaceHumanByAIPlayerCompleteEvent : EventBase
{
    public int HumanClientId;
    public int AIClientId;
    public int SeatId;
}
