using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 匹配房间中玩家数据的 UI 显示项
/// </summary>
public class PlayerDataItem : MonoPoolObject
{
    [SerializeField]
    private TMPro.TextMeshProUGUI NameText;

    [SerializeField]
    private Toggle ReadyToggle;

    [SerializeField]
    private Image AvatarImg;

    private PlayerLobbyData m_PlayerData;

    public override string PoolKey => nameof(PlayerDataItem);

    public override void OnCreate()
    {
        ReadyToggle.onValueChanged.AddListener(OnReadyToggleChanged);
    }

    public override void OnSpawn()
    {
        ReadyToggle.SetIsOnWithoutNotify(false);
    }

    public override void OnDispose()
    {
        ReadyToggle.onValueChanged.RemoveListener(OnReadyToggleChanged);
    }

    private void OnReadyToggleChanged(bool b)
    {
        if (ReadyToggle.interactable)
        {
            ClientChangePlayerReadyNtf ntf = new ClientChangePlayerReadyNtf();
            ntf.ClientId = (uint)m_PlayerData.ClientId;
            ntf.IsReady = b;
            NetworkMgr.Instance.SendMessageToHost(MessageId.ClientChangePlayerReadyNtf, ntf);
        }
    }

    public void UpdateView(PlayerLobbyData data)
    {
        m_PlayerData = data;

        NameText.text = data.Name.ToString();
        ReadyToggle.isOn = data.IsReady;
        AvatarImg.sprite = DataMgr.Instance.GetLocalAvatarSprite(data.AvatarId.ToString());

        ReadyToggle.interactable = data.ClientId == (int)NetworkManager.Singleton.LocalClientId;
    }
}
