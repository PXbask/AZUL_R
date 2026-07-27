using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerMgr : MonoSingleton<PlayerMgr>
{
    private Dictionary<int, PlayerData> ConnectedPlayerData = new Dictionary<int, PlayerData>();

    private void Start()
    {
        EventMgr.Instance.Subscribe<PlayerDisconnectedEvent>(OnClientDisconnected);

        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
    }

    protected override void OnDestroy()
    {
        if (EventMgr.Instance != null)
        {
            EventMgr.Instance.Unsubscribe<PlayerDisconnectedEvent>(OnClientDisconnected);
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        }

        base.OnDestroy();
    }

    private void OnClientStopped(bool obj)
    {
        ConnectedPlayerData.Clear();
    }

    private void OnClientDisconnected(PlayerDisconnectedEvent e)
    {
        Debug.Log($"检测到玩家断开连接: {e.ClientId}");
        RemovePlayer((int)e.ClientId);
    }

    public void AddPlayer(int clientId, PlayerLocalInfoData data)
    {
        Debug.Log($"AddPlayer clientId: {clientId}, PlayerLocalInfoData: {data}");

        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("Only host can add player.");
            return;
        }
        if (ConnectedPlayerData.ContainsKey(clientId))
        {
            Debug.LogError("Player with ClientId " + clientId + " already exists.");
            return;
        }

        PlayerData playerData = new PlayerData()
        {
            PlayerType = PlayerType.Human,
            ClientId = (int)clientId,
            GameId = ConnectedPlayerData.Count,
            Name = data.Name.ToString(),
            AvatarId = data.AvatarId.ToString(),
            IsReady = false,
        };
        ConnectedPlayerData[clientId] = playerData;

        if (clientId == (int)NetworkManager.Singleton.LocalClientId)
        {
            //生成Ai玩家
            for (int i = 0; i < GameMgr.Instance.LobbyConfig.AiNum; i++)
            {
                int fakeClientId = -i - 1;
                PlayerData tdata = new PlayerData()
                {
                    PlayerType = PlayerType.AI,
                    ClientId = fakeClientId,
                    GameId = ConnectedPlayerData.Count,
                    Name = string.Format("Ai [{0}]", fakeClientId),
                    AvatarId = GameStatic.DefaultAvatarId,
                    IsReady = true,
                };
                ConnectedPlayerData[fakeClientId] = tdata;
            }
        }

        NgoMgr.Instance.UpdateLobbyPlayerDataClientRpc(ConnectedPlayerData.Values.ToArray(), GameMgr.Instance.LobbyConfig);
    }

    public void RemovePlayer(int clientId)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("Only host can remove player.");
            return;
        }
        if (!ConnectedPlayerData.TryGetValue(clientId, out PlayerData value))
        {
            Debug.LogWarning($"PlayerDataDict does not contain clientId {clientId}");
        }
        else
        {
            PlayerData data = ConnectedPlayerData[clientId];
            ConnectedPlayerData.Remove(clientId);

            //如果是host移除左右Ai玩家
            if (clientId == (int)NetworkManager.Singleton.LocalClientId)
            {
                //移除Ai玩家
                for (int i = 0; i < GameMgr.Instance.LobbyConfig.AiNum; i++)
                {
                    int fakeClientId = -i - 1;
                    ConnectedPlayerData.Remove(fakeClientId);
                }
            }
            else
            {
                //如果是其他玩家 而且目前在游戏中 使用Ai玩家顶替(托管)
                if (GameMgr.Instance.IsInGame)
                {
                    var currentAiNum = GameMgr.Instance.LobbyConfig.AiNum + 1;

                    int fakeClientId = -currentAiNum;
                    PlayerData tdata = new PlayerData()
                    {
                        PlayerType = PlayerType.AI,
                        ClientId = fakeClientId,
                        GameId = value.GameId,
                        Name = data.Name,
                        AvatarId = data.AvatarId,
                        IsReady = true,
                    };
                    ConnectedPlayerData[fakeClientId] = tdata;

                    NgoMgr.Instance.ReplaceHumanByAIPlayerClientRpc(value.GameId, clientId, fakeClientId);
                }
            }

            NgoMgr.Instance.UpdateLobbyPlayerDataClientRpc(ConnectedPlayerData.Values.ToArray(), GameMgr.Instance.LobbyConfig);
        }
    }

    public bool ContainPlayer(int clientId)
    {
        return ConnectedPlayerData.ContainsKey(clientId);
    }

    public void UpdateConnectedPlayerData(PlayerData[] dataArr, LobbyConfig config)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            ConnectedPlayerData.Clear();
            foreach (var data in dataArr)
            {
                ConnectedPlayerData[data.ClientId] = data;
            }

            GameMgr.Instance.LobbyConfig = config;
        }

        EventMgr.Instance.Trigger(NoneArgEventEnum.PlayerStateChangeEvent);
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

    public List<PlayerData> GetAllAiPlayer()
    {
        List<PlayerData> res = new List<PlayerData>();
        var players = GetAllPlayers();
        foreach (var player in players)
        {
            if (player.PlayerType == PlayerType.AI)
            {
                res.Add(player);
            }
        }
        return res;
    }

    public void PlayerSetReady(int clientId, bool v)
    {
        if (ContainPlayer(clientId))
        {
            var data = ConnectedPlayerData[clientId];
            data.IsReady = v;
            ConnectedPlayerData[clientId] = data;

            NgoMgr.Instance.UpdateLobbyPlayerDataClientRpc(ConnectedPlayerData.Values.ToArray(), GameMgr.Instance.LobbyConfig);
        }
    }

    public int GetSeatIdByClientId(int clientId)
    {
        if (ContainPlayer(clientId))
        {
            return ConnectedPlayerData[clientId].GameId;
        }
        else
        {
            Debug.LogError($"GetGameIdByClientId error, clientId: {clientId}");
            return -1;
        }
    }

    public PlayerData GetPlayerDataByClientId(int clientId)
    {
        if (ConnectedPlayerData.TryGetValue(clientId, out PlayerData data))
        {
            return data;
        }
        Debug.LogError($"GetPlayerDataByClientId error, clientId: {clientId}");
        return default;
    }

    public PlayerData GetPlayerDataBySeatId(int gameId)
    {
        foreach (var data in ConnectedPlayerData.Values)
        {
            if (data.GameId == gameId)
            {
                return data;
            }
        }
        Debug.LogError($"GetPlayerDataByGameId error, gameId: {gameId}");
        return default;
    }
}
