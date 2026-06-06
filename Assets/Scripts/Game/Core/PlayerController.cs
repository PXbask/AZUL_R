using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField]
    private Camera Camera;

    [SerializeField]
    private CameraMovement CameraMovement;

    [SerializeField]
    private Canvas NameCanvas;

    /// <summary>所有已生成的玩家控制器，key = ClientId</summary>
    public static readonly Dictionary<ulong, PlayerController> All = new Dictionary<ulong, PlayerController>();

    public override void OnNetworkSpawn()
    {
        All[OwnerClientId] = this;

        Camera.enabled = IsOwner;
        CameraMovement.enabled = IsOwner;
    }

    public override void OnNetworkDespawn()
    {
        All.Remove(OwnerClientId);
    }

    /// <summary>获取本机自己的 PlayerController</summary>
    public static PlayerController Local =>
        NetworkManager.Singleton != null &&
        All.TryGetValue(NetworkManager.Singleton.LocalClientId, out var pc) ? pc : null;

    private void LateUpdate()
    {
        if (IsOwner) return;

        var LocalPlayerObj = Local;
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
}
