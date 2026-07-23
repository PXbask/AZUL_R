using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 弹窗面板
/// </summary>
public class PopupPanel : UIPanel
{
    [SerializeField]
    private RectTransform TipRoot;

    [SerializeField]
    private Image BgImage;

    [SerializeField]
    private TextMeshProUGUI TipText;

    private Sequence m_ShowSequence = null;
    private float m_OriginAlpha = 0;

    public override string PoolKey => nameof(PopupPanel);

    protected override void OnInit()
    {
        m_OriginAlpha = BgImage.color.a;
    }

    protected override void OnShow(object data)
    {
        base.OnShow(data);

        string content = data as string;
        if(content != null)
        {
            TipText.text = content;
        }
        ResetAnims();

        m_ShowSequence = DOTween.Sequence();
        m_ShowSequence.AppendInterval(0.5f);
        m_ShowSequence.Append(BgImage.DOFade(0, 0.5f));
        m_ShowSequence.Join(TipRoot.DOAnchorPosY(250f, 0.5f));
        m_ShowSequence.AppendCallback(() => Hide());

        m_ShowSequence.Play();
    }

    protected override void OnHide()
    {
        base.OnHide();

        KillAnims();
    }

    protected override void OnRemove()
    {
        base.OnRemove();

        KillAnims();
    }

    private void ResetAnims()
    {
        KillAnims();
        TipRoot.anchoredPosition = Vector2.zero;
        BgImage.color = new Color(BgImage.color.r, BgImage.color.g, BgImage.color.b, m_OriginAlpha);
    }

    private void KillAnims()
    {
        if (m_ShowSequence != null)
        {
            m_ShowSequence.Kill();
            m_ShowSequence = null;
        }
    }
}
