using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 结算面板
/// </summary>
public class SettlePanel : UIPanel
{
    [SerializeField]
    private TMPro.TextMeshProUGUI m_ResultText;

    [SerializeField]
    private Button m_RestartBtn;

    [SerializeField]
    private Button m_HideBtn;

    [SerializeField]
    private Button m_ShowBtn;

    [SerializeField]
    private CanvasGroup m_CanvasGroup;

    [SerializeField]
    private List<TMPro.TextMeshProUGUI> m_RankTexts;

    public override string PoolKey => nameof(SettlePanel);

    protected override void OnInit()
    {
        base.OnInit();
        m_HideBtn.gameObject.SetActive(true);
        m_ShowBtn.gameObject.SetActive(false);

        m_RestartBtn.onClick.AddListener(OnClickRestartBtn);
        m_HideBtn.onClick.AddListener(OnClickHideBtn);
        m_ShowBtn.onClick.AddListener(OnClickShowBtn);
    }

    protected override void OnShow(object data)
    {
        base.OnShow(data);
        ShowPanel();

        bool host = NetworkManager.Singleton.IsHost;
        m_RestartBtn.interactable = host;

        GameResultNtf ntf = data as GameResultNtf;
        SetPanelInfo(ntf);
    }

    private void SetPanelInfo(GameResultNtf ntf)
    {
        if (ntf != null)
        {
            List<NetBoardGamePlayerData> winners = new List<NetBoardGamePlayerData>();
            for (int i = 0; i < ntf.WinnerIds.Count; i++)
            {
                for (int j = 0; j < ntf.PlayerDatas.Count; j++)
                {
                    if (ntf.PlayerDatas[j].PlayerSeatId == ntf.WinnerIds[i])
                    {
                        winners.Add(ntf.PlayerDatas[j]);
                        continue;
                    }
                }
            }

            List<string> winnerNames = new List<string>();
            for (int i = 0; i < winners.Count; i++)
            {
                winnerNames.Add(winners[i].PlayerName.ToString());
            }

            m_ResultText.text = string.Join("、", winnerNames) + "获胜";

            for (int i = 0; i < m_RankTexts.Count; i++)
            {
                if (i < ntf.PlayerDatas.Count)
                {
                    m_RankTexts[i].gameObject.SetActive(true);
                    m_RankTexts[i].text = ntf.PlayerDatas[i].PlayerName.ToString() + "：" + ntf.PlayerDatas[i].PlayerScore + "分";
                }
                else
                {
                    m_RankTexts[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            Debug.LogError("GameResultNtf is null");
            return;
        }
    }

    protected override void OnRemove()
    {
        base.OnRemove();
        m_RestartBtn.onClick.RemoveListener(OnClickRestartBtn);
        m_HideBtn.onClick.RemoveListener(OnClickHideBtn);
        m_ShowBtn.onClick.RemoveListener(OnClickShowBtn);
    }

    private void OnClickRestartBtn()
    {
        if(NetworkManager.Singleton.IsHost)
        {
            GameResetNtf ntf = new GameResetNtf();
            NetworkMgr.Instance.SendMessageToAllClients(MessageId.GameResetNtf, ntf);
        }
    }

    private void OnClickHideBtn()
    {
        HidePanel();
    }

    private void OnClickShowBtn()
    {
        ShowPanel();
    }

    private void ShowPanel()
    {
        m_CanvasGroup.alpha = 1;
        m_CanvasGroup.interactable = true;
        m_CanvasGroup.blocksRaycasts = true;

        m_HideBtn.gameObject.SetActive(true);
        m_ShowBtn.gameObject.SetActive(false);
    }

    private void HidePanel()
    {
        m_CanvasGroup.alpha = 0;
        m_CanvasGroup.interactable = false;
        m_CanvasGroup.blocksRaycasts = false;

        m_HideBtn.gameObject.SetActive(false);
        m_ShowBtn.gameObject.SetActive(true);
    }
}
