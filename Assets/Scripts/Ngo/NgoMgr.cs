using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class NgoMgr : NetcodeSingleton<NgoMgr>
{
    void Start()
    {
        EventMgr.Instance.Subscribe<CreateLobbyEvent>(OnCreateLobby);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (EventMgr.Instance != null)
        {
            EventMgr.Instance.UnSubscribe<CreateLobbyEvent>(OnCreateLobby);
        }
    }

    private void OnCreateLobby(CreateLobbyEvent e)
    {
        var ut = NetworkManager.Singleton.NetworkConfig.NetworkTransport as Unity.Netcode.Transports.UTP.UnityTransport;
        if (ut != null)
        {
            ut.SetConnectionData("127.0.0.1", e.PlayerPort);
            Debug.Log($"Set UnityTransport connection data to IP: {"127.0.0.1"}, Port: {e.PlayerPort}");
        }
        else
        {
            Debug.LogError("UnityTransport is not being used as the network transport.");
        }

        NetworkManager.Singleton.StartHost();
        NgoLoadScene(SceneStatic.LobbySceneName);
    }

    public void NgoLoadScene(string sceneName)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
