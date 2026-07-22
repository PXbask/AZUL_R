using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    public override void OnShow(object data)
    {
        base.OnShow(data);

        JoinBtn.onClick.AddListener(OnClickJoinBtn);
        QuitBtn.onClick.AddListener(OnClickQuitBtn);

        ServerPortField.placeholder.GetComponent<TextMeshProUGUI>().text = GameStatic.NgoDefaultPort.ToString();
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
        UIMgr.Instance.HideTopPanel();
    }
}
