using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuPanel : MonoBehaviour
{
    [SerializeField]
    private Button CreateBtn;

    [SerializeField]
    private Button JoinBtn;

    [SerializeField]
    private Button QuitBtn;

    private void OnEnable()
    {
        CreateBtn.onClick.AddListener(OnCreateBtnClick);
        JoinBtn.onClick.AddListener(OnJoinBtnClick);
        QuitBtn.onClick.AddListener(OnQuitBtnClick);
    }

    private void OnCreateBtnClick()
    {
        UIMgr.Instance.ShowPanel(UIStatic.CreatePanelName);
    }

    private void OnJoinBtnClick()
    {
        UIMgr.Instance.ShowPanel(UIStatic.JoinPanelName);
    }

    private void OnQuitBtnClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
