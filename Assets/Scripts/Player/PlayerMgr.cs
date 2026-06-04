using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerMgr : MonoSingleton<PlayerMgr>
{
    private Dictionary<int, PlayerData> ConnectedPlayerData = new Dictionary<int, PlayerData>();

    private void Start()
    {
        EventMgr.Instance.Subscribe<PlayerConnectedEvent>(OnClientConnected);
        EventMgr.Instance.Subscribe<PlayerDisconnectedEvent>(OnClientDisconnected);
    }

    protected override void OnDestroy()
    {
        if (EventMgr.Instance != null)
        {
            EventMgr.Instance.UnSubscribe<PlayerConnectedEvent>(OnClientConnected);
            EventMgr.Instance.UnSubscribe<PlayerDisconnectedEvent>(OnClientDisconnected);
        }
    }

    private void OnClientConnected(PlayerConnectedEvent e)
    {
        AddPlayer((int)e.ClientId);
    }

    private void OnClientDisconnected(PlayerDisconnectedEvent e)
    {
        RemovePlayer((int)e.ClientId);
    }

    public void AddPlayer(int clientId)
    {
        if (ConnectedPlayerData.ContainsKey(clientId))
        {
            Debug.LogError("Player with ClientId " + clientId + " already exists.");
            return;
        }
        PlayerData data = new PlayerData()
        {
            PlayerType = PlayerType.Human,
            ClientId = (int)clientId,
            Name = string.Format("Human [{0}]", clientId),
            IsReady = false,
        };
        ConnectedPlayerData[clientId] = data;
        EventMgr.Instance.Trigger(NoneArgEventEnum.PlayerStateChangeEvent);
    }

    public void RemovePlayer(int clientId)
    {
        if (!ConnectedPlayerData.TryGetValue(clientId, out PlayerData value))
        {
            Debug.LogError($"PlayerDataDict does not contain clientId {clientId}");
        }
        else
        {
            ConnectedPlayerData.Remove(clientId);
            EventMgr.Instance.Trigger(NoneArgEventEnum.PlayerStateChangeEvent);
        }
    }

    public List<PlayerData> GetAllPlayers()
    {
        int localClientId = (int)NetworkManager.Singleton.LocalClientId;

        List<PlayerData> result = new List<PlayerData>(ConnectedPlayerData.Values);
        result.Sort((a, b) =>
        {
            bool aIsLocal = a.ClientId == localClientId;
            bool bIsLocal = b.ClientId == localClientId;
            if (aIsLocal != bIsLocal)
                return aIsLocal ? -1 : 1;

            if (a.PlayerType != b.PlayerType)
                return a.PlayerType == PlayerType.Human ? -1 : 1;

            return a.ClientId.CompareTo(b.ClientId);
        });

        return result;
    }
}
