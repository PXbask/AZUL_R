using UnityEngine;

public abstract class UIPanel : MonoPoolObject
{
    public string PanelName { get; set; }

    public bool IsPopup { get; set; }

    /// <summary>
    /// 物体第一次被创建时调用（只调用一次）
    /// </summary>
    public override void OnCreate()
    {
        base.OnCreate();
        OnInit();
    }

    /// <summary>
    /// UI显示时调用开放接口
    /// </summary>
    public void PanelShow(object data)
    {
        OnShow(data);
    }

    /// <summary>
    /// UI更新时调用开放接口
    /// </summary>
    public void PanelUpdate()
    {
        OnUpdate();
    }

    /// <summary>
    /// UI隐藏时调用开放接口
    /// </summary>
    public void PanelHide()
    {
        OnHide();
    }

    /// <summary>
    /// 池销毁时调用
    /// </summary>
    public override void OnDispose()
    {
        base.OnDispose();
        OnRemove();
    }

    /// <summary>
    /// UI初始化时调用
    /// </summary>
    protected virtual void OnInit() { }

    /// <summary>
    /// UI显示时调用，data为传入的参数
    /// </summary>
    protected virtual void OnShow(object data) { }

    /// <summary>
    /// UI更新时调用
    /// </summary>
    protected virtual void OnUpdate() { }

    /// <summary>
    /// UI隐藏时调用
    /// </summary>
    protected virtual void OnHide() { }

    /// <summary>
    /// UI销毁时调用
    /// </summary>
    protected virtual void OnRemove() { }

    /// <summary>
    /// 主动隐藏自身，UIMgr 会从栈中找到并移除此面板
    /// </summary>
    protected void Hide()
    {
        UIMgr.Instance.HidePanel(this);
    }
}
