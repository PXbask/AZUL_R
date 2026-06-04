using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEditor.Experimental.GraphView;
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

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (EventMgr.Instance != null)
        {
            EventMgr.Instance.UnSubscribe<CreateLobbyEvent>(OnCreateLobby);
            EventMgr.Instance.UnSubscribe<JoinLobbyEvent>(OnJoinLobby);
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
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

    public void NgoLoadScene(string sceneName)
    {
        if (!NetworkManager.Singleton.IsHost) return;

        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);

        UIMgr.Instance.HideAllPanels();
        UIMgr.Instance.HideAllPopups();
    }
}
