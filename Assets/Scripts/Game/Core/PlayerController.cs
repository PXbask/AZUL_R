using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Ngo玩家控制器
/// </summary>
public class PlayerController : NetPoolObject
{
    [SerializeField]
    private Camera Camera;

    [SerializeField]
    private CameraMovement CameraMovement;

    [SerializeField]
    private GameObject PlayerModel;

    private PlayerInfoCanvas m_PlayerInfoCanvas;

    public NetworkVariable<PlayerLobbyData> PlayerData = new NetworkVariable<PlayerLobbyData>(default);

    /// <summary>所有已生成的玩家控制器，key = ClientId</summary>
    public static readonly Dictionary<ulong, PlayerController> AllHuman = new Dictionary<ulong, PlayerController>();
    public static readonly Dictionary<int, PlayerController> All = new Dictionary<int, PlayerController>();

    public void SetPlayerData(PlayerLobbyData data)
    {
        PlayerData.Initialize(this);
        PlayerData.Value = data;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        PlayerData.OnValueChanged += OnPlayerDataChanged;

        ApplyPlayerData(PlayerData.Value);
        SpawnPlayerInfoCanvas();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        PlayerData.OnValueChanged -= OnPlayerDataChanged;

        if (PlayerData.Value.PlayerType == PlayerType.Human &&
            AllHuman.ContainsKey(OwnerClientId))
            AllHuman.Remove(OwnerClientId);

        RecyclePlayerInfoCanvas();

        if (IsHost && PlayerData != null)
            PlayerData.Value = default;
    }

    public override void OnCreate()
    {
        base.OnCreate();
        PlayerModel.SetActive(false);
    }

    private void OnPlayerDataChanged(PlayerLobbyData previousValue, PlayerLobbyData newValue)
    {
        Debug.Log($"PlayerData changed for ClientId {OwnerClientId}: {previousValue} -> {newValue}");
        ApplyPlayerData(newValue);
    }

    private void ApplyPlayerData(PlayerLobbyData newValue)
    {
        All[newValue.ClientId] = this;
        bool isHuman = newValue.PlayerType == PlayerType.Human;
        if (isHuman)
        {
            AllHuman[OwnerClientId] = this;
        }
        Camera.enabled = IsOwner && isHuman;
        CameraMovement.enabled = IsOwner && isHuman;
    }

    private void SpawnPlayerInfoCanvas()
    {
        if (m_PlayerInfoCanvas != null) return;

        m_PlayerInfoCanvas = PoolMgr.Instance.Spawn<PlayerInfoCanvas>();
        m_PlayerInfoCanvas.PlayerCtrl = this;
    }

    private void RecyclePlayerInfoCanvas()
    {
        if (m_PlayerInfoCanvas != null)
        {
            PoolMgr.Instance?.Recycle(m_PlayerInfoCanvas);
            m_PlayerInfoCanvas = null;
        }
    }

    /// <summary>获取本机自己的 PlayerController</summary>
    public static PlayerController Local
    {
        get
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogWarning("NetworkManager.Singleton is null. Cannot get local PlayerController.");
                return null;
            }
            if(AllHuman.TryGetValue(NetworkManager.Singleton.LocalClientId, out var pc))
            {
                return pc;
            }
            else
            {
                return null;
            }
        }
    }

    public override string PoolKey => nameof(PlayerController);

    public static PlayerController GetHuman(ulong clientId) =>
        AllHuman.TryGetValue(clientId, out var pc) ? pc : null;

    public Camera GetPlayerCamera() => Camera;
}
