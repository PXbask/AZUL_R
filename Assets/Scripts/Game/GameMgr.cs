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
        EventMgr.Instance?.Subscribe<ReceiveMessageEvent<ReplaceHumanByAIPlayerNtf>>(OnReplaceHumanByAIPlayer);
    }

    protected override void OnDestroy()
    {
        EventMgr.Instance?.Unsubscribe<ReceiveMessageEvent<ReplaceHumanByAIPlayerNtf>>(OnReplaceHumanByAIPlayer);

        base.OnDestroy();
    }

    private void OnReplaceHumanByAIPlayer(ReceiveMessageEvent<ReplaceHumanByAIPlayerNtf> e)
    {
        var lobbyData = LobbyConfig;
        --lobbyData.HumanPlayerNum;
        ++lobbyData.AiNum;
        LobbyConfig = lobbyData;
    }
}
