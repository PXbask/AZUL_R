using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

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

    /// <summary>
    /// 状态切换队列，确保状态切换按顺序进行
    /// </summary>
    private Queue<StateRequest> _stateRequestQueue;

    public TOwner Owner { get; private set; }

    private CancellationTokenSource _cts;

    // 队列中每条记录同时保存状态类型和附加数据
    private struct StateRequest
    {
        public FsmStateType StateType;
        public int Data;
    }

    public FsmMgr(TOwner owner)
    {
        Owner = owner;

        _currentState = null;
        m_SyncStateDoneCount = 0;
        m_FirstChange = true;

        _stateRequestQueue = new Queue<StateRequest>();
        _cts = new CancellationTokenSource();

        EventMgr.Instance?.Subscribe<FsmChangeStateEvent>(OnFsmChangeStateEvent);

        if (NetworkManager.Singleton.IsHost)
        {
            EventMgr.Instance?.Subscribe(NoneArgEventEnum.FsmSyncEvent, OnFsmSyncEvent);
            // 创建时启动队列处理循环
            ProcessStateQueueAsync(_cts.Token).Forget();
        }
    }

    ~FsmMgr()
    {
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
        if (!NetworkManager.Singleton.IsHost) return;

        if (!_states.ContainsKey(stateType))
        {
            Debug.LogError($"[FsmMgr] State {stateType} not registered.");
            return;
        }

        // 压入请求队列，由 ProcessStateQueueAsync 统一按序处理
        _stateRequestQueue.Enqueue(new StateRequest { StateType = stateType, Data = data });
    }

    /// <summary>
    /// 队列处理循环：在 FsmMgr 创建时启动，销毁时通过 CancellationToken 停止
    /// 每次从队列取出一个请求，等待所有客户端同步完成后再发送状态切换指令
    /// </summary>
    private async UniTaskVoid ProcessStateQueueAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // 等待队列有值
            await UniTask.WaitUntil(() => _stateRequestQueue.Count > 0, cancellationToken: ct);

            if (ct.IsCancellationRequested) break;

            StateRequest request = _stateRequestQueue.Dequeue();

            // 等待所有客户端完成上一个状态（首次切换直接跳过等待）
            await UniTask.WaitUntil(
                () => m_FirstChange || m_SyncStateDoneCount >= GameMgr.Instance.LobbyConfig.TotalPlayerNum,
                cancellationToken: ct
            ).TimeoutWithoutException(TimeSpan.FromSeconds(10));

            var stateType = request.StateType;
            var data = request.Data;
            Debug.Log($"[FsmMgr] host已等待全部client完成上一状态，切换至 {stateType}");

            m_FirstChange = false;
            m_SyncStateDoneCount = 0;

            NgoMgr.Instance.FsmChangeStateClientRpc(stateType, data);
        }
    }

    public void Update()
    {
        _currentState?.OnUpdate(this);
    }

    public void OnDestroy()
    {
        EventMgr.Instance?.Unsubscribe<FsmChangeStateEvent>(OnFsmChangeStateEvent);
        if (NetworkManager.Singleton && NetworkManager.Singleton.IsHost)
        {
            EventMgr.Instance?.Unsubscribe(NoneArgEventEnum.FsmSyncEvent, OnFsmSyncEvent);
        }

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
