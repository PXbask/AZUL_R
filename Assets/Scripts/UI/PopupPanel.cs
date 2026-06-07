using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupPanel : UIPanel
{
    [SerializeField]
    private RectTransform TipRoot;

    [SerializeField]
    private Image BgImage;

    [SerializeField]
    private TextMeshProUGUI TipText;

    private Sequence m_ShowSequence = null;

    public override string PoolKey => nameof(PopupPanel);
    public override void OnShow(object data)
    {
        base.OnShow(data);

        string content = data as string;
        TipRoot.anchoredPosition = Vector2.zero;
        if(content != null)
        {
            TipText.text = content;
        }

        m_ShowSequence = DOTween.Sequence();
        m_ShowSequence.AppendInterval(0.5f);
        m_ShowSequence.Append(BgImage.DOFade(0, 0.5f));
        m_ShowSequence.Join(TipRoot.DOAnchorPosY(250f, 0.5f));
        m_ShowSequence.AppendCallback(() => Hide());

        m_ShowSequence.Play();
    }

    public override void OnHide()
    {
        base.OnHide();

        if(m_ShowSequence != null)
        {
            m_ShowSequence.Kill();
            m_ShowSequence = null;
        }
    }
}
