using UnityEngine;

public abstract class UIPanel : MonoBehaviour, IPoolObject, IUIPanel
{
    public string PanelName { get; private set; }
    public bool IsPopup { get; internal set; }

    // IPoolObject
    public virtual string PoolKey => nameof(UIPanel);
    public virtual void OnSpawn() { }
    public virtual void OnRecycle() { }
    public virtual void OnDispose() => OnRelease();
    public void Recycle() => PoolMgr.Instance.Recycle(this);

    internal void SetPanelName(string name) => PanelName = name;

    // IUIPanel
    public virtual void OnInit() { }
    public virtual void OnShow(object data) { }
    public virtual void OnUpdate() { }
    public virtual void OnHide() { }
    public virtual void OnRelease() { }
}
