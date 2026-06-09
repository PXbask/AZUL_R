using AZUL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public class BoardGameMgr : MonoSingleton<BoardGameMgr>
{
    private BoardGameController m_GameController;
    private BoardGameController GameController
    {
        get
        {
            if(m_GameController == null)
            {
                GameObject obj = GameObject.Find("BoardGameController");
                if (obj)
                {
                    m_GameController = obj.GetComponent<BoardGameController>();
                    m_GameController.Init();
                }
            }
            return m_GameController;
        }
    }

    private Dictionary<int, bool> PlayerEnterSceneFlag = new();

    private void Start()
    {
        EventMgr.Instance.Subscribe<NgoLoadSceneCompleteEvent>(OnNgoLoadSceneComplete);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (EventMgr.Instance)
        {
            EventMgr.Instance.Unsubscribe<NgoLoadSceneCompleteEvent>(OnNgoLoadSceneComplete);
        }
    }

    private void OnNgoLoadSceneComplete(NgoLoadSceneCompleteEvent e)
    {
        if (!NetworkManager.Singleton.IsHost) return;

        if(e.SceneName == SceneStatic.GameSceneName)
        {
            if (e.ClientId == (int)NetworkManager.Singleton.LocalClientId)
            {
                HostEnterBoardGameScene();
            }

            HostAddNewPlayer((int)e.ClientId);

            if (e.ClientId == (int)NetworkManager.Singleton.LocalClientId)
            {
                //模拟Ai玩家入场
                var players = PlayerMgr.Instance.GetAllPlayers();
                foreach (var player in players)
                {
                    if (player.PlayerType == PlayerType.AI)
                    {
                        EventMgr.Instance.Trigger(new NgoLoadSceneCompleteEvent
                        {
                            ClientId = player.ClientId,
                            SceneName = e.SceneName,
                        });
                    }
                }
            }
        }
        else
        {
            if(e.ClientId == (int)NetworkManager.Singleton.LocalClientId)
            {
                HostLeaveBoardGameScene();
            }
        }
    }

    private void HostEnterBoardGameScene()
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("Local is not Host!");
            return;
        }
    }

    private void HostLeaveBoardGameScene()
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("Local is not Host!");
            return;
        }
    }

    public void HostAddNewPlayer(int clientId)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("Local is not Host!");
            return;
        }

        int gameId = PlayerMgr.Instance.GetGameIdByClientId(clientId);
        var seatTrans = GetSeatTransByGameId(gameId);
        var aiObj = NgoMgr.Instance.SpawnFromPool<PlayerController>(
            clientId >= 0 ? (ulong)clientId : NetworkManager.Singleton.LocalClientId,
            seatTrans.position,
            seatTrans.rotation);
        var pc = aiObj.GetComponent<PlayerController>();
        pc.PlayerData.Value = PlayerMgr.Instance.GetPlayerDataByGameId(gameId);

        if (!PlayerEnterSceneFlag.ContainsKey(clientId))
        {
            PlayerEnterSceneFlag[clientId] = true;
        }
        else
        {
            Debug.Log($"Player ClientId:{clientId} is already enter scene");
            return;
        }

        if(PlayerEnterSceneFlag.Count == GameMgr.Instance.LobbyConfig.TotalPlayerNum)
        {
            //说明所有人都已进入棋牌场景
            OnAllPlayerEnterGameScene();
        }
    }

    private void OnAllPlayerEnterGameScene()
    {
        NgoMgr.Instance.SpawnPlayerBoardsClientRpc();
        NgoMgr.Instance.SpawnFactoryDisksClientRpc();
    }

    public Transform GetSeatTransByGameId(int gameId)
    {
        return GameController.GetSeatTransByGameId(gameId);
    }

    public Transform GetBoardTransByGameId(int gameId)
    {
        return GameController.GetBoardTransByGameId(gameId);
    }

    public Transform GetDiskTransform()
    {
        return GameController.GetDiskTrans();
    }

    public void AddBoardGamePlayer(int clientId, int gameId, PlayerBoard board)
    {
        GameController.MakeBoardGamePlayer(clientId, gameId, board);
    }

    public void AddFactoryDisk(int index, FactoryDisk disk)
    {
        GameController.AddFactoryDisk(index, disk);
    }

    public void SpawnAllPlayerBoards()
    {
        foreach (var player in PlayerMgr.Instance.GetAllPlayers())
        {
            var clientId = player.ClientId;
            int gameId = PlayerMgr.Instance.GetGameIdByClientId(clientId);
            var boardTrans = GetBoardTransByGameId(gameId);
            var board = PoolMgr.Instance.Spawn<PlayerBoard>(boardTrans);
            board.transform.SetPositionAndRotation(boardTrans.position, boardTrans.rotation);

            AddBoardGamePlayer(clientId, gameId, board);
        }
    }

    public void SpawnAllFactoryDisks()
    {
        int totalNum = GameMgr.Instance.LobbyConfig.TotalPlayerNum;
        var diskNum = GetFactoryDisksByPlayerNum(totalNum);
        var diskTrans = GetDiskTransform();
        for (int i = 0; i < diskNum; i++)
        {
            var disk = PoolMgr.Instance.Spawn<FactoryDisk>(diskTrans);
            disk.Init();
            var degreePiece = 360f / diskNum;
            Vector3 pos = new
                (0.35f * Mathf.Cos(Mathf.Deg2Rad * i * degreePiece),
                0,
                0.35f * Mathf.Sin(Mathf.Deg2Rad * i * degreePiece));
            disk.transform.localPosition = pos;

            AddFactoryDisk(i, disk);
        }
    }

    public void FsmChangeState<T>(object data) where T : FsmState<BoardGameController>
    {
        GameController.ChangeState<T>(data);
    }

    private int GetFactoryDisksByPlayerNum(int num)
    {
        return 2 * num + 1;
    }

    public void SpawnAllPieceTokens(int[] factoryData, int cols)
    {
        GameController.SpawnAllPieceTokens(factoryData, cols);
    }

    public void SetCurrentPlayerTurn(int seatId)
    {
        GameController.SetCurrentPlayerTurn(seatId);
    }

    public int GetCurrentPlayerTurn()
    {
        return GameController.CurrentPlayerSeatId;
    }
}
