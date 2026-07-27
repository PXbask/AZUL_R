using cfg;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        public ObservableProperty<string> Name { get;  private set; }

        public ObservableProperty<string> AvatarId { get; private set; }

        public LocalStorageData()
        {
            var name = PlayerPrefs.GetString(KEY_NAME, GameStatic.DefaultPlayerName);
            Name = new ObservableProperty<string>(name);

            var avatarId = PlayerPrefs.GetString(KEY_AVATAR_ID, GameStatic.DefaultAvatarId);
            AvatarId = new ObservableProperty<string>(avatarId);

            Name.OnValueChanged += OnPlayerNameChanged;
            AvatarId.OnValueChanged += OnPlayerAvatarIdChanged;
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
            PlayerLocalInfoData data = new PlayerLocalInfoData();
            data.Name = Name.Value;
            data.AvatarId = AvatarId.Value;

            NgoMgr.Instance?.NotifyAddPlayerServerRpc((int)e.ClientId, data);
        }

        public void OnDestroy()
        {
            Name.OnValueChanged -= OnPlayerNameChanged;
            AvatarId.OnValueChanged -= OnPlayerAvatarIdChanged;

            EventMgr.Instance?.Unsubscribe<LocalClientConnectedEvent>(OnPlayerConnected);
        }
    }
}
