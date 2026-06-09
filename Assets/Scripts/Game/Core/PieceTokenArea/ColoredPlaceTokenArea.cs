using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AZUL
{
    public class ColoredPlaceTokenArea : NormalPlaceTokenArea
    {
        [SerializeField]
        private PieceColorType m_ColorType;

        public PieceColorType ColorType => m_ColorType;
    }
}
