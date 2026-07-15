using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class SettlePanel : UIPanel
{
    [SerializeField]
    private TMPro.TextMeshProUGUI m_ResultText;

    [SerializeField]
    private Button m_RestartBtn;

    public override string PoolKey => nameof(SettlePanel);

    public override void OnInit()
    {
        m_RestartBtn.onClick.AddListener(OnClickRestartBtn);
    }

    private void OnClickRestartBtn()
    {
        BoardGameMgr.Instance.GameReset();
    }

    public override void OnShow(object data)
    {
        bool host = NetworkManager.Singleton.IsHost;
        m_RestartBtn.interactable = host;

        BoardGamePlayerData[] playerDataArr = data as BoardGamePlayerData[];
        if (playerDataArr != null && playerDataArr.Length > 0)
        {
            if(playerDataArr.Length == 1)
            {
                m_ResultText.text = $"Winner: {playerDataArr[0].Name}";
            }
            else
            {
                string winners = string.Join(", ", System.Array.ConvertAll(playerDataArr, p => p.Name.ToString()));
                m_ResultText.text = $"Winners: {winners}";
            }
        }
    }
}
