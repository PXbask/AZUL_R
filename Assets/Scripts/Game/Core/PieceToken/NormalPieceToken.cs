using AZUL;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalPieceToken : PieceTokenBase
{
    [SerializeField]
    private MeshRenderer m_Renderer;

    public cfg.AZUL.Piece PieceData {  get; private set; }

    private PieceTokenDataBinding m_Binding;

    private Tween m_SelectTween = null;
    private Tween m_DeselectTween = null;

    public override string PoolKey => nameof(NormalPieceToken);

    public void Init(int Id)
    {
        PieceData = DataMgr.Instance.Table.TbPiece.DataMap[Id];
        if(PieceData == null)
        {
            Debug.LogError($"NormalPieceToken Init Failed, Id: {Id}");
        }

        m_Binding = GetComponent<PieceTokenDataBinding>();
        if (m_Binding == null)
        {
            Debug.LogError($"NormalPieceToken Init Failed, Id: {Id}, PieceTokenDataBinding is null");
        }

        m_Renderer.material = m_Binding.GetMaterial((PieceColorType)PieceData.PieceTokenType);
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        m_SelectTween = null;
        m_DeselectTween = null;
    }

    public override void OnRecycle()
    {
        base.OnRecycle();
        if (m_SelectTween != null)
        {
            m_SelectTween.Kill();
            m_SelectTween = null;
        }
        if (m_DeselectTween != null)
        {
            m_DeselectTween.Kill();
            m_DeselectTween = null;
        }
    }

    public void PlaySelectAnim()
    {
        //表现：向上移动一定距离
        if (OwnerPlaceTokenArea != null)
        {
            if (m_DeselectTween != null)
            {
                m_DeselectTween.Kill();
                m_DeselectTween = null;
            }
            var endPos = OwnerPlaceTokenArea.PlaceDestination + Vector3.up * 0.2f;

            m_SelectTween = transform.DOMove(endPos, 0.2f);
        }
    }

    public void PlayDeselectAnim()
    {
        if (OwnerPlaceTokenArea != null)
        {
            if (m_SelectTween != null)
            {
                m_SelectTween.Kill();
                m_DeselectTween = null;
            }
            var endPos = OwnerPlaceTokenArea.PlaceDestination;

            m_SelectTween = transform.DOMove(endPos, 0.2f);
        }
    }   
}
