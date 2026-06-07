public abstract class FsmState<T> : IFsmState<T>
{
    public virtual void OnInit(FsmMgr<T> fsm) { }
    public virtual void OnEnter(FsmMgr<T> fsm, object data = null) { }
    public virtual void OnUpdate(FsmMgr<T> fsm) { }
    public virtual void OnLeave(FsmMgr<T> fsm) { }
    public virtual void OnRelease(FsmMgr<T> fsm) { }
}
