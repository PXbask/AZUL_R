using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AZUL
{
    public class LosePlaceTokenArea : NormalPlaceTokenArea
    {
        [SerializeField]
        private int m_LosePoint;

        public int LosePoint => m_LosePoint;
    }
}
