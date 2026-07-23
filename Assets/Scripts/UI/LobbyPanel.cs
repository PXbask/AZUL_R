using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPanel : MonoBehaviour
{
    [SerializeField]
    private Button StartBtn;

    [SerializeField]
    private TextMeshProUGUI StartBtnText;

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
        EventMgr.Instance.Subscribe(NoneArgEventEnum.ClearSceneObjectEvent, OnClearScene);
        EventMgr.Instance.Subscribe(NoneArgEventEnum.PlayerStateChangeEvent, OnPlayerStateChange);
        StartBtn.onClick.AddListener(OnStartBtnClicked);
        QuitBtn.onClick.AddListener(OnQuitBtnClicked);
    }

    private void Start()
    {
        UpdateView();
    }

    private void OnDisable()
    {
        StartBtn.onClick.RemoveListener(OnStartBtnClicked);
        QuitBtn.onClick.RemoveListener(OnQuitBtnClicked);
        if (EventMgr.Instance != null)
        {
            EventMgr.Instance.Unsubscribe(NoneArgEventEnum.PlayerStateChangeEvent, OnPlayerStateChange);
            EventMgr.Instance.Unsubscribe(NoneArgEventEnum.ClearSceneObjectEvent, OnClearScene);
        }
    }

    private void OnClearScene()
    {
        foreach (var item in ActiveItems)
        {
            item.Recycle();
        }
        ActiveItems.Clear();
    }

    private void OnStartBtnClicked()
    {
        NgoMgr.Instance.NgoLoadScene(SceneStatic.GameSceneName);
    }

    private void OnQuitBtnClicked()
    {
        NgoMgr.Instance.LeaveGame();
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
                item = PoolMgr.Instance.Spawn<PlayerDataItem>(PlayerDataRoot);
                ActiveItems.Add(item);
            }

            item.transform.SetParent(PlayerDataRoot, false);
            item.UpdateView(dataList[i]);
        }

        int totalPlayerNum = GameMgr.Instance.LobbyConfig.TotalPlayerNum;
        if (dataList.Count > totalPlayerNum)
        {
            Debug.LogError($"玩家数量超过上限: {dataList.Count} > {totalPlayerNum}");
            StartBtn.interactable = false;
        }
        else if(dataList.Count < totalPlayerNum)
        {
            StartBtn.interactable = false;
            StartBtnText.text = $"等待玩家: {dataList.Count} / {totalPlayerNum}";
            Debug.Log($"等待玩家加入: {dataList.Count} / {totalPlayerNum}");
        }
        else if(dataList.Any(p => !p.IsReady))
        {
            StartBtn.interactable = false;
            StartBtnText.text = "等待玩家准备";
        }
        else
        {
            StartBtn.interactable = true;
            StartBtn.interactable = NetworkManager.Singleton.IsHost;
            StartBtnText.text = "开始游戏";
        }
    }
}
