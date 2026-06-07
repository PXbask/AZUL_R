using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMgr : MonoSingleton<GameMgr>
{
    public LobbyConfig LobbyConfig { get; set;  }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}
