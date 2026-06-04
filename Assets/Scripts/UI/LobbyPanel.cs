using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPanel : MonoBehaviour
{
    [SerializeField]
    private Button StartBtn;

    [SerializeField]
    private Button QuitBtn;

    [SerializeField]
    private RectTransform PlayerDataRoot;

    private readonly List<PlayerDataItem> ActiveItems = new List<PlayerDataItem>();

    private void Awake()
    {
        ActiveItems.Clear();
    }

    private void OnEnable()
    {
        EventMgr.Instance.Subscribe(NoneArgEventEnum.PlayerStateChangeEvent, OnPlayerStateChange);
    }

    private void Start()
    {
        UpdateView();
    }

    private void OnDisable()
    {
        EventMgr.Instance.UnSubscribe(NoneArgEventEnum.PlayerStateChangeEvent, OnPlayerStateChange);
    }

    private void OnPlayerStateChange()
    {
        UpdateView();
    }

    private void UpdateView()
    {
        List<PlayerData> dataList = PlayerMgr.Instance.GetAllPlayers();

        // 回收多余的 item
        for (int i = ActiveItems.Count - 1; i >= dataList.Count; i--)
        {
            ActiveItems[i].Recycle();
            ActiveItems.RemoveAt(i);
        }

        // 复用已有 item，不足则从池中补充
        for (int i = 0; i < dataList.Count; i++)
        {
            PlayerDataItem item = null;
            if (i < ActiveItems.Count)
            {
                item = ActiveItems[i];
            }
            else
            {
                item = PoolMgr.Instance.Spawn<PlayerDataItem>(PlayerDataItem.PoolKey, PlayerDataRoot);
                ActiveItems.Add(item);
            }

            item.transform.SetParent(PlayerDataRoot, false);
            item.UpdateView(dataList[i]);
        }
    }
}
