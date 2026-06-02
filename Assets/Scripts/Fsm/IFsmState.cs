public interface IFsmState
{
    void OnInit(FsmMgr fsm);
    void OnEnter(FsmMgr fsm);
    void OnUpdate(FsmMgr fsm);
    void OnLeave(FsmMgr fsm);
    void OnRelease(FsmMgr fsm);
}
