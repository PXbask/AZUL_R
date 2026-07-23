using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AZUL
{
    /// <summary>
    /// 工厂圆盘
    /// </summary>
    public class FactoryDisk : MonoPoolObject
    {
        [SerializeField]
        private int id;

        [SerializeField]
        public List<NormalPlaceTokenArea> PlaceTokenAreas = new List<NormalPlaceTokenArea>();

        GUIStyle m_Style;

        public override string PoolKey => nameof(FactoryDisk);

        public override void OnCreate()
        {
#if UNITY_EDITOR
            m_Style = new GUIStyle();
            m_Style.normal.textColor = Color.black;
            m_Style.fontStyle = FontStyle.Bold;
#endif
        }

        public void Init(int id)
        {
            this.id = id;

            for(int i = 0; i < PlaceTokenAreas.Count; i++)
            {
                var item = PlaceTokenAreas[i];
                item.Init(0, i, PlaceTokenPositionGroup.Factory, -1);
            }
        }

        public NormalPlaceTokenArea GetArea(int index)
        {
            try
            {
                return PlaceTokenAreas[index];
            }
            catch (System.Exception e)
            {
                Debug.LogError($"error:{e.Message}");
                return null;
            }
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"[{id}]", m_Style);
#endif
        }
    }
}
