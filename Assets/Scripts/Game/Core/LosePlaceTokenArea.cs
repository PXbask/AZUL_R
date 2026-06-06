using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AZUL
{
    public class LosePlaceTokenArea : PlaceTokenArea
    {
        [SerializeField]
        private int m_LosePoint;

        public int LosePoint => m_LosePoint;
    }
}
