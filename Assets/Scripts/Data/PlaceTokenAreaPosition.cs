using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AZUL
{
    public enum PlaceTokenPositionGroup : int
    {
        //可手动放置
        Manual = 0,
        Colored = 1,
        Lose = 2,
        Factory = 3,
        MidTable = 4,
        Score,
    }

    public struct PlaceTokenAreaPosition
    {
        public PlaceTokenPositionGroup PositionGroup;
        public int Row;
        public int Column;
    }
}
