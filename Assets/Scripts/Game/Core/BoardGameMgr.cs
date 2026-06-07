using AZUL;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public class BoardGameMgr : MonoSingleton<BoardGameMgr>
{
    private BoardGameController m_GameController;

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

    private void EnterGameScene()
    {
        GameObject obj = GameObject.Find("BoardGameController");
        if (obj)
        {
            m_GameController = obj.GetComponent<BoardGameController>();
            m_GameController.Init();
        }
    }

    private void OnNgoLoadSceneComplete(NgoLoadSceneCompleteEvent e)
    {
        if(e.SceneName == SceneStatic.GameSceneName)
        {
            if(e.ClientId == (int)NetworkManager.Singleton.LocalClient.ClientId)
            {
                //BoardGameController初始化
                EnterGameScene();
            }

            if (NetworkManager.Singleton.IsHost)
            {
                HostAddNewPlayer((int)e.ClientId);

                if(e.ClientId == (int)NetworkManager.Singleton.LocalClientId)
                {
                    //模拟Ai玩家入场
                    var players = PlayerMgr.Instance.GetAllPlayers();
                    foreach (var player in players)
                    {
                        if(player.PlayerType == PlayerType.AI)
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
        }
    }

    public void HostAddNewPlayer(int clientId)
    {
        if(clientId >= 0)
        {
            //动态生成人类玩家棋盘
            int gameId = PlayerMgr.Instance.GetGameIdByClientId(clientId);
            var boardTrans = GetBoardTransByGameId(gameId);
            NgoMgr.Instance.SpawnFromPool<PlayerBoard>(
               (ulong)clientId,
                boardTrans.position,
                boardTrans.rotation
            );

            //动态生成人类玩家网络预制体
            var seatTrans = GetSeatTransByGameId(gameId);
            var obj = NgoMgr.Instance.SpawnFromPool<PlayerController>(
                (ulong)clientId,
                seatTrans.position,
                seatTrans.rotation);
            var pc = obj.GetComponent<PlayerController>();
            pc.PlayerData.Value = PlayerMgr.Instance.GetPlayerDataByGameId(gameId);
        }
        else
        {
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            //动态生成Ai玩家棋盘
            int gameId = PlayerMgr.Instance.GetGameIdByClientId(clientId);
            var boardTrans = GetBoardTransByGameId(gameId);
            NgoMgr.Instance.SpawnFromPool<PlayerBoard>(
               localClientId,
                boardTrans.position,
                boardTrans.rotation
            );

            //生成Ai玩家网络预制体
            var seatTrans = GetSeatTransByGameId(gameId);
            var aiObj =NgoMgr.Instance.SpawnFromPool<PlayerController>(
                localClientId,
                seatTrans.position,
                seatTrans.rotation);
            var pc = aiObj.GetComponent<PlayerController>();
            pc.PlayerData.Value = PlayerMgr.Instance.GetPlayerDataByGameId(gameId);
        }
    }

    public Transform GetSeatTransByGameId(int gameId)
    {
        return m_GameController.GetSeatTransByGameId(gameId);
    }

    public Transform GetBoardTransByGameId(int gameId)
    {
        return m_GameController.GetBoardTransByGameId(gameId);
    }
}
