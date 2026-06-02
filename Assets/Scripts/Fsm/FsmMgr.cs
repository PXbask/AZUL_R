using System;
using System.Collections.Generic;
using UnityEngine;

public class FsmMgr : MonoSingleton<FsmMgr>
{
    private readonly Dictionary<Type, IFsmState> _states = new Dictionary<Type, IFsmState>();
    private IFsmState _currentState;

    public IFsmState CurrentState => _currentState;

    /// <summary>
    /// 注册状态
    /// </summary>
    public void AddState<T>() where T : FsmState, new()
    {
        Type type = typeof(T);
        if (_states.ContainsKey(type))
        {
            Debug.LogWarning($"[FsmMgr] State {type.Name} already exists.");
            return;
        }
        T state = new T();
        state.OnInit(this);
        _states[type] = state;
    }

    /// <summary>
    /// 移除状态
    /// </summary>
    public void RemoveState<T>() where T : FsmState
    {
        Type type = typeof(T);
        if (_states.TryGetValue(type, out IFsmState state))
        {
            if (_currentState == state)
            {
                _currentState.OnLeave(this);
                _currentState = null;
            }
            state.OnRelease(this);
            _states.Remove(type);
        }
        else
        {
            Debug.LogWarning($"[FsmMgr] State {type.Name} not found.");
        }
    }

    /// <summary>
    /// 切换到目标状态
    /// </summary>
    public void ChangeState<T>() where T : FsmState
    {
        Type type = typeof(T);
        if (!_states.TryGetValue(type, out IFsmState nextState))
        {
            Debug.LogError($"[FsmMgr] State {type.Name} not registered.");
            return;
        }

        _currentState?.OnLeave(this);
        _currentState = nextState;
        _currentState.OnEnter(this);
    }

    /// <summary>
    /// 是否拥有某状态
    /// </summary>
    public bool HasState<T>() where T : FsmState
    {
        return _states.ContainsKey(typeof(T));
    }

    private void Update()
    {
        _currentState?.OnUpdate(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _currentState?.OnLeave(this);
        foreach (var state in _states.Values)
        {
            state.OnRelease(this);
        }
        _states.Clear();
        _currentState = null;
    }
}
