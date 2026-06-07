using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDataItem : MonoBehaviour, IPoolObject
{

    [SerializeField]
    private TMPro.TextMeshProUGUI NameText;

    [SerializeField]
    private Toggle ReadyToggle;

    string IPoolObject.PoolKey => nameof(PlayerDataItem);

    private void Start()
    {
        ReadyToggle.onValueChanged.AddListener(OnReadyToggleChanged);
    }

    private void OnDestroy()
    {
        ReadyToggle.onValueChanged.RemoveListener(OnReadyToggleChanged);
    }

    private void OnReadyToggleChanged(bool b)
    {
        if (ReadyToggle.interactable)
        {
            NgoMgr.Instance.ChangePlayerReadyStateServerRpc((int)NetworkManager.Singleton.LocalClientId, b);
        }
    }

    public void UpdateView(PlayerData data)
    {
        NameText.text = data.Name.ToString();
        ReadyToggle.isOn = data.IsReady;

        ReadyToggle.interactable = data.ClientId == (int)NetworkManager.Singleton.LocalClientId;
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
