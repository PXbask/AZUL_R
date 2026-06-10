using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AZUL
{
    public class FactoryDisk : MonoBehaviour, IPoolObject
    {
        [SerializeField]
        public List<NormalPlaceTokenArea> PlaceTokenAreas = new List<NormalPlaceTokenArea>();

        public string PoolKey => nameof(FactoryDisk);

        public void Init()
        {
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
