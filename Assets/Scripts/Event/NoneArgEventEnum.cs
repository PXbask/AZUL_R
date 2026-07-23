using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NoneArgEventEnum : int
{
    None = 0,
    PlayerStateChangeEvent,
    GameReset,
    FsmSyncEvent,
    /// <summary>
    /// 即将切换场景，清除场景对象
    /// </summary>
    ClearSceneObjectEvent,
}
