using UnityEngine;

public abstract class FsmState<T> : IFsmState<T>
{
    protected T m_Owner;
    public virtual void OnInit(FsmMgr<T> fsm)
    {
        m_Owner = fsm.Owner;
    }
    public virtual void OnEnter(FsmMgr<T> fsm, object data = null)
    {
        m_Owner = fsm.Owner;
        Debug.Log($"进入状态: {this.GetType().Name}");
    }
    public virtual void OnUpdate(FsmMgr<T> fsm) { }
    public virtual void OnLeave(FsmMgr<T> fsm)
    {
        Debug.Log($"离开状态: {this.GetType().Name}");
    }
    public virtual void OnRelease(FsmMgr<T> fsm) { }
}
