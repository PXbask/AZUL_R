using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMgr : MonoSingleton<GameMgr>
{
    public LobbyConfig LobbyConfig { get; set;  }

    public bool IsInGame
    {
        get
        {
            return BoardGameMgr.Instance.GameController != null;
        }
    }

    private void Start()
    {
        EventMgr.Instance?.Subscribe<ReplaceHumanByAIPlayerEvent>(OnReplaceHumanByAIPlayer);
    }

    protected override void OnDestroy()
    {
        EventMgr.Instance?.Unsubscribe<ReplaceHumanByAIPlayerEvent>(OnReplaceHumanByAIPlayer);

        base.OnDestroy();
    }

    private void OnReplaceHumanByAIPlayer(ReplaceHumanByAIPlayerEvent e)
    {
        var lobbyData = LobbyConfig;
        --lobbyData.PlayerNum;
        ++lobbyData.AiNum;
        LobbyConfig = lobbyData;
    }
}
