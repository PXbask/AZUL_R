using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AZUL
{
    public class PlayerBoard : MonoBehaviour, IPoolObject
    {
        [SerializeField]
        private Transform ScoreTrans;

        [SerializeField]
        private Transform LeftTrans;

        [SerializeField]
        private Transform RightTrans;

        [SerializeField]
        private Transform LoseTrans;

        public int SeatId { get; private set;  }

        public int Score { get; set; }

        public ScorePieceToken ScorePieceToken;

        /// <summary>
        /// 分数棋子放置区域
        /// </summary>
        public List<ScorePlaceTokenArea> ScorePlaceTokenAreas = new List<ScorePlaceTokenArea>();

        /// <summary>
        /// 左侧放置区域（使用包装类支持 Inspector 序列化）
        /// </summary>
        public List<List<NormalPlaceTokenArea>> LeftPlaceTokenAreas = new List<List<NormalPlaceTokenArea>>();

        /// <summary>
        /// 右侧彩色放置区域（使用包装类支持 Inspector 序列化）
        /// </summary>
        public List<List<ColoredPlaceTokenArea>> RightPlaceTokenAreas = new List<List<ColoredPlaceTokenArea>>();

        /// <summary>
        /// 下方减分区域（使用包装类支持 Inspector 序列化）
        /// </summary>
        public List<LosePlaceTokenArea> LosePlaceTokenAreas = new List<LosePlaceTokenArea>();

        public string PoolKey => nameof(PlayerBoard);

        public void Init(int seatId)
        {
            ClearRegisterComponents();

            SeatId = seatId;
            for (int i = 0; i < ScoreTrans.childCount; i++)
            {
                var obj = ScoreTrans.GetChild(i);
                var spta = obj.GetComponent<ScorePlaceTokenArea>();
                if(spta == null)
                {
                    Debug.LogError("cant find ScorePlaceTokenArea component!");
                }
                else
                {
                    spta.Init(0, i, PlaceTokenPositionGroup.Score, SeatId);
                    ScorePlaceTokenAreas.Add(spta);
                }
            }

            for(int i = 0; i < LeftTrans.childCount; i++)
            {
                var trans = LeftTrans.GetChild(i);
                for(int j = 0; j < trans.childCount; j++)
                {
                    var obj = trans.GetChild(j);
                    var npta = obj.GetComponent<NormalPlaceTokenArea>();
                    if (npta == null)
                    {
                        Debug.LogError("cant find NormalPlaceTokenArea component!");
                    }
                    else
                    {
                        npta.Init(i, j, PlaceTokenPositionGroup.Manual, SeatId);
                        LeftPlaceTokenAreas[i].Add(npta);
                    }
                }
            }

            for (int i = 0; i < RightTrans.childCount; i++)
            {
                var trans = RightTrans.GetChild(i);
                for (int j = 0; j < trans.childCount; j++)
                {
                    var obj = trans.GetChild(j);
                    var cpta = obj.GetComponent<ColoredPlaceTokenArea>();
                    if (cpta == null)
                    {
                        Debug.LogError("cant find ColoredPlaceTokenArea component!");
                    }
                    else
                    {
                        cpta.Init(i, j, PlaceTokenPositionGroup.Colored, SeatId);
                        RightPlaceTokenAreas[i].Add(cpta);
                    }
                }
            }

            for (int i = 0; i < LoseTrans.childCount; i++)
            {
                var obj = LoseTrans.GetChild(i);
                var lpta = obj.GetComponent<LosePlaceTokenArea>();
                if (lpta == null)
                {
                    Debug.LogError("cant find LosePlaceTokenArea component!");
                }
                else
                {
                    lpta.Init(0, i, PlaceTokenPositionGroup.Lose, SeatId);
                    LosePlaceTokenAreas.Add(lpta);
                }
            }
        }

        private void ClearRegisterComponents()
        {
            ScorePlaceTokenAreas.Clear();
            LeftPlaceTokenAreas.Clear();
            RightPlaceTokenAreas.Clear();
            LosePlaceTokenAreas.Clear();
            for(int i = 0; i < LeftTrans.childCount; i++)
            {
                var lst = new List<NormalPlaceTokenArea>(); ;
                LeftPlaceTokenAreas.Add(lst);
            }
            for (int i = 0; i < RightTrans.childCount; i++)
            {
                var lst = new List<ColoredPlaceTokenArea>(); ;
                RightPlaceTokenAreas.Add(lst);
            }
        }

        public void OnDispose()
        {
            
        }

        public void OnRecycle()
        {
            
        }

        public void OnSpawn()
        {
            
        }

        public void Recycle()
        {
            
        }
    }
}
