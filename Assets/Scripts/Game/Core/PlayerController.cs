using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    private Canvas NameCanvas;

    [SerializeField]
    private TextMeshProUGUI NameText;

    public NetworkVariable<PlayerData> PlayerData = new NetworkVariable<PlayerData>(default);

    /// <summary>所有已生成的玩家控制器，key = ClientId</summary>
    public static readonly Dictionary<ulong, PlayerController> AllHuman = new Dictionary<ulong, PlayerController>();
    public static readonly Dictionary<int, PlayerController> All = new Dictionary<int, PlayerController>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        PlayerData.OnValueChanged += OnPlayerDataChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        PlayerData.OnValueChanged -= OnPlayerDataChanged;

        if (PlayerData.Value.PlayerType == PlayerType.Human &&
            AllHuman.ContainsKey(OwnerClientId))
            AllHuman.Remove(OwnerClientId);
    }

    private void OnPlayerDataChanged(PlayerData previousValue, PlayerData newValue)
    {
        if (newValue == previousValue) return;

        All[newValue.ClientId] = this;
        bool isHuman = newValue.PlayerType == PlayerType.Human;
        if (isHuman)
        {
            AllHuman[OwnerClientId] = this;
        }
        Camera.enabled = IsOwner && isHuman;
        CameraMovement.enabled = IsOwner && isHuman;

        UpdateName();
    }

    private void UpdateName()
    {
        NameText.text = PlayerData.Value.Name.ToString();
    }

    /// <summary>获取本机自己的 PlayerController</summary>
    public static PlayerController Local =>
        NetworkManager.Singleton != null &&
        AllHuman.TryGetValue(NetworkManager.Singleton.LocalClientId, out var pc) ? pc : null;

    public override string PoolKey => nameof(PlayerController);

    public static PlayerController Get(ulong clientId) =>
        AllHuman.TryGetValue(clientId, out var pc) ? pc : null;

    private void LateUpdate()
    {
        if (IsOwner && PlayerData.Value.PlayerType == PlayerType.Human) return;

        var LocalPlayerObj = Local;
        if (LocalPlayerObj == null) return;

        foreach (var item in All.Values)
        {
            item.NameFaceTo(LocalPlayerObj.transform);
        }
    }

    private void NameFaceTo(Transform trans)
    {
        Vector3 targetPos = trans.position;
        Vector3 selfPos = NameCanvas.transform.position;

        // 仅保留 Y 轴方向差，忽略高度差
        Vector3 direction = targetPos - selfPos;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) return;

        NameCanvas.transform.rotation = Quaternion.LookRotation(-direction);
    }

    public Camera GetPlayerCamera() => Camera;
}
