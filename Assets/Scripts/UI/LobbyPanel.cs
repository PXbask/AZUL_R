using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPanel : NetworkBehaviour
{
    public class PlayerDataUI
    {
        public PlayerData PlayerData;
        public bool IsReady;
    }

    [SerializeField]
    private Button StartBtn;

    [SerializeField]
    private Button QuitBtn;

    [SerializeField]
    private RectTransform PlayerDataRoot;

    private Dictionary<int, PlayerDataUI> PlayerDataUIDict = new Dictionary<int, PlayerDataUI>();

    private readonly List<PlayerDataItem> _activeItems = new List<PlayerDataItem>();

    private void OnEnable()
    {
        if(NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong obj)
    {
        if (!IsHost) return;

        PlayerData data = new PlayerData()
        {
            PlayerType = PlayerType.Human,
            ClientId = (int)obj,
            Name = string.Format("Human [{0}]", obj)
        };

        PlayerDataUI dataUI = new PlayerDataUI()
        {
            PlayerData = data,
            IsReady = false
        };

        PlayerDataUIDict[(int)obj] = dataUI;
        UpdateView();
    }

    private void OnClientDisconnected(ulong obj)
    {
        if (!IsHost) return;

        if (PlayerDataUIDict.TryGetValue((int)obj, out PlayerDataUI value))
        {
            PlayerDataUIDict.Remove((int)obj);
        }
        else
        {
            Debug.LogError($"PlayerDataUIDict does not contain clientId {(int)obj}");
        }

        UpdateView();
    }

    private void UpdateView()
    {
        List<PlayerDataUI> dataList = new List<PlayerDataUI>(PlayerDataUIDict.Values);

        // 回收多余的 item
        for (int i = _activeItems.Count - 1; i >= dataList.Count; i--)
        {
            _activeItems[i].Recycle();
            _activeItems.RemoveAt(i);
        }

        // 复用已有 item，不足则从池中补充
        for (int i = 0; i < dataList.Count; i++)
        {
            PlayerDataItem item = null;
            if (i < _activeItems.Count)
            {
                item = _activeItems[i];
            }
            else
            {
                item = PoolMgr.Instance.Spawn<PlayerDataItem>(PlayerDataItem.PoolKey, PlayerDataRoot);
                _activeItems.Add(item);
            }

            item.transform.SetParent(PlayerDataRoot, false);
            item.UpdateView(dataList[i].PlayerData, dataList[i].IsReady);
        }
    }
}
