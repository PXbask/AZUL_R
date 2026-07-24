using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PoolObjectSpawner<T> : INetworkPrefabInstanceHandler where T : NetPoolObject
{
    private readonly string PoolKey;

    public GameObject Prefab => PoolMgr.Instance.GetPrefabByKey(PoolKey);

    public PoolObjectSpawner(string poolKey)
    {
        PoolKey = poolKey;
    }

    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        T obj = PoolMgr.Instance.SpawnNetObj<T>();
        if (obj == null)
        {
            Debug.LogError($"[PoolObjectSpawner<{typeof(T).Name}>] 对象池 Spawn 失败，Key={PoolKey}");
            return null;
        }
        obj.transform.SetPositionAndRotation(position, rotation);
        return obj.GetComponent<NetworkObject>();
    }

    public void Destroy(NetworkObject networkObject)
    {
        T obj = networkObject.GetComponent<T>();
        if (obj != null)
        {
            PoolMgr.Instance.Recycle(obj, true);
        }
        else
            Object.Destroy(networkObject.gameObject);
    }
}
