using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AZUL
{
    public class ScorePlaceTokenArea : BasePlaceTokenArea
    {
        [SerializeField]
        protected ScorePieceToken m_Token;

        public override Vector3 PlaceDestination => transform.position + Vector3.up * 0.02f;

        public override IPieceToken Token
        {
            get { return m_Token; }
            protected set { m_Token = value as ScorePieceToken; }
        }

        protected override void Awake()
        {
            base.Awake();
            Token = null;
        }

        public override void PlaceToken(IPieceToken pieceToken)
        {
            if (pieceToken is not ScorePieceToken)
            {
                Debug.LogError($"PlaceTokenArea can only place PieceToken, but got {pieceToken.GetType()}");
                return;
            }

            base.PlaceToken(pieceToken);
        }
    }
}
