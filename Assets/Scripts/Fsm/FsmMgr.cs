using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public enum FsmStateType
{
    Idle,
    SelectFirstPlayer,
    DealCards,
    PlayerTurn,
    GameStepSettle,
    FinalSettle,
    SettlePanel,
}

public class FsmMgr<TOwner>
{
    private readonly Dictionary<FsmStateType, IFsmState<TOwner>> _states = new Dictionary<FsmStateType, IFsmState<TOwner>>();

    /// <summary>
    /// 当前状态
    /// </summary>
    private IFsmState<TOwner> _currentState;
    public IFsmState<TOwner> CurrentState => _currentState;

    /// <summary>
    /// 上一个状态完成同步的客户端数量
    /// </summary>
    private int m_SyncStateDoneCount = 0;
    private bool m_FirstChange;

    public TOwner Owner { get; private set; }

    private CancellationTokenSource _cts;

    public FsmMgr(TOwner owner)
    {
        Owner = owner;
        _cts = new CancellationTokenSource();

        _currentState = null;
        m_SyncStateDoneCount = 0;
        m_FirstChange = true;

        Debug.Log("订阅FsmChangeStateEvent和FsmSyncEvent事件");
        EventMgr.Instance?.Subscribe<FsmChangeStateEvent>(OnFsmChangeStateEvent);
        if (NetworkManager.Singleton.IsHost)
        {
            EventMgr.Instance?.Subscribe(NoneArgEventEnum.FsmSyncEvent, OnFsmSyncEvent);
        }
    }

    ~FsmMgr()
    {
        EventMgr.Instance?.Unsubscribe<FsmChangeStateEvent>(OnFsmChangeStateEvent);
        if (NetworkManager.Singleton.IsHost)
        {
            EventMgr.Instance?.Unsubscribe(NoneArgEventEnum.FsmSyncEvent, OnFsmSyncEvent);
        }
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private void OnFsmSyncEvent()
    {
        if(!NetworkManager.Singleton.IsHost) return;

        m_SyncStateDoneCount++;
        if (m_SyncStateDoneCount > GameMgr.Instance.LobbyConfig.TotalPlayerNum)
        {
            Debug.LogWarning("[FsmMgr] SyncStateDoneCount exceeds total player number.");
            m_SyncStateDoneCount = GameMgr.Instance.LobbyConfig.TotalPlayerNum;
        }
        Debug.Log($"[FsmMgr] 当前状态:{_currentState?.GetType().Name} 同步状态: {m_SyncStateDoneCount}/{GameMgr.Instance.LobbyConfig.TotalPlayerNum}");
    }

    private void OnFsmChangeStateEvent(FsmChangeStateEvent e)
    {
        var stateType = e.stateType;
        ClientChangeState(stateType, e.data);
    }

    /// <summary>
    /// 注册状态
    /// </summary>
    public void AddState(FsmStateType stateType, IFsmState<TOwner> state)
    {
        if (_states.ContainsKey(stateType))
        {
            Debug.LogWarning($"[FsmMgr] State {stateType} already exists.");
            return;
        }

        state.OnInit(this);
        _states[stateType] = state;
    }

    /// <summary>
    /// 移除状态
    /// </summary>
    public void RemoveState(FsmStateType stateType)
    {
        if (_states.TryGetValue(stateType, out IFsmState<TOwner> state))
        {
            if (_currentState == state)
            {
                _currentState.OnLeave(this);
                _currentState = null;
            }
            state.OnRelease(this);
            _states.Remove(stateType);
        }
        else
        {
            Debug.LogWarning($"[FsmMgr] State {stateType} not found.");
        }
    }

    public void RemoveAllStates()
    {
        if (_currentState != null)
        {
            _currentState.OnLeave(this);
            _currentState = null;
        }
        foreach (var state in _states.Values)
        {
            state.OnRelease(this);
        }
        _states.Clear();
    }

    /// <summary>
    /// 切换到目标状态
    /// </summary>
    public void ClientChangeState(FsmStateType stateType, object data = null)
    {
        if (!_states.TryGetValue(stateType, out IFsmState<TOwner> nextState))
        {
            Debug.LogError($"[FsmMgr] State {stateType} not registered.");
            return;
        }

        _currentState?.OnLeave(this);
        _currentState = nextState;
        _currentState.OnEnter(this, data);

        NgoMgr.Instance.NotifyHostFsmSyncServerRpc(stateType);
    }

    public void HostChangeState(FsmStateType stateType, int data = 0)
    {
        if(!NetworkManager.Singleton.IsHost) return;

        if (!_states.TryGetValue(stateType, out IFsmState<TOwner> nextState))
        {
            Debug.LogError($"[FsmMgr] State {stateType} not registered.");
            return;
        }

        WaitForAllClientState(stateType, data, _cts.Token).Forget();
    }

    private async UniTask WaitForAllClientState(FsmStateType stateType, int data, CancellationToken ct)
    {
        bool allClientDone = m_SyncStateDoneCount >= GameMgr.Instance.LobbyConfig.TotalPlayerNum;
        await UniTask.WaitUntil(() => m_FirstChange || (m_SyncStateDoneCount >= GameMgr.Instance.LobbyConfig.TotalPlayerNum), cancellationToken: ct);
        Debug.Log("host已等待全部client完成上一状态");

        m_FirstChange = false;
        m_SyncStateDoneCount = 0;

        NgoMgr.Instance.FsmChangeStateClientRpc(stateType, data);
    }

    /// <summary>
    /// 是否拥有某状态
    /// </summary>
    public bool HasState(FsmStateType stateType)
    {
        return _states.ContainsKey(stateType);
    }

    public void Update()
    {
        _currentState?.OnUpdate(this);
    }

    public void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        _currentState?.OnLeave(this);
        foreach (var state in _states.Values)
        {
            state.OnRelease(this);
        }
        _states.Clear();
        _currentState = null;
    }
}
