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
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        //注册NetworkPrefabInstanceHandler
        RegisterPrefabHandler();

        if (NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoadComplete;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
        }
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

    /// <summary>
    /// Host 端从对象池取出对象并通过 NGO Spawn（Client 端会自动走 PoolObjectSpawner）
    /// </summary>
    public NetworkObject SpawnFromPool<T>(ulong ownerClientId, Vector3 position, Quaternion rotation, bool destroyWithScene = true, Action<T> beforeSpawn = null)
        where T : MonoBehaviour, IPoolObject
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("[NgoMgr] SpawnFromPool 只能在 Host 端调用");
            return null;
        }

        // 获取 PoolKey（通过 T 的实例属性）
        string poolKey = typeof(T).Name;

        // Host 端从对象池取出
        T obj = PoolMgr.Instance.Spawn<T>(poolKey);
        if (obj == null)
        {
            Debug.LogError($"[NgoMgr] 对象池 Spawn 失败，Key={poolKey}");
            return null;
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        beforeSpawn?.Invoke(obj);

        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(ownerClientId, destroyWithScene);

        return netObj;
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
    }

    private void OnClientConnected(ulong obj)
    {
        if (!IsHost) return;

        Debug.Log($"Client connected with ID: {obj}");
        EventMgr.Instance.Trigger(new PlayerConnectedEvent { ClientId = obj });
    }

    private void OnClientDisconnected(ulong obj)
    {
        if (!IsHost) return;

        Debug.Log($"Client disconnected with ID: {obj}");
        EventMgr.Instance.Trigger(new PlayerDisconnectedEvent { ClientId = obj });
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

    [ClientRpc]
    public void FsmChangeStateClientRpc(FsmStateType stateType, int data = 0)
    {
        Debug.Log("触发FsmChangeStateEvent事件");
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
    public void SpawnPlayerBoardsClientRpc()
    {
        BoardGameMgr.Instance.SpawnAllPlayerBoards();
    }

    [ClientRpc]
    public void SpawnFactoryDisksClientRpc()
    {
        BoardGameMgr.Instance.SpawnAllFactoryDisks();
    }

    [ClientRpc]
    public void ShowPopupContentClientRpc(string content)
    {
        UIMgr.Instance.ShowDefaultPopup(content);
    }

    [ClientRpc]
    public void SpawnFactoryDiskPieceTokensClientRpc(int[] factoryData, int cols, bool reset)
    {
        BoardGameMgr.Instance.SpawnFactoryDiskPieceTokens(factoryData, cols, reset);
    }

    [ClientRpc]
    public void SpawnFirstTokenClientRpc()
    {
        BoardGameMgr.Instance.SpawnFirstToken();
    }

    [ClientRpc]
    public void SpawnScorePieceTokenClientRpc()
    {
        BoardGameMgr.Instance.SpawnScorePieceToken();
    }

    [ClientRpc]
    public void SetCurrentPlayerTurnClientRpc(int seatId)
    {
        BoardGameMgr.Instance.SetCurrentPlayerTurn(seatId);
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
    public void ChangePlayerReadyStateServerRpc(int clientId, bool v)
    {
        PlayerMgr.Instance.PlayerSetReady(clientId, v);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ClientDoActionServerRpc(PlayerActionData data)
    {
        BoardGameMgr.Instance.ClientDoAction(data);
    }
}
