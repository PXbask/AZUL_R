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
    private TMP_InputField ServerPortField;

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
        ipPlaceholderText.text = GameStatic.NgoDefaultPort.ToString();
    }

    protected override void OnRemove()
    {
        base.OnRemove();

        JoinBtn.onClick.RemoveListener(OnClickJoinBtn);
        QuitBtn.onClick.RemoveListener(OnClickQuitBtn);
    }

    private void OnClickJoinBtn()
    {
#if UNITY_EDITOR
        EventMgr.Instance.Trigger(new JoinLobbyEvent
        {
            IpAddress = GameStatic.LocalIp,
            Port = GameStatic.NgoDefaultPort
        });
#else
        if (ushort.TryParse(ServerPortField.text, out ushort port))
        {
            EventMgr.Instance.Trigger(new JoinLobbyEvent
            {
                IpAddress = ServerIpField.text,
                Port = port
            });
        }
        else
        {
            Debug.LogError($"Invalid port number: {ServerPortField.text}");
        }
#endif
    }

    private void OnClickQuitBtn()
    {
        Hide();
    }
}
