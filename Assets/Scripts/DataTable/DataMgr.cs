using cfg;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class DataMgr : MonoSingleton<DataMgr>
{
    [SerializeField]
    private LocalAvatarConfig localAvatarConfig;

    [SerializeField]
    private Dictionary<string, Sprite> localAvatarSpriteDict;

    public Tables Table { get; private set; }

    public LocalStorageData LocalStorage { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        if(localAvatarConfig != null)
        {
            localAvatarSpriteDict = new Dictionary<string, Sprite>();
            foreach (var item in localAvatarConfig.entries)
            {
                localAvatarSpriteDict[item.id] = item.avatarSprite;
            }
        }
        else
        {
            Debug.LogError("[DataMgr] LocalAvatarConfig 未设置，请在 Inspector 中设置！");
        }

        Table = new Tables(LoadJson);
        LocalStorage = new LocalStorageData();
    }

    private void Start()
    {
        LocalStorage.Start();
    }

    protected override void OnDestroy()
    {
        LocalStorage.OnDestroy();
        base.OnDestroy();
    }

    /// <summary>
    /// 从 StreamingAssets 中读取指定文件名的 JSON，返回 JArray
    /// </summary>
    private JArray LoadJson(string fileName)
    {
        string path = Application.streamingAssetsPath + "/GameCfg/" + fileName + ".json";
        if (!File.Exists(path))
        {
            Debug.LogError($"[DataMgr] JSON 文件不存在: {path}");
            return new JArray();
        }
        string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
        return JArray.Parse(json);
    }

    public Sprite GetLocalAvatarSprite(string avatarId)
    {
        if(localAvatarSpriteDict.TryGetValue(avatarId, out var sprite))
        {
            return sprite;
        }
        else
        {
            Debug.LogError($"[DataMgr] 未找到对应的本地头像: {avatarId}");
            return null;
        }
    }

    public List<string> GetAllLocalAvatarIds()
    {
        return new List<string>(localAvatarSpriteDict.Keys);
    }

    public class LocalStorageData
    {
        public const string KEY_NAME = "name";
        public const string KEY_AVATAR_ID = "avatar_id";
        public const string KEY_ENABLE_RUNTIME_LOG = "enable_runtime_log";
        public const string KEY_RUNTIME_LOG_PATH = "runtime_log_path";

        public ObservableProperty<string> Name { get;  private set; }

        public ObservableProperty<string> AvatarId { get; private set; }

        public ObservableProperty<bool> EnableRuntimeLog { get; private set; }

        public ObservableProperty<string> RuntimeLogPath { get; private set; }

        public LocalStorageData()
        {
            var name = PlayerPrefs.GetString(KEY_NAME, GameStatic.DefaultPlayerName);
            Name = new ObservableProperty<string>(name);

            var avatarId = PlayerPrefs.GetString(KEY_AVATAR_ID, GameStatic.DefaultAvatarId);
            AvatarId = new ObservableProperty<string>(avatarId);

            var enableRuntimeLog = PlayerPrefs.GetInt(KEY_ENABLE_RUNTIME_LOG,
                GameStatic.DefaultEnableRuntimeLog ? 1 : 0);
            EnableRuntimeLog = new ObservableProperty<bool>(enableRuntimeLog != 0);

            var runtimeLogPath = PlayerPrefs.GetString(KEY_RUNTIME_LOG_PATH, GameStatic.DefaultRuntimeLogPath);
            RuntimeLogPath = new ObservableProperty<string>(runtimeLogPath);

            Name.OnValueChanged += OnPlayerNameChanged;
            AvatarId.OnValueChanged += OnPlayerAvatarIdChanged;
            EnableRuntimeLog.OnValueChanged += OnEnableRuntimeLogChanged;
            RuntimeLogPath.OnValueChanged += OnRuntimeLogPathChanged;
        }

        private void OnRuntimeLogPathChanged(string arg1, string arg2)
        {
            PlayerPrefs.SetString(KEY_RUNTIME_LOG_PATH, arg2);
        }

        private void OnEnableRuntimeLogChanged(bool arg1, bool arg2)
        {
            PlayerPrefs.SetInt(KEY_ENABLE_RUNTIME_LOG, arg2 ? 1 : 0);
        }

        private void OnPlayerNameChanged(string oldValue, string newValue)
        {
            PlayerPrefs.SetString(KEY_NAME, newValue);
        }

        private void OnPlayerAvatarIdChanged(string oldValue, string newValue)
        {
            PlayerPrefs.SetString(KEY_AVATAR_ID, newValue);
        }

        public void Start()
        {
            EventMgr.Instance?.Subscribe<LocalClientConnectedEvent>(OnPlayerConnected);
        }

        private void OnPlayerConnected(LocalClientConnectedEvent e)
        {
            ClientProvideLocalInfoNtf ntf = new ClientProvideLocalInfoNtf();
            ntf.ClientId = (uint)e.ClientId;
            ntf.Name = Name.Value;
            ntf.AvatarId = AvatarId.Value;
            NetworkMgr.Instance?.SendMessageToHost(MessageId.ClientProvideLocalInfoNtf, ntf);
        }

        public void OnDestroy()
        {
            Name.OnValueChanged -= OnPlayerNameChanged;
            AvatarId.OnValueChanged -= OnPlayerAvatarIdChanged;
            EnableRuntimeLog.OnValueChanged -= OnEnableRuntimeLogChanged;
            RuntimeLogPath.OnValueChanged -= OnRuntimeLogPathChanged;

            EventMgr.Instance?.Unsubscribe<LocalClientConnectedEvent>(OnPlayerConnected);
        }
    }
}
