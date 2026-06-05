using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NoneArgEventEnum : int
{
    None = 0,
    PlayerStateChangeEvent,
    SceneLoadedEvent,   // 新增：场景加载完毕（客户端/Host 通用）
}
