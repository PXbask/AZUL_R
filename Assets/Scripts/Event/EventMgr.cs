using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件管理器
/// - 以事件类的 Type 为 key 维护监听列表
/// - 支持注册、注销、发送事件
/// </summary>
public class EventMgr : MonoSingleton<EventMgr>
{
    // Type -> 监听器列表（存储为 Delegate，运行时强转）
    private readonly Dictionary<Type, List<Delegate>> _listeners = new Dictionary<Type, List<Delegate>>();

    #region 注册 / 注销

    /// <summary>注册事件监听</summary>
    public void Subscribe<T>(Action<T> handler) where T : EventBase
    {
        Type type = typeof(T);
        if (!_listeners.ContainsKey(type))
            _listeners[type] = new List<Delegate>();

        if (!_listeners[type].Contains(handler))
            _listeners[type].Add(handler);
    }

    /// <summary>注销事件监听</summary>
    public void UnSubscribe<T>(Action<T> handler) where T : EventBase
    {
        Type type = typeof(T);
        if (_listeners.TryGetValue(type, out List<Delegate> list))
        {
            list.Remove(handler);
            if (list.Count == 0)
                _listeners.Remove(type);
        }
    }

    /// <summary>注销某类型的全部监听</summary>
    public void UnSubscribeAll<T>() where T : EventBase
    {
        _listeners.Remove(typeof(T));
    }

    #endregion

    #region 发送事件

    /// <summary>发送事件，触发所有该类型监听器</summary>
    public void Trigger<T>(T evt) where T : EventBase
    {
        Type type = typeof(T);
        if (!_listeners.TryGetValue(type, out List<Delegate> list) || list.Count == 0)
            return;

        // 拷贝一份，防止监听器内部修改列表
        Delegate[] snapshot = list.ToArray();
        foreach (Delegate d in snapshot)
        {
            try
            {
                ((Action<T>)d)?.Invoke(evt);
            }
            catch (Exception e)
            {
                Debug.LogError($"[EventMgr] Exception in handler for {type.Name}: {e}");
            }
        }
    }

    #endregion

    #region 清理

    /// <summary>清空所有监听</summary>
    public void Clear()
    {
        _listeners.Clear();
    }

    protected override void OnDestroy()
    {
        Clear();
        base.OnDestroy();
    }

    #endregion
}
