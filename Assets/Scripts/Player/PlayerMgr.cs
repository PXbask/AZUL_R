using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.Intrinsics;
using Unity.Netcode;
using UnityEngine;

public class PlayerMgr : MonoSingleton<PlayerMgr>
{
    private Dictionary<int, PlayerLobbyData> ConnectedPlayerData = new Dictionary<int, PlayerLobbyData>();

    private void Start()
    {
        EventMgr.Instance?.Subscribe<PlayerDisconnectedEvent>(OnClientDisconnected);
        EventMgr.Instance?.Subscribe<ReceiveMessageEvent<UpdateLobbyInfoNtf>>(OnUpdateLobbyInfo);
        EventMgr.Instance?.Subscribe<ReceiveMessageEvent<ClientProvideLocalInfoNtf>>(OnClientProvideLocalInfoNtf);
        EventMgr.Instance?.Subscribe<ReceiveMessageEvent<ClientChangePlayerReadyNtf>>(OnClientChangePlayerReadyNtf);
        EventMgr.Instance?.Subscribe<ReceiveMessageEvent<ClientLeaveGameNtf>>(OnClientLeaveGameNtf);

        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
    }

    protected override void OnDestroy()
    {
        if (EventMgr.Instance != null)
        {
            EventMgr.Instance.Unsubscribe<PlayerDisconnectedEvent>(OnClientDisconnected);
            EventMgr.Instance.Unsubscribe<ReceiveMessageEvent<UpdateLobbyInfoNtf>>(OnUpdateLobbyInfo);
            EventMgr.Instance.Unsubscribe<ReceiveMessageEvent<ClientProvideLocalInfoNtf>>(OnClientProvideLocalInfoNtf);
            EventMgr.Instance.Unsubscribe<ReceiveMessageEvent<ClientChangePlayerReadyNtf>>(OnClientChangePlayerReadyNtf);
            EventMgr.Instance.Unsubscribe<ReceiveMessageEvent<ClientLeaveGameNtf>>(OnClientLeaveGameNtf);
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        }

        base.OnDestroy();
    }

    private void OnClientLeaveGameNtf(ReceiveMessageEvent<ClientLeaveGameNtf> e)
    {
        if(e.Message == null)
        {
            Debug.LogError("OnClientLeaveGameNtf error, e.Message is null");
            return;
        }

        if(ConnectedPlayerData.TryGetValue((int)e.Message.ClientId, out PlayerLobbyData playerData))
        {
            UIMgr.Instance?.ShowDefaultPopup($"玩家 {e.Message.ClientId} 离开了游戏");
            RemovePlayer((int)e.Message.ClientId);
        }
        else
        {
            Debug.LogError("OnClientLeaveGameNtf error, playerData not found for clientId: " + e.Message.ClientId);
        }
    }

    private void OnClientChangePlayerReadyNtf(ReceiveMessageEvent<ClientChangePlayerReadyNtf> e)
    {
        if(e.Message == null)
        {
            Debug.LogError("OnClientChangePlayerReadyNtf error, e.Message is null");
            return;
        }

        if(!NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("OnClientChangePlayerReadyNtf error, not host");
            return;
        }

        if (ConnectedPlayerData.TryGetValue((int)e.Message.ClientId, out PlayerLobbyData playerData))
        {
            playerData.IsReady = e.Message.IsReady;
            ConnectedPlayerData[(int)e.Message.ClientId] = playerData;
            SendUpdateLobbyInfoNtf();
        }
    }

    private void OnClientProvideLocalInfoNtf(ReceiveMessageEvent<ClientProvideLocalInfoNtf> e)
    {
        if (!NetworkManager.Singleton.IsHost) return;

        if(e.Message == null)
        {
            Debug.LogError("OnClientProvideLocalInfoNtf error, e.Message is null");
            return;
        }

        AddPlayer((int)e.Message.ClientId, e.Message);
    }

    private void OnUpdateLobbyInfo(ReceiveMessageEvent<UpdateLobbyInfoNtf> e)
    {
        if(e.Message ==null)
        {
            Debug.LogError("OnUpdateLobbyInfo error, e.Message is null");
            return;
        }
        UpdateConnectedPlayerData(e.Message);
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

    public void AddPlayer(int clientId, ClientProvideLocalInfoNtf data)
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

        PlayerLobbyData playerData = new PlayerLobbyData()
        {
            PlayerType = PlayerType.Human,
            ClientId = (int)clientId,
            SeatId = ConnectedPlayerData.Count,
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
                PlayerLobbyData tdata = new PlayerLobbyData()
                {
                    PlayerType = PlayerType.AI,
                    ClientId = fakeClientId,
                    SeatId = ConnectedPlayerData.Count,
                    Name = string.Format("Ai [{0}]", fakeClientId),
                    AvatarId = GameStatic.DefaultAvatarId,
                    IsReady = true,
                };
                ConnectedPlayerData[fakeClientId] = tdata;
            }
        }

        SendUpdateLobbyInfoNtf();
    }

    public void RemovePlayer(int clientId)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("Only host can remove player.");
            return;
        }
        if (!ConnectedPlayerData.TryGetValue(clientId, out PlayerLobbyData value))
        {
            Debug.LogWarning($"PlayerDataDict does not contain clientId {clientId}");
        }
        else
        {
            PlayerLobbyData data = ConnectedPlayerData[clientId];
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
                    PlayerLobbyData tdata = new PlayerLobbyData()
                    {
                        PlayerType = PlayerType.AI,
                        ClientId = fakeClientId,
                        SeatId = value.SeatId,
                        Name = data.Name,
                        AvatarId = data.AvatarId,
                        IsReady = true,
                    };
                    ConnectedPlayerData[fakeClientId] = tdata;

                    ReplaceHumanByAIPlayerNtf ntf = new ReplaceHumanByAIPlayerNtf()
                    {
                        SeatId = value.SeatId,
                        HumanClientId = clientId,
                        AiClientId = fakeClientId,
                    };
                    NetworkMgr.Instance.SendMessageToAllClients(MessageId.ReplaceHumanByAIPlayerNtf, ntf);
                }
            }

            SendUpdateLobbyInfoNtf();
        }
    }

    public bool ContainPlayer(int clientId)
    {
        return ConnectedPlayerData.ContainsKey(clientId);
    }

    private void SendUpdateLobbyInfoNtf()
    {
        UpdateLobbyInfoNtf ntf = new UpdateLobbyInfoNtf();
        NetPlayerLobbyData netPlayerData = null;
        foreach (var item in ConnectedPlayerData.Values)
        {
            netPlayerData = NetworkUtility.MakeNetPlayerLobbyData(item);
            ntf.PlayerDatas.Add(netPlayerData);
        }
        ntf.LobbyConfig = NetworkUtility.MakeNetLobbyConfig(GameMgr.Instance.LobbyConfig);
        NetworkMgr.Instance?.SendMessageToAllClients(MessageId.UpdateLobbyInfoNtf, ntf);
    }

    public void UpdateConnectedPlayerData(UpdateLobbyInfoNtf message)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            ConnectedPlayerData.Clear();
            foreach (var data in message.PlayerDatas)
            {
                var playerData = NetworkUtility.MakePlayerLobbyData(data);
                ConnectedPlayerData[data.ClientId] = playerData;
            }

            var lobbyConfig = NetworkUtility.MakeLobbyConfig(message.LobbyConfig);
            GameMgr.Instance.LobbyConfig = lobbyConfig;
        }

        EventMgr.Instance.Trigger(NoneArgEventEnum.PlayerStateChangeEvent);
    }

    public List<PlayerLobbyData> GetAllPlayers()
    {
        int localClientId = (int)NetworkManager.Singleton.LocalClientId;

        List<PlayerLobbyData> result = new List<PlayerLobbyData>(ConnectedPlayerData.Values);
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

    public List<PlayerLobbyData> GetAllAiPlayer()
    {
        List<PlayerLobbyData> res = new List<PlayerLobbyData>();
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

    public int GetSeatIdByClientId(int clientId)
    {
        if (ContainPlayer(clientId))
        {
            return ConnectedPlayerData[clientId].SeatId;
        }
        else
        {
            Debug.LogError($"GetGameIdByClientId error, clientId: {clientId}");
            return -1;
        }
    }

    public PlayerLobbyData GetPlayerDataByClientId(int clientId)
    {
        if (ConnectedPlayerData.TryGetValue(clientId, out PlayerLobbyData data))
        {
            return data;
        }
        Debug.LogError($"GetPlayerDataByClientId error, clientId: {clientId}");
        return default;
    }

    public PlayerLobbyData GetPlayerDataBySeatId(int gameId)
    {
        foreach (var data in ConnectedPlayerData.Values)
        {
            if (data.SeatId == gameId)
            {
                return data;
            }
        }
        Debug.LogError($"GetPlayerDataByGameId error, gameId: {gameId}");
        return default;
    }
}
