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
            if(e.ClientId == NetworkManager.Singleton.LocalClient.ClientId)
            {
                //BoardGameController初始化
                EnterGameScene();
                //重新设定玩家位置
                PlayerController localPlayer = PlayerController.Local;
                if (localPlayer == null)
                {
                    Debug.LogError("[BoardGameMgr] 本机 PlayerController 未找到");
                    return;
                }
                var trans = GetSeatTransByGameId((int)e.ClientId);
                localPlayer.transform.position = trans.position;
                localPlayer.transform.rotation = trans.rotation;
            }

            //动态生成棋盘
            if (NetworkManager.Singleton.IsHost)
            {
                PlayerBoard go = PoolMgr.Instance.Spawn<PlayerBoard>("Board");
                NetworkObject netObj = go.GetComponent<NetworkObject>();
                netObj.SpawnWithOwnership(e.ClientId);
                var trans = GetBoardTransByGameId((int)e.ClientId);
                netObj.transform.position = trans.position;
                netObj.transform.rotation = trans.rotation;
            }
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
