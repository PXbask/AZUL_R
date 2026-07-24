using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 通用对象池管理器
/// - 支持按 key 注册预制体
/// - Spawn 时优先从池中取，无则实例化
/// - Recycle 时禁用并归还池
/// - Dispose 销毁指定池或全部池
/// </summary>
public class PoolMgr : MonoSingleton<PoolMgr>
{
    // 预制体注册表  key -> prefab
    private readonly Dictionary<string, GameObject> _registry = new Dictionary<string, GameObject>();

    // 对象池  key -> 缓存队列
    private readonly Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();

    // 各池最大缓存数量
    private readonly Dictionary<string, int> _maxSizes = new Dictionary<string, int>();

    public readonly List<GameObject> NetworkObjectPrefabList = new List<GameObject>();

    [Tooltip("池配置文件，自动注册所有预制体信息")]
    [SerializeField] private PoolConfig _config;

    [Tooltip("池对象的默认父节点，留空则使用 PoolMgr 自身")]
    [SerializeField] private Transform _poolRoot;

    protected override void Awake()
    {
        base.Awake();
        if (_poolRoot == null)
            _poolRoot = transform;
        InitFromConfig();
    }

    private void InitFromConfig()
    {
        if (_config == null) return;

        NetworkObjectPrefabList.Clear();
        foreach (PoolConfigEntry entry in _config.entries)
        {
            if (entry == null || entry.prefab == null) continue;

            IPoolObject com = entry.prefab.GetComponent<IPoolObject>();
            if(com != null && !string.IsNullOrEmpty(com.PoolKey))
            {
                Register(com.PoolKey, entry.prefab, entry.maxSize);

                //将网络预制体存入，等待NgoMgr处理
                if(entry.prefab.GetComponent<NetPoolObject>() != null)
                {
                    if (!NetworkObjectPrefabList.Contains(entry.prefab))
                    {
                        NetworkObjectPrefabList.Add(entry.prefab);
                    }
                }
            }
        }
    }

    #region 注册 / 注销

    /// <summary>注册预制体到对象池</summary>
    public void Register(string key, GameObject prefab, int maxSize = 20)
    {
        if (_registry.ContainsKey(key))
        {
            Debug.LogWarning($"[PoolMgr] Key '{key}' already registered.");
            return;
        }
        _registry[key] = prefab;
        int count = Mathf.Max(1, maxSize);
        _maxSizes[key] = count;
        Prewarm(key, prefab, count);
    }

    private void Prewarm(string key, GameObject prefab, int count)
    {
        if (!_pools.ContainsKey(key))
            _pools[key] = new Queue<GameObject>();

        Queue<GameObject> queue = _pools[key];
        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(prefab, _poolRoot);
            IPoolObject obj = go.GetComponent<IPoolObject>();
            obj?.OnCreate();

            go.SetActive(false);
            queue.Enqueue(go);
        }
    }

    /// <summary>注销并销毁指定池的所有对象</summary>
    public void Unregister(string key)
    {
        Dispose(key);
        _registry.Remove(key);
    }

    #endregion

    #region Spawn / Recycle

    /// <summary>从池中取出或新建对象</summary>
    public T Spawn<T>(string key, Transform parent = null) where T : MonoBehaviour, IPoolObject
    {
        IPoolObject obj = GetOrCreate(key, parent);
        if (obj == null) return null;
        (obj as MonoBehaviour).gameObject.SetActive(true);
        obj.OnSpawn();
        return obj as T;
    }

    public T Spawn<T>(Transform parent = null) where T : MonoBehaviour, IPoolObject
    {
        string key = typeof(T).Name;
        return Spawn<T>(key, parent);
    }

    /// <summary>从池中取出或新建对象（非泛型）</summary>
    public IPoolObject Spawn(string key, Transform parent = null)
    {
        IPoolObject obj = GetOrCreate(key, parent);
        if (obj == null) return null;
        (obj as MonoBehaviour).gameObject.SetActive(true);
        obj.OnSpawn();
        return obj;
    }

    /// <summary>
    /// 专供网络对象使用：从池中取出但不 SetActive，
    /// 由 NGO 的 SpawnWithOwnership 控制激活时机
    /// </summary>
    public T SpawnNetObj<T>() where T : NetPoolObject
    {
        string key = typeof(T).Name;
        IPoolObject obj = GetOrCreate(key, null, true);
        if (obj == null) return null;
        (obj as MonoBehaviour).gameObject.SetActive(true);
        obj.OnSpawn();
        return obj as T;
    }

    /// <summary>归还对象到池</summary>
    public void Recycle(IPoolObject obj, bool isNetobj = false)
    {
        if (obj == null) return;
        MonoBehaviour mb = obj as MonoBehaviour;
        if (mb == null) return;

        string key = obj.PoolKey;
        if (!_pools.ContainsKey(key))
            _pools[key] = new Queue<GameObject>();

        int max = _maxSizes.TryGetValue(key, out int m) ? m : 20;
        if (_pools[key].Count >= max)
        {
            obj.OnDispose();
            Destroy(mb.gameObject);
            return;
        }

        obj.OnRecycle();
        mb.transform.SetParent(isNetobj ? null : _poolRoot);
        mb.gameObject.SetActive(false);
        _pools[key].Enqueue(mb.gameObject);
    }

    #endregion

    #region 销毁

    /// <summary>销毁指定池的所有缓存对象</summary>
    public void Dispose(string key)
    {
        if (!_pools.TryGetValue(key, out Queue<GameObject> queue)) return;
        foreach (var go in queue)
        {
            if (go)
            {
                go.GetComponent<IPoolObject>()?.OnDispose();
            }
            Destroy(go);
        }
        queue.Clear();
        _pools.Remove(key);
    }

    /// <summary>销毁所有池的所有缓存对象</summary>
    public void DisposeAll()
    {
        foreach (var key in new List<string>(_pools.Keys))
            Dispose(key);
    }

    #endregion

    #region 查询

    /// <summary>获取指定池当前缓存数量</summary>
    public int CountInPool(string key)
    {
        return _pools.TryGetValue(key, out Queue<GameObject> q) ? q.Count : 0;
    }

    public GameObject GetPrefabByKey(string key)
    {
        if(_registry.TryGetValue(key, out var res))
        {
            return res;
        }
        else
        {
            Debug.LogError($"can not find key:{key} in poolmgr registry");
            return null;
        }
    }

    #endregion

    #region 内部

    private IPoolObject GetOrCreate(string key, Transform parent, bool isNetObj = false)
    {
        // 从池中取
        if (_pools.TryGetValue(key, out Queue<GameObject> queue) && queue.Count > 0)
        {
            GameObject pooled = queue.Dequeue();

            var par = parent != null ? parent : _poolRoot;
            if(!isNetObj)
                pooled.transform.SetParent(par);

            return pooled.GetComponent<IPoolObject>();
        }

        // 实例化新对象
        if (!_registry.TryGetValue(key, out GameObject prefab))
        {
            Debug.LogError($"[PoolMgr] Prefab for key '{key}' not registered.");
            return null;
        }

        GameObject go = Instantiate(prefab, parent != null ? parent : _poolRoot);
        IPoolObject obj = go.GetComponent<IPoolObject>();
        if (obj == null)
        {
            Debug.LogError($"[PoolMgr] Prefab '{key}' has no IPoolObject component.");
            Destroy(go);
            return null;
        }
        else
        {
            obj.OnCreate();
            return obj;
        }
    }

    protected override void OnDestroy()
    {
        DisposeAll();
        base.OnDestroy();
    }

    #endregion
}
