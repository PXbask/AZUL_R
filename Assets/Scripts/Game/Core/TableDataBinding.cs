using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TableDataBinding : MonoBehaviour
{
    [SerializeField]
    private Material m_SelfMat;

    [SerializeField]
    private Material m_OppoMat;

    [SerializeField]
    private Material m_NoneMat;

    public void Init(int gameId)
    {
        var renderers = GetComponent<Renderer>();
        var totalPlayerNum = GameMgr.Instance.LobbyConfig.TotalPlayerNum;
        if (gameId >= totalPlayerNum)
        {
            renderers.material = m_NoneMat;
        }
        else
        {
            var clientId = NetworkManager.Singleton.LocalClientId;
            var myseatId = PlayerMgr.Instance.GetSeatIdByClientId((int)clientId);
            if(myseatId == gameId)
            {
                renderers.material = m_SelfMat;
            }
            else
            {
                renderers.material = m_OppoMat;
            }
        }
    }
}
