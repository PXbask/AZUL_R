public abstract class FsmState : IFsmState
{
    public virtual void OnInit(FsmMgr fsm) { }
    public virtual void OnEnter(FsmMgr fsm) { }
    public virtual void OnUpdate(FsmMgr fsm) { }
    public virtual void OnLeave(FsmMgr fsm) { }
    public virtual void OnRelease(FsmMgr fsm) { }
}
