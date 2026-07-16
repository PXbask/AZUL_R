using System;
using System.Collections.Generic;
using UnityEngine;

public class DebugConsole : MonoSingleton<DebugConsole>
{
    private struct LogEntry
    {
        public string time;
        public string message;
        public string stackTrace;
        public LogType type;
    }

    private readonly List<LogEntry> _logs = new List<LogEntry>();

    private bool _visible = true;
    private bool _showStack = false;
    private int _selectedIndex = -1;

    private Rect _windowRect = new Rect(20, 20, 700, 400);
    private Vector2 _logScrollPos;
    private Vector2 _stackScrollPos;

    private bool _showLog = true;
    private bool _showWarning = true;
    private bool _showError = true;

    private const int WindowId = 20260101;
    private const int MaxLogs = 500;

    // Colors
    private static readonly Color ColorLog     = Color.white;
    private static readonly Color ColorWarning = new Color(1f, 0.85f, 0f);
    private static readonly Color ColorError   = new Color(1f, 0.35f, 0.35f);

    // Cached textures and styles to avoid allocating per-frame
    private Texture2D _texSelected;
    private Texture2D _texEven;
    private Texture2D _texOdd;
    private GUIStyle _styleSelected;
    private GUIStyle _styleEven;
    private GUIStyle _styleOdd;

    protected override void Awake()
    {
        base.Awake();
        Application.logMessageReceived += OnLogReceived;
        CreateCachedStyles();
    }

    protected override void OnDestroy()
    {
        Application.logMessageReceived -= OnLogReceived;
        DestroyCachedTextures();
        base.OnDestroy();
    }

    private void CreateCachedStyles()
    {
        _texSelected = MakeTex(2, 2, new Color(0.3f, 0.5f, 1f, 0.5f));
        _texEven     = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.8f));
        _texOdd      = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.15f, 0.8f));

        _styleSelected = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
        _styleEven     = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
        _styleOdd      = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, wordWrap = false };

        _styleSelected.normal.background = _texSelected;
        _styleEven.normal.background     = _texEven;
        _styleOdd.normal.background      = _texOdd;
    }

    private void DestroyCachedTextures()
    {
        void DestroySafe(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        DestroySafe(_texSelected);
        DestroySafe(_texEven);
        DestroySafe(_texOdd);

        _texSelected = _texEven = _texOdd = null;
        _styleSelected = _styleEven = _styleOdd = null;
    }

    private void OnLogReceived(string message, string stackTrace, LogType type)
    {
        if (_logs.Count >= MaxLogs)
            _logs.RemoveAt(0);

        _logs.Add(new LogEntry
        {
            time       = DateTime.Now.ToString("HH:mm:ss.fff"),
            message    = message,
            stackTrace = stackTrace,
            type       = type
        });

        // Auto-scroll to bottom
        _logScrollPos.y = float.MaxValue;
    }

    private void OnGUI()
    {
        if (!_visible) return;

        _windowRect = GUI.Window(WindowId, _windowRect, DrawWindow, "Debug Console");
    }

    private void DrawWindow(int id)
    {
        DrawToolbar();

        float stackHeight = _showStack && _selectedIndex >= 0 ? 100f : 0f;
        float logHeight   = _windowRect.height - 50f - stackHeight;

        // Log list
        _logScrollPos = GUILayout.BeginScrollView(_logScrollPos, GUILayout.Height(logHeight));

        for (int i = 0; i < _logs.Count; i++)
        {
            var entry = _logs[i];
            if (!IsVisible(entry.type)) continue;

            Color prev = GUI.color;
            GUI.color = GetColor(entry.type);

            string label = $"[{entry.time}][{GetTag(entry.type)}] {entry.message}";

            if (GUILayout.Button(label, GetStyle(i), GUILayout.ExpandWidth(true)))
            {
                _selectedIndex = (_selectedIndex == i) ? -1 : i;
                _showStack     = _selectedIndex >= 0;
            }

            GUI.color = prev;
        }

        GUILayout.EndScrollView();

        // Stack trace
        if (_showStack && _selectedIndex >= 0 && _selectedIndex < _logs.Count)
        {
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            _stackScrollPos = GUILayout.BeginScrollView(_stackScrollPos, GUILayout.Height(stackHeight));
            GUILayout.Label(_logs[_selectedIndex].stackTrace);
            GUILayout.EndScrollView();
        }

        GUI.DragWindow(new Rect(0, 0, _windowRect.width, 20));
    }

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Clear", GUILayout.Width(60)))
        {
            _logs.Clear();
            _selectedIndex = -1;
            _showStack     = false;
        }

        GUILayout.Space(10);

        GUI.color = _showLog ? ColorLog : Color.gray;
        _showLog = GUILayout.Toggle(_showLog, "Log", GUILayout.Width(50));

        GUI.color = _showWarning ? ColorWarning : Color.gray;
        _showWarning = GUILayout.Toggle(_showWarning, "Warning", GUILayout.Width(70));

        GUI.color = _showError ? ColorError : Color.gray;
        _showError = GUILayout.Toggle(_showError, "Error", GUILayout.Width(60));

        GUI.color = Color.white;

        GUILayout.FlexibleSpace();

        int logCount  = 0, warnCount = 0, errCount = 0;
        foreach (var e in _logs)
        {
            if (e.type == LogType.Log) logCount++;
            else if (e.type == LogType.Warning) warnCount++;
            else if (e.type == LogType.Error || e.type == LogType.Exception) errCount++;
        }
        GUILayout.Label($"Log:{logCount}  Warn:{warnCount}  Err:{errCount}");

        GUILayout.EndHorizontal();
    }

    private bool IsVisible(LogType type)
    {
        switch (type)
        {
            case LogType.Log:       return _showLog;
            case LogType.Warning:   return _showWarning;
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:    return _showError;
            default:                return true;
        }
    }

    private Color GetColor(LogType type)
    {
        switch (type)
        {
            case LogType.Warning:                        return ColorWarning;
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:                         return ColorError;
            default:                                     return ColorLog;
        }
    }

    private string GetTag(LogType type)
    {
        switch (type)
        {
            case LogType.Warning:   return "Warning";
            case LogType.Error:     return "Error";
            case LogType.Exception: return "Exception";
            case LogType.Assert:    return "Assert";
            default:                return "Log";
        }
    }

    private GUIStyle GetStyle(int index)
    {
        if (_styleSelected == null) CreateCachedStyles();
        if (index == _selectedIndex) return _styleSelected;
        return (index % 2 == 0) ? _styleEven : _styleOdd;
    }

    private Texture2D MakeTex(int width, int height, Color color)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = color;
        Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        tex.SetPixels(pix);
        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        return tex;
    }

    /// <summary>
    /// 按 F1 切换显示/隐藏
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            _visible = !_visible;
    }
}
