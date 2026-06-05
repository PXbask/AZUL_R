using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMgr : MonoSingleton<GameMgr>
{
    public LobbyConfig LobbyConfig { get; set;  }
    private void Start()
    {
        EventMgr.Instance.Subscribe<CreateLobbyEvent>(OnCreateLobby);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (EventMgr.Instance)
        {
            EventMgr.Instance.Unsubscribe<CreateLobbyEvent>(OnCreateLobby);
        }
    }

    private void OnCreateLobby(CreateLobbyEvent e)
    {
        LobbyConfig = new LobbyConfig
        {
            AiPort = e.AiPort,
            PlayerPort = e.PlayerPort,
            TotalPlayerNum = e.TotalPlayerNum,
            PlayerNum = e.PlayerNum,
            AiNum = e.AiNum
        };
    }
}
