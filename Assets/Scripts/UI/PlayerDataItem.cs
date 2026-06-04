using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDataItem : MonoBehaviour, IPoolObject
{
    public const string PoolKey = "PlayerDataItem";

    [SerializeField]
    private TMPro.TextMeshProUGUI NameText;

    [SerializeField]
    private Toggle ReadyToggle;

    string IPoolObject.PoolKey { get; set; } = PoolKey;

    public void UpdateView(PlayerData data)
    {
        NameText.text = data.Name;
        ReadyToggle.isOn = data.IsReady;
    }

    public void OnSpawn() { }

    public void OnRecycle()
    {
        ReadyToggle.SetIsOnWithoutNotify(false);
    }

    public void OnDispose() { }

    public void Recycle()
    {
        PoolMgr.Instance.Recycle(this);
    }
}
