using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoCanvas : MonoPoolObject
{
    [SerializeField]
    private TMPro.TextMeshProUGUI playerNameText;

    [SerializeField]
    private Image playerAvatarImg;

    [SerializeField]
    private PlayerController m_PlayerCtrl;
    public PlayerController PlayerCtrl
    {
        get => m_PlayerCtrl;
        set
        {
            m_PlayerCtrl = value;
            if (m_PlayerCtrl != null)
            {
                UpdateInfo(m_PlayerCtrl.PlayerData.Value);
                m_PlayerCtrl.PlayerData.OnValueChanged += OnPlayerDataChanged;
            }
            else
            {
                playerNameText.text = string.Empty;
                playerAvatarImg.sprite = null;
            }
        }
    }

    private void UpdateInfo(PlayerLobbyData data)
    {
        if (data == default) return;
        playerNameText.text = data.Name.ToString();
        playerAvatarImg.sprite = DataMgr.Instance.GetLocalAvatarSprite(data.AvatarId.ToString());
    }

    private void OnPlayerDataChanged(PlayerLobbyData previousValue, PlayerLobbyData newValue)
    {
        UpdateInfo(newValue);
    }

    public override void OnSpawn()
    {
        base.OnSpawn();

        playerNameText.text = string.Empty;
        playerAvatarImg.sprite = null;
    }

    public override void OnRecycle()
    {
        base.OnRecycle();
        if (m_PlayerCtrl != null)
        {
            m_PlayerCtrl.PlayerData.OnValueChanged -= OnPlayerDataChanged;
            m_PlayerCtrl = null;
        }
    }

    private void LateUpdate()
    {
        if (PlayerCtrl == null) return;

        // 将UI位置设置为玩家位置的上方
        Vector3 worldPosition = PlayerCtrl.transform.position;
        transform.position = worldPosition;

        PlayerController mainPlayer = PlayerController.Local;
        if(mainPlayer == null) return;

        // 仅保留 Y 轴方向差，忽略高度差
        Vector3 direction = mainPlayer.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(-direction);
    }
}
