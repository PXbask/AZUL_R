using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AZUL
{
    public class FactoryDisk : MonoBehaviour, IPoolObject
    {
        public string PoolKey => nameof(FactoryDisk);

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
