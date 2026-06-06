using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerBoardSpawner : INetworkPrefabInstanceHandler
{
    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        throw new System.NotImplementedException();
    }

    public void Destroy(NetworkObject networkObject)
    {
        throw new System.NotImplementedException();
    }
}
