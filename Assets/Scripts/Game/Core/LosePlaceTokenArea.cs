using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AZUL
{
    public class LosePlaceTokenArea : BasePlaceTokenArea
    {
        [SerializeField]
        private int m_LosePoint;

        public int LosePoint => m_LosePoint;
    }
}
