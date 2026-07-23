/// <summary>
/// 受对象池管理的物体接口
/// </summary>
public interface IPoolObject
{
    /// <summary>
    /// 注册到对象池时使用的键名
    /// </summary>
    string PoolKey { get; }

    /// <summary>
    /// 物体第一次被创建时调用（只调用一次）
    /// </summary>
    void OnCreate();

    /// <summary>
    /// 从池中取出、激活时调用
    /// </summary>
    void OnSpawn();

    /// <summary>
    /// 归还到池中、禁用时调用
    /// </summary>
    void OnRecycle();

    /// <summary>
    /// 池销毁时调用
    /// </summary>
    void OnDispose();
}
