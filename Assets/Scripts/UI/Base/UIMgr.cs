using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 管理器
/// - 链表维护面板层级顺序（栈式导航）
/// - 弹窗独立栈管理，不影响主界面层级
/// - 通过 PoolMgr 管理面板对象池
/// </summary>
public class UIMgr : MonoSingleton<UIMgr>
{
    [Tooltip("所有 UI 面板的父节点，挂在 Canvas 下")]
    [SerializeField] private Transform _uiRoot;

    // 主面板层级链表（链表尾 = 栈顶 = 当前激活面板）
    private readonly LinkedList<UIPanel> _panelStack = new LinkedList<UIPanel>();

    // 弹窗栈
    private readonly LinkedList<UIPanel> _popupStack = new LinkedList<UIPanel>();

    // 已初始化过的面板集合（OnInit 只调用一次）
    private readonly HashSet<string> _initializedKeys = new HashSet<string>();

    #region 注册

    /// <summary>
    /// 注册面板预制体到 PoolMgr（需在使用前完成注册）
    /// </summary>
    public void Register(string panelName, GameObject prefab)
    {
        PoolMgr.Instance.Register(panelName, prefab);
    }

    #endregion

    #region 主面板导航

    /// <summary>
    /// 打开面板（压栈），隐藏当前栈顶面板
    /// </summary>
    public UIPanel ShowPanel(string panelName, object data = null)
    {
        // 隐藏当前栈顶
        if (_panelStack.Last != null)
            HideTopInternal(_panelStack.Last.Value, temporary: true);

        UIPanel panel = GetOrCreate(panelName, isPopup: false);
        _panelStack.AddLast(panel);
        ShowInternal(panel, data);
        RefreshSiblingOrder();
        return panel;
    }

    /// <summary>
    /// 关闭当前栈顶面板，回到上一个面板
    /// </summary>
    public void HideTopPanel()
    {
        if (_panelStack.Last == null)
        {
            Debug.LogWarning("[UIMgr] Panel stack is empty.");
            return;
        }

        UIPanel top = _panelStack.Last.Value;
        _panelStack.RemoveLast();
        HideTopInternal(top, temporary: false);

        // 重新显示新栈顶
        if (_panelStack.Last != null)
            ShowInternal(_panelStack.Last.Value, null);

        RefreshSiblingOrder();
    }

    /// <summary>
    /// 关闭所有主面板
    /// </summary>
    public void HideAllPanels()
    {
        var node = _panelStack.Last;
        while (node != null)
        {
            HideTopInternal(node.Value, temporary: false);
            node = node.Previous;
        }
        _panelStack.Clear();
    }

    #endregion

    /// <summary>
    /// 关闭指定面板，从主面板栈或弹窗栈中移除
    /// </summary>
    public void HidePanel(UIPanel panel)
    {
        if (panel == null) return;

        // 先在主面板栈中查找
        LinkedListNode<UIPanel> node = _panelStack.Find(panel);
        if (node != null)
        {
            bool wasTop = node == _panelStack.Last;
            _panelStack.Remove(node);
            HideTopInternal(panel, temporary: false);

            // 若关闭的是栈顶，则重新显示新栈顶
            if (wasTop && _panelStack.Last != null)
                ShowInternal(_panelStack.Last.Value, null);

            RefreshSiblingOrder();
            return;
        }

        // 再在弹窗栈中查找
        LinkedListNode<UIPanel> popupNode = _popupStack.Find(panel);
        if (popupNode != null)
        {
            _popupStack.Remove(popupNode);
            HideTopInternal(panel, temporary: false);
            RefreshSiblingOrder();
        }
    }

    #region 弹窗

    /// <summary>
    /// 显示弹窗（弹窗叠加在主面板之上，不影响主面板层级）
    /// </summary>
    public UIPanel ShowPopup(string panelName, object data = null)
    {
        UIPanel popup = GetOrCreate(panelName, isPopup: true);
        _popupStack.AddLast(popup);
        ShowInternal(popup, data);
        RefreshSiblingOrder();
        return popup;
    }

    /// <summary>
    /// 关闭栈顶弹窗
    /// </summary>
    public void HideTopPopup()
    {
        if (_popupStack.Last == null)
        {
            Debug.LogWarning("[UIMgr] Popup stack is empty.");
            return;
        }

        UIPanel top = _popupStack.Last.Value;
        _popupStack.RemoveLast();
        HideTopInternal(top, temporary: false);
        RefreshSiblingOrder();
    }

    /// <summary>
    /// 关闭所有弹窗
    /// </summary>
    public void HideAllPopups()
    {
        var node = _popupStack.Last;
        while (node != null)
        {
            HideTopInternal(node.Value, temporary: false);
            node = node.Previous;
        }
        _popupStack.Clear();
    }

    #endregion

    #region Update

    private void Update()
    {
        // 更新主面板栈顶
        _panelStack.Last?.Value.OnUpdate();

        // 更新所有弹窗
        var node = _popupStack.First;
        while (node != null)
        {
            node.Value.OnUpdate();
            node = node.Next;
        }
    }

    #endregion

    #region 内部工具

    private UIPanel GetOrCreate(string panelName, bool isPopup)
    {
        UIPanel panel = PoolMgr.Instance.Spawn<UIPanel>(panelName, _uiRoot);
        if (panel == null) return null;

        panel.SetPanelName(panelName);
        panel.IsPopup = isPopup;

        // OnInit 只执行一次
        if (_initializedKeys.Add(panelName))
            panel.OnInit();

        return panel;
    }

    private void ReturnToPool(UIPanel panel)
    {
        PoolMgr.Instance?.Recycle(panel);
    }

    private void ShowInternal(UIPanel panel, object data)
    {
        panel.gameObject.SetActive(true);
        panel.transform.SetParent(_uiRoot, worldPositionStays: false);
        panel.OnShow(data);
    }

    /// <param name="temporary">true = 被压栈暂时隐藏，不回池；false = 正式关闭，回池</param>
    private void HideTopInternal(UIPanel panel, bool temporary)
    {
        panel.OnHide();
        if (!temporary)
            ReturnToPool(panel);
        else
            panel.gameObject.SetActive(false);
    }

    /// <summary>
    /// 刷新 UI 层级：主面板在下，弹窗在上，栈顶 sibling 最大
    /// </summary>
    private void RefreshSiblingOrder()
    {
        int order = 0;
        foreach (var p in _panelStack)
            p.transform.SetSiblingIndex(order++);

        foreach (var p in _popupStack)
            p.transform.SetSiblingIndex(order++);
    }

    #endregion

    #region 释放

    /// <summary>
    /// 释放所有面板资源（退出游戏或切换大场景时调用）
    /// </summary>
    public void ReleaseAll()
    {
        HideAllPopups();
        HideAllPanels();
        _initializedKeys.Clear();
    }

    protected override void OnDestroy()
    {
        ReleaseAll();
        base.OnDestroy();
    }

    #endregion

    public void ShowDefaultPopup(string message)
    {
        Debug.Log(message);
        ShowPopup(UIStatic.PopupPanelName, message);
    }
}

