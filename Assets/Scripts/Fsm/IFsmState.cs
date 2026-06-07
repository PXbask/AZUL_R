public interface IFsmState<T>
{
    void OnInit(FsmMgr<T> fsm);
    void OnEnter(FsmMgr<T> fsm, object data = null);
    void OnUpdate(FsmMgr<T> fsm);
    void OnLeave(FsmMgr<T> fsm);
    void OnRelease(FsmMgr<T> fsm);
}
