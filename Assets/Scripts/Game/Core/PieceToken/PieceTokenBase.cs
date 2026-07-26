using AZUL;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPieceToken
{
    IPlaceTokenArea OwnerPlaceTokenArea { get; set; }

    bool Interactable { get; set; }

    Transform Transform { get; }

    void GotoArea(IPlaceTokenArea area);
}
public abstract class PieceTokenBase : MonoPoolObject, IPieceToken
{
    [SerializeField]
    private IPlaceTokenArea m_PlaceTokenArea = null;
    public virtual IPlaceTokenArea OwnerPlaceTokenArea
    {
        get => m_PlaceTokenArea;
        set => m_PlaceTokenArea = value;
    }

    [SerializeField]
    private bool m_Interactable = true;
    public bool Interactable
    {
        get => m_Interactable;
        set => m_Interactable = value;
    }

    public Transform Transform => transform;

    public override string PoolKey => nameof(PieceTokenBase);

    protected Tween m_GotoAreaTween = null;

    public override void OnSpawn()
    {
        base.OnSpawn();
        m_GotoAreaTween = null;
    }

    public override void OnRecycle()
    {
        base.OnRecycle();

        KillAnims();
        OwnerPlaceTokenArea = null;
    }

    public override void OnDispose()
    {
        base.OnDispose();

        KillAnims();
        OwnerPlaceTokenArea = null;
    }

    private void KillAnims()
    {
        if (m_GotoAreaTween != null)
        {
            m_GotoAreaTween.Kill();
            m_GotoAreaTween = null;
        }
    }

    public virtual void GotoArea(IPlaceTokenArea area)
    {
        if (area == null)
        {
            Debug.LogWarning("Target area is invalid.");
            return;
        }

        if (OwnerPlaceTokenArea != null)
        {
            OwnerPlaceTokenArea.RemoveToken();
            OwnerPlaceTokenArea = null;
        }
        OwnerPlaceTokenArea = area;

        var curPos = Transform.position;
        if (Vector3.Distance(curPos, area.PlaceDestination) < 0.01f)
            return;

        Interactable = false;
        m_GotoAreaTween = Transform.DOMove(area.PlaceDestination, GameStatic.TokenGoToAreaAnimInterval).SetEase(Ease.InOutSine);
        m_GotoAreaTween.onKill += () =>
        {
            Interactable = true;
            Transform.position = area.PlaceDestination;
        };
    }
}
