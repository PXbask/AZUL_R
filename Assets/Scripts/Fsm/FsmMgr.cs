using System;
using System.Collections.Generic;
using UnityEngine;

public class FsmMgr<TOwner>
{
    private readonly Dictionary<Type, IFsmState<TOwner>> _states = new Dictionary<Type, IFsmState<TOwner>>();
    private IFsmState<TOwner> _currentState;

    public IFsmState<TOwner> CurrentState => _currentState;

    public TOwner Owner { get; private set; }

    public FsmMgr(TOwner owner)
    {
        Owner = owner;
    }

    /// <summary>
    /// 注册状态
    /// </summary>
    public void AddState<T>() where T : FsmState<TOwner>, new()
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
    public void RemoveState<T>() where T : FsmState<TOwner>
    {
        Type type = typeof(T);
        if (_states.TryGetValue(type, out IFsmState<TOwner> state))
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
    public void ChangeState<T>(object data = null) where T : FsmState<TOwner>
    {
        Type type = typeof(T);
        if (!_states.TryGetValue(type, out IFsmState<TOwner> nextState))
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
    public bool HasState<T>() where T : FsmState<TOwner>
    {
        return _states.ContainsKey(typeof(T));
    }

    public void Update()
    {
        _currentState?.OnUpdate(this);
    }

    public void OnDestroy()
    {
        _currentState?.OnLeave(this);
        foreach (var state in _states.Values)
        {
            state.OnRelease(this);
        }
        _states.Clear();
        _currentState = null;
    }
}
