using AZUL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.Netcode;
using UnityEngine;

public class NgoMgr : NetcodeSingleton<NgoMgr>
{
    void Start()
    {
        EventMgr.Instance.Subscribe<CreateLobbyEvent>(OnCreateLobby);
        EventMgr.Instance.Subscribe<JoinLobbyEvent>(OnJoinLobby);

        if (NetworkManager.Singleton != null)
        {
            // 开启连接审批
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // 注册审批回调
            NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionApproval;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        //注册NetworkPrefabInstanceHandler
        RegisterPrefabHandler();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        RemovePrefabHandler();
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
        }
    }

    /// <summary>
    /// 统一订阅 SceneManager 事件，避免重复注册
    /// </summary>
    private void SubscribeSceneManagerEvents()
    {
        var sm = NetworkManager.Singleton.SceneManager;
        if (sm == null) return;

        sm.OnLoadComplete += OnSceneLoadComplete;

        Debug.Log("[NgoMgr] SceneManager.OnLoadComplete 已订阅");
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (EventMgr.Instance != null)
        {
            EventMgr.Instance.Unsubscribe<CreateLobbyEvent>(OnCreateLobby);
            EventMgr.Instance.Unsubscribe<JoinLobbyEvent>(OnJoinLobby);
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

            NetworkManager.Singleton.ConnectionApprovalCallback -= OnConnectionApproval;
        }
    }

    private void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        int currentCount = NetworkManager.Singleton.ConnectedClients.Count;
        int maxPlayers = GameMgr.Instance.LobbyConfig.PlayerNum; // 只计算真人玩家上限

        if (currentCount >= maxPlayers)
        {
            // 拒绝连接
            response.Approved = false;
            response.Reason = $"房间已满 ({currentCount}/{maxPlayers})";
            Debug.Log($"[NgoMgr] 拒绝连接，房间已满: {currentCount}/{maxPlayers}");
        }
        else
        {
            response.Approved = true;
            response.CreatePlayerObject = true; // 自动生成 PlayerObject
        }
    }

    private void RegisterPrefabHandler()
    {
        var lst = PoolMgr.Instance.NetworkObjectPrefabList;
        foreach (var prefab in lst)
        {
            // 获取实现了 IPoolObject 的组件
            IPoolObject poolObj = prefab.GetComponent<IPoolObject>();
            if (poolObj == null)
            {
                Debug.LogWarning($"[NgoMgr] {prefab.name} 没有实现 IPoolObject，跳过注册");
                continue;
            }

            string poolKey = poolObj.PoolKey;
            if (string.IsNullOrEmpty(poolKey))
            {
                Debug.LogWarning($"[NgoMgr] {prefab.name} 的 PoolKey 为空，跳过注册");
                continue;
            }

            // 获取运行时具体类型（如 PlayerBoard）
            Type concreteType = poolObj.GetType();

            // 动态创建 PoolObjectSpawner<T>，T = concreteType
            Type spawnerType = typeof(PoolObjectSpawner<>).MakeGenericType(concreteType);
            INetworkPrefabInstanceHandler handler =
                (INetworkPrefabInstanceHandler)Activator.CreateInstance(spawnerType, poolKey);

            // 注册到 NGO
            NetworkManager.Singleton.PrefabHandler.AddHandler(prefab, handler);
            //_registeredHandlers[prefab] = handler;

            Debug.Log($"[NgoMgr] 注册 PrefabHandler: {prefab.name} → PoolObjectSpawner<{concreteType.Name}> Key={poolKey}");
        }
    }

    private void RemovePrefabHandler()
    {
        var lst = PoolMgr.Instance.NetworkObjectPrefabList;
        foreach (var prefab in lst)
        {
           NetworkManager.Singleton.PrefabHandler.RemoveHandler(prefab);
        }
    }

    /// <summary>
    /// Host 端从对象池取出对象并通过 NGO Spawn（Client 端会自动走 PoolObjectSpawner）
    /// </summary>
    public NetworkObject SpawnFromPool<T>(ulong ownerClientId, Vector3 position, Quaternion rotation, bool destroyWithScene = true, Action<T> beforeSpawn = null)
        where T : NetPoolObject
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("[NgoMgr] SpawnFromPool 只能在 Host 端调用");
            return null;
        }

        // Host 端从对象池取出
        T obj = PoolMgr.Instance.SpawnNetObj<T>();
        if (obj == null)
        {
            Debug.LogError($"[NgoMgr] 对象池 Spawn 失败");
            return null;
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        beforeSpawn?.Invoke(obj);

        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnWithOwnership(ownerClientId, destroyWithScene);
            return netObj;
        }
        else
        {
            Debug.LogError($"[NgoMgr] 对象 {obj.name} 没有 NetworkObject 组件，无法 Spawn");
            return null;
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

        GameMgr.Instance.LobbyConfig = new LobbyConfig
        {
            TotalPlayerNum = e.TotalPlayerNum,
            PlayerNum = e.PlayerNum,
            AiNum = e.AiNum,
            AiPort = e.AiPort,
            PlayerPort = e.PlayerPort
        };

        NetworkManager.Singleton.StartHost();
        SubscribeSceneManagerEvents();

        NgoLoadScene(SceneStatic.LobbySceneName);
    }

    private void OnJoinLobby(JoinLobbyEvent e)
    {
        string ipPart = e.IpAddress;
        string ip = string.Empty;
        ushort port = e.Port;
        // ✅ 域名解析：检查是否是域名，如果是则解析为 IP
        try
        {
            // 尝试解析为 IP 地址
            if (IPAddress.TryParse(ipPart, out IPAddress parsedIp))
            {
                // 已经是 IP 地址，直接使用
                ip = ipPart;
                Debug.Log($"Using IP address: {ip}:{port}");
            }
            else
            {
                // 是域名，需要解析
                Debug.Log($"Resolving domain: {ipPart}...");

                IPHostEntry hostEntry = Dns.GetHostEntry(ipPart);

                if (hostEntry.AddressList.Length > 0)
                {
                    // 优先使用 IPv4 地址
                    IPAddress resolvedIp = null;
                    foreach (var addr in hostEntry.AddressList)
                    {
                        if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            resolvedIp = addr;
                            break;
                        }
                    }

                    // 如果没有 IPv4，使用第一个地址
                    if (resolvedIp == null)
                    {
                        resolvedIp = hostEntry.AddressList[0];
                    }

                    ip = resolvedIp.ToString();
                    Debug.Log($"✅ Domain resolved: {ipPart} → {ip}:{port}");
                }
                else
                {
                    Debug.LogError($"Could not resolve domain: {ipPart}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"DNS resolution failed for '{ipPart}': {ex.Message}");
        }

        var ut = NetworkManager.Singleton.NetworkConfig.NetworkTransport as Unity.Netcode.Transports.UTP.UnityTransport;
        if (ut != null)
        {
            ut.SetConnectionData(ip, (ushort)port);
            Debug.Log($"Set UnityTransport connection data to IP: {ip}, Port: {port}");
        }
        else
        {
            Debug.LogError("UnityTransport is not being used as the network transport.");
        }

        NetworkManager.Singleton.StartClient();
        SubscribeSceneManagerEvents();
    }

    /// <summary>
    /// 本机玩家主动离开游戏，返回 Menu 场景。
    /// Host 调用：通知所有 Client 也返回 Menu，再关闭网络。
    /// Client 调用：通知 Host 后断开连接，本地跳转 Menu。
    /// </summary>
    public void LeaveGame()
    {
        if (NetworkManager.Singleton == null)
        {
            LoadMenuSceneLocal();
            return;
        }

        if (NetworkManager.Singleton.IsHost)
        {
            // Host 离开：先通知所有 Client 返回 Menu，再自己关闭
            NotifyAllClientsLeaveGameClientRpc();
            //ShutdownAndLoadMenu();
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            // Client 离开：通知 Host 自己离开，再断开
            NotifyHostLeaveGameServerRpc();
            ShutdownAndLoadMenu();
        }
        else
        {
            LoadMenuSceneLocal();
        }
    }

    /// <summary>
    /// 关闭网络并跳转到 Menu 场景（本地操作）
    /// </summary>
    private void ShutdownAndLoadMenu()
    {
        Debug.Log("[NgoMgr] 断开网络，返回 Menu 场景");
        StartCoroutine(ShutdownAndLoadMenuCoroutine());
    }

    private IEnumerator ShutdownAndLoadMenuCoroutine()
    {
        NetworkManager.Singleton.Shutdown();

        // 等待 NetworkManager 完全关闭（IsListening 变为 false）
        float timeout = 5f;
        float elapsed = 0f;
        while (NetworkManager.Singleton != null
               && NetworkManager.Singleton.IsListening
               && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (elapsed >= timeout)
        {
            Debug.LogWarning("[NgoMgr] Shutdown 超时，强制跳转 Menu");
        }
        else
        {
            Debug.Log($"IsListening: {NetworkManager.Singleton.IsListening}");
            Debug.Log("[NgoMgr] NetworkManager 已关闭，跳转 Menu");
        }

        LoadMenuSceneLocal();
    }

    /// <summary>
    /// 直接用 Unity SceneManager 加载 Menu（不经过 NGO，因为此时网络已断开）
    /// </summary>
    private void LoadMenuSceneLocal()
    {
        SceneMgr.Instance.LoadScene(SceneStatic.MenuSceneName);
    }

    private void OnClientConnected(ulong obj)
    {
        if(obj == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"本机玩家已连接，ClientId: {obj}");
            EventMgr.Instance.Trigger(new LocalClientConnectedEvent { ClientId = obj });
        }
        else
        {
            Debug.Log($"检测到新玩家连接，ClientId: {obj}");
        }
    }

    private void OnClientDisconnected(ulong obj)
    {
        //reason不为空说明是审批不通过，否则是正常断线
        string reason = NetworkManager.Singleton.DisconnectReason;

        if (IsHost)
        {
            // Host 端回调，说明有客户端断开连接
            Debug.Log($"Client disconnected with ID: {obj}");
            EventMgr.Instance.Trigger(new PlayerDisconnectedEvent { ClientId = obj });
        }
        else
        {
            if (!string.IsNullOrEmpty(reason))
                UIMgr.Instance.ShowDefaultPopup($"连接被拒绝，原因：{reason}");
            else
                UIMgr.Instance.ShowDefaultPopup("与服务器断开连接");
        }
    }

    /// <summary>
    /// Host 和 Client 场景加载完成时均会回调
    /// clientId: 完成加载的客户端 ID
    /// sceneName: 场景名
    /// loadSceneMode: 加载模式
    /// </summary>
    private void OnSceneLoadComplete(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
    {
        if(clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"[NgoMgr] 本机场景加载完成: {sceneName}");

            // 隐藏所有 UI（Client 端同步处理）
            UIMgr.Instance.HideAllPanels();
            UIMgr.Instance.HideAllPopups();
        }

        // 广播场景加载完成事件，供各模块监听
        EventMgr.Instance.Trigger(new NgoLoadSceneCompleteEvent { ClientId = (int)clientId, SceneName = sceneName });
    }

    public void NgoLoadScene(string sceneName)
    {
        if (!NetworkManager.Singleton.IsHost) return;

        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);

        //UIMgr.Instance.HideAllPanels();
        //UIMgr.Instance.HideAllPopups();
    }

#region RPCs

    /// <summary>
    /// Host → 广播所有 Client：强制返回 Menu
    /// </summary>
    [ClientRpc]
    public void NotifyAllClientsLeaveGameClientRpc()
    {
        // Host 自身也会收到 ClientRpc，但 ShutdownAndLoadMenu 已在 LeaveGame() 里调用
        // 只让非 Host 的纯 Client 执行
        ShutdownAndLoadMenu();
        UIMgr.Instance.ShowDefaultPopup("房主已离开游戏，返回主菜单");
    }

    [ClientRpc]
    public void ReplaceHumanByAIPlayerClientRpc(int seatId, int humanClientId, int aiClientId)
    {
        EventMgr.Instance?.Trigger(new ReplaceHumanByAIPlayerEvent
        {
            SeatId = seatId,
            HumanClientId = humanClientId,
            AIClientId = aiClientId
        });
    }

    [ClientRpc]
    public void FsmChangeStateClientRpc(FsmStateType stateType, int data = 0)
    {
        EventMgr.Instance?.Trigger(new FsmChangeStateEvent { stateType = stateType, data = data });
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyHostFsmSyncServerRpc(FsmStateType stateType)
    {
        if (!NetworkManager.Singleton.IsHost) return;
        EventMgr.Instance?.Trigger(NoneArgEventEnum.FsmSyncEvent);
    }

    [ClientRpc]
    public void UpdateLobbyPlayerDataClientRpc(PlayerData[] arr, LobbyConfig lobbyConfig)
    {
        PlayerMgr.Instance.UpdateConnectedPlayerData(arr, lobbyConfig);
    }

    [ClientRpc]
    public void SpawnGameSectorsClientRpc()
    {
        BoardGameMgr.Instance.OnSpawnAllGameSectors();
    }

    [ClientRpc]
    public void ShowPopupContentClientRpc(string content)
    {
        UIMgr.Instance.ShowDefaultPopup(content);
    }

    [ClientRpc]
    public void SpawnFactoryDiskPieceTokensClientRpc(int[] factoryData, int cols, bool reset)
    {
        BoardGameMgr.Instance.OnSpawnFactoryDiskPieceTokens(factoryData, cols, reset);
    }

    [ClientRpc]
    public void SpawnFirstTokenClientRpc()
    {
        BoardGameMgr.Instance.OnSpawnFirstToken();
    }

    [ClientRpc]
    public void SpawnScorePieceTokenClientRpc()
    {
        BoardGameMgr.Instance.OnSpawnScorePieceToken();
    }

    [ClientRpc]
    public void SetCurrentPlayerTurnClientRpc(int seatId)
    {
        BoardGameMgr.Instance.OnSetCurrentPlayerTurn(seatId);
    }

    [ClientRpc]
    public void DoActionClientRpc(PlayerActionData data)
    {
        Debug.Log($"Client received Action: Action: {data}");
        EventMgr.Instance.Trigger(new PlayerDoActionEvent { Data = data });
    }

    [ClientRpc]
    public void ShowSettlePanelClientRpc(GameResultNtf ntf)
    {
        EventMgr.Instance.Trigger(new ShowSettlePanelEvent { ntf = ntf });
    }

    [ClientRpc]
    public void GameResetClientRpc()
    {
        UIMgr.Instance.HideAllPanels();
        BoardGameMgr.Instance.GameReset();
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyAddPlayerServerRpc(int clientId,  PlayerLocalInfoData data)
    {
        if (!NetworkManager.Singleton.IsHost) return;

        PlayerMgr.Instance.AddPlayer(clientId, data);
    }

    /// <summary>
    /// Client → 通知 Host 有玩家主动离开
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void NotifyHostLeaveGameServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"[NgoMgr] Client {clientId} 主动离开游戏");
        // 可在此处做房间状态清理，例如移除玩家数据
        EventMgr.Instance?.Trigger(new PlayerLeaveGameEvent { ClientId = (int)clientId });
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangePlayerReadyStateServerRpc(int clientId, bool v)
    {
        PlayerMgr.Instance.PlayerSetReady(clientId, v);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ClientDoActionServerRpc(PlayerActionData data)
    {
        BoardGameMgr.Instance.ClientDoAction(data);
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyHostEnterGameServerRpc(int clientId)
    {
        BoardGameMgr.Instance.ClientEnterBoardGameScene(clientId);
    }
}

#endregion
