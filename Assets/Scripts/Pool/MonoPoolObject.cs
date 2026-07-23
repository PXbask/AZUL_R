using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 受对象池管理的游戏物体基类
/// </summary>
public abstract class MonoPoolObject : MonoBehaviour, IPoolObject
{
    /// <summary>
    /// 注册到对象池时使用的键名
    /// </summary>
    public virtual string PoolKey => GetType().Name;

    /// <summary>
    /// 物体第一次被创建时调用（只调用一次）
    /// </summary>
    public virtual void OnCreate() { }

    /// <summary>
    /// 从池中取出、激活时调用
    /// </summary>
    public virtual void OnSpawn() { }

    /// <summary>
    /// 归还到池中、禁用时调用
    /// </summary>
    public virtual void OnRecycle() { }

    /// <summary>
    /// 池销毁时调用
    /// </summary>
    public virtual void OnDispose() { }

    /// <summary>
    /// 快捷归还自身到对象池
    /// </summary>
    public virtual void Recycle()
    {
        PoolMgr.Instance?.Recycle(this);
    }
}
