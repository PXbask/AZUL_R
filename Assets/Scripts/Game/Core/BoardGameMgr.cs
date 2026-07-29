using AZUL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Unity.Netcode;
using UnityEngine;

public class BoardGameMgr : MonoSingleton<BoardGameMgr>
{
    public BoardGameController GameController {  get; private set; }

    private Dictionary<int, bool> PlayerEnterSceneFlag = new();

    private void Start()
    {
        EventMgr.Instance.Subscribe<NgoLoadSceneCompleteEvent>(OnNgoLoadSceneComplete);
        EventMgr.Instance.Subscribe<ReceiveMessageEvent<GameResultNtf>>(OnGameResultNtf);
        EventMgr.Instance.Subscribe<ReceiveMessageEvent<DealCardsNtf>>(OnDealCardsNtf);
        EventMgr.Instance.Subscribe<ReceiveMessageEvent<PlayerActionRequest>>(OnPlayerActionRequest);
        EventMgr.Instance.Subscribe<ReceiveMessageEvent<ChangePlayerTurnNtf>>(OnChangePlayerTurnNtf);
        EventMgr.Instance.Subscribe<ReceiveMessageEvent<ClientEnterGameSceneNtf>>(OnClientEnterGameSceneNtf);
        EventMgr.Instance.Subscribe<ReceiveMessageEvent<GameResetNtf>>(OnGameResetNtf);

        NetworkManager.Singleton.OnClientStarted += OnClientStarted;
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (EventMgr.Instance)
        {
            EventMgr.Instance.Unsubscribe<NgoLoadSceneCompleteEvent>(OnNgoLoadSceneComplete);
            EventMgr.Instance.Unsubscribe<ReceiveMessageEvent<GameResultNtf>>(OnGameResultNtf);
            EventMgr.Instance.Unsubscribe<ReceiveMessageEvent<DealCardsNtf>>(OnDealCardsNtf);   
            EventMgr.Instance.Unsubscribe<ReceiveMessageEvent<PlayerActionRequest>>(OnPlayerActionRequest);
            EventMgr.Instance.Unsubscribe<ReceiveMessageEvent<ChangePlayerTurnNtf>>(OnChangePlayerTurnNtf);
            EventMgr.Instance.Unsubscribe<ReceiveMessageEvent<ClientEnterGameSceneNtf>>(OnClientEnterGameSceneNtf);
            EventMgr.Instance.Unsubscribe<ReceiveMessageEvent<GameResetNtf>>(OnGameResetNtf);
        }

        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        }
    }

    private void OnClientStarted()
    {
        
    }

    private void OnClientStopped(bool obj)
    {
        GameController = null;
        PlayerEnterSceneFlag.Clear();
    }

    public void GameReset()
    {
        GameController.GameReset();
    }

    private void OnNgoLoadSceneComplete(NgoLoadSceneCompleteEvent e)
    {
        //只进行本地逻辑
        if (e.ClientId != (int)NetworkManager.Singleton.LocalClientId) return;

        //进入游戏场景
        if(e.SceneName == SceneStatic.GameSceneName)
        {
            //确保GameController已初始化
            if (GameController == null)
            {
                GameObject obj = GameObject.Find("BoardGameController");
                if (obj)
                {
                    GameController = obj.GetComponent<BoardGameController>();
                    GameController.Init();
                }
                else
                {
                    Debug.LogError("BoardGameController not found in scene!");
                }
            }

            //通知Host本地初始化完毕
            ClientEnterGameSceneNtf ntf = new ClientEnterGameSceneNtf();
            ntf.ClientId = (uint)e.ClientId;
            NetworkMgr.Instance?.SendMessageToHost(MessageId.ClientEnterGameSceneNtf, ntf);

            if (NetworkManager.Singleton.IsHost)
            {
                //AI也要通知Host
                var aiPlayers = PlayerMgr.Instance.GetAllAiPlayer();
                foreach (var aiPlayer in aiPlayers)
                {
                    ClientEnterBoardGameScene(aiPlayer.ClientId);
                }
            }
        }
    }

    private void OnGameResetNtf(ReceiveMessageEvent<GameResetNtf> e)
    {
        if (e.Message == null)
        {
            Debug.LogError("GameResetNtf message is null!");
            return;
        }

        UIMgr.Instance.HideAllPanels();
        GameReset();
    }

    private void OnClientEnterGameSceneNtf(ReceiveMessageEvent<ClientEnterGameSceneNtf> e)
    {
        ClientEnterBoardGameScene((int)e.Message.ClientId);
    }

    private void OnGameResultNtf(ReceiveMessageEvent<GameResultNtf> e)
    {
        UIMgr.Instance.ShowPanel(UIStatic.SettlePanelName, e.Message);
    }

    private void OnDealCardsNtf(ReceiveMessageEvent<DealCardsNtf> e)
    {
        if(e.Message == null)
        {
            Debug.LogError("DealCardsNtf message is null!");
            return;
        }

        GameController.OnDealCardsNtf(e.Message);
    }

    private void OnPlayerActionRequest(ReceiveMessageEvent<PlayerActionRequest> e)
    {
        PlayerActionResponse response = new();
        response.Success = false;

        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("Only Host can call ClientDoAction!");
            return;
        }

        if(e.Message == null)
        {
            Debug.LogError("PlayerActionRequest message is null!");
            return;
        }

        Debug.Log($"Host received Action:Action: {e.Message}");

        response.Success = true;
        response.ActionData = e.Message.ActionData;
        NetworkMgr.Instance?.SendMessageToAllClients(MessageId.PlayerDoActionRsp, response);
    }

    /// <summary>
    /// Host收到客户端进入游戏场景的通知后，调用此方法进行处理
    /// </summary>
    public void ClientEnterBoardGameScene(int clientId)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("Local is not Host!");
            return;
        }

        if(PlayerEnterSceneFlag.ContainsKey(clientId))
        {
            Debug.LogError($"Player ClientId:{clientId} is already enter scene");
            return;
        }

        HostAddNewPlayer(clientId);

        //说明所有人都已进入游戏场景并初始化完成
        if (PlayerEnterSceneFlag.Count == GameMgr.Instance.LobbyConfig.TotalPlayerNum)
        {
            GameController.ProcessEnterIdleFsm();
        }
    }

    public void HostAddNewPlayer(int clientId)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("Local is not Host!");
            return;
        }

        GameController.SpawnPlayerControllerObject(clientId);
       
        if (!PlayerEnterSceneFlag.ContainsKey(clientId))
        {
            PlayerEnterSceneFlag[clientId] = true;
        }
        else
        {
            Debug.LogError($"Player ClientId:{clientId} is already enter scene");
            return;
        }
    }

    public void SendCurrentBoardInfoToAIServer(int seatId)
    {
        var tableData = GameController.GetTableData(seatId);
        string jsonString = LitJson.JsonMapper.ToJson(tableData);
        Debug.Log($"Table Data JSON: {jsonString}");

        // 发送 JSON 字符串到 AI 服务器
        AIMgr.Instance.SendNetworkMessage(jsonString);
    }

    public void OnChangePlayerTurnNtf(ReceiveMessageEvent<ChangePlayerTurnNtf> e)
    {
        if(e.Message == null)
        {
            Debug.LogError("ChangePlayerTurnNtf message is null!");
            return;
        }
        GameController.SetCurrentPlayerTurn(e.Message.CurrentPlayerSeatId);
    }
}
