using AZUL;
using System;
using System.Collections;
using System.Collections.Generic;
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
        EventMgr.Instance.Subscribe<ShowSettlePanelEvent>(OnShowSettlePanelEvent);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (EventMgr.Instance)
        {
            EventMgr.Instance.Unsubscribe<NgoLoadSceneCompleteEvent>(OnNgoLoadSceneComplete);
            EventMgr.Instance.Unsubscribe<ShowSettlePanelEvent>(OnShowSettlePanelEvent);
        }
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
            NgoMgr.Instance.NotifyHostEnterGameServerRpc(e.ClientId);
            if (NetworkManager.Singleton.IsHost)
            {
                //AI也要通知Host
                var aiPlayers = PlayerMgr.Instance.GetAllAiPlayer();
                foreach (var aiPlayer in aiPlayers)
                {
                    NgoMgr.Instance.NotifyHostEnterGameServerRpc(aiPlayer.ClientId);
                }
            }
        }
    }

    private void OnShowSettlePanelEvent(ShowSettlePanelEvent e)
    {
        UIMgr.Instance.ShowPanel(UIStatic.SettlePanelName, e);
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

        //说明所有人都已进入游戏场景并初始化完成
        if(PlayerEnterSceneFlag.Count == GameMgr.Instance.LobbyConfig.TotalPlayerNum)
        {
            GameController.HostChangeState(FsmStateType.Idle);
            NgoMgr.Instance.SpawnGameSectorsClientRpc();
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

    /// <summary>
    /// 生成桌游用到的所有的游戏板块
    /// </summary>
    public void OnSpawnAllGameSectors()
    {
        GameController.SpawnAllGameSectors();
    }

    public void OnSpawnFactoryDiskPieceTokens(int[] factoryData, int cols, bool reset)
    {
        GameController.SpawnFactoryDiskPieceTokens(factoryData, cols, reset);
    }

    public void OnSpawnFirstToken()
    {
        GameController.SpawnFirstToken();
    }

    public void OnSpawnScorePieceToken()
    {
        GameController.SpawnScorePieceToken();
    }

    public void OnSetCurrentPlayerTurn(int seatId)
    {
        GameController.SetCurrentPlayerTurn(seatId);
    }

    public void ClientDoAction(PlayerActionData data)
    {
        if(!NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("Only Host can call ClientDoAction!");
            return;
        }

        Debug.Log($"Host received Action:Action: {data}");
        NgoMgr.Instance.DoActionClientRpc(data);
    }
}
