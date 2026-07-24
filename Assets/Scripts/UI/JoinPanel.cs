using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 加入房间面板
/// </summary>
public class JoinPanel : UIPanel
{
    [SerializeField]
    private TMP_InputField ServerIpField;

    [SerializeField]
    private Button JoinBtn;

    [SerializeField]
    private Button QuitBtn;

    public override string PoolKey => nameof(JoinPanel);

    protected override void OnInit()
    {
        base.OnInit();

        JoinBtn.onClick.AddListener(OnClickJoinBtn);
        QuitBtn.onClick.AddListener(OnClickQuitBtn);

        var ipPlaceholderText = ServerIpField.placeholder.GetComponent<TextMeshProUGUI>();
        ipPlaceholderText.text = $"{GameStatic.LocalIp}:{GameStatic.NgoDefaultPort}";
    }

    protected override void OnRemove()
    {
        base.OnRemove();

        JoinBtn.onClick.RemoveListener(OnClickJoinBtn);
        QuitBtn.onClick.RemoveListener(OnClickQuitBtn);
    }

    private void OnClickJoinBtn()
    {
        string ipStr = ServerIpField.text;
        if (string.IsNullOrEmpty(ipStr))
        {
            ipStr = $"{GameStatic.LocalIp}:{GameStatic.NgoDefaultPort}";
        }
        string[] strs = ipStr.Split(':');
        if(strs.Length != 2)
        {
            UIMgr.Instance.ShowDefaultPopup("输入的格式不符合要求!");
            return;
        }

        string ipText = strs[0];
        string portText = strs[1];
        if (ushort.TryParse(portText, out ushort port))
        {
            EventMgr.Instance.Trigger(new JoinLobbyEvent
            {
                IpAddress = ipText,
                Port = port
            });
        }
        else
        {
            Debug.LogError($"Invalid port number: {portText}");
        }
    }

    private void OnClickQuitBtn()
    {
        Hide();
    }
}
