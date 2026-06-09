using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AZUL
{
    public class ScorePieceToken : PieceTokenBase
    {
        [SerializeField]
        private ScorePlaceTokenArea m_PlaceTokenArea = null;
        public override IPlaceTokenArea OwnerPlaceTokenArea
        {
            get => m_PlaceTokenArea;
            set => m_PlaceTokenArea = value as ScorePlaceTokenArea;
        }

        public override string PoolKey => nameof(ScorePieceToken);
    }
}
