using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : UIPanel
{
    [SerializeField]
    private TextMeshProUGUI m_CurrentNameText;

    [SerializeField]
    private TMP_InputField m_NameInputField;

    [SerializeField]
    private Button m_ApplyNameBtn;

    [SerializeField]
    private Image m_CurrentAvatarImg;

    [SerializeField]
    private TMP_Dropdown m_AvatarDropdown;

    [SerializeField]
    private Image m_AvatarPreviewImg;

    [SerializeField]
    private Button m_ApplyAvatarBtn;

    [SerializeField]
    private Button m_QuitBtn;

    private List<string> m_AvatarIdList;

    protected override void OnInit()
    {
        base.OnInit();

        m_AvatarIdList = DataMgr.Instance.GetAllLocalAvatarIds();
        m_ApplyNameBtn.onClick.AddListener(OnApplyNameBtnClick);
        m_ApplyAvatarBtn.onClick.AddListener(OnApplyAvatarBtnClick);
        m_AvatarDropdown.AddOptions(m_AvatarIdList);
        m_AvatarDropdown.onValueChanged.AddListener(OnAvatarDropdownValueChanged);
        m_QuitBtn.onClick.AddListener(OnClickQuitBtn);

        DataMgr.Instance.LocalStorage.Name.OnValueChanged += OnPlayerNameChanged;
        DataMgr.Instance.LocalStorage.AvatarId.OnValueChanged += OnPlayerAvatarChanged;
    }

    protected override void OnShow(object data)
    {
        base.OnShow(data);

        m_CurrentNameText.text = DataMgr.Instance.LocalStorage.Name.Value;
        m_NameInputField.text = string.Empty;

        m_CurrentAvatarImg.sprite = DataMgr.Instance.GetLocalAvatarSprite(DataMgr.Instance.LocalStorage.AvatarId.Value);
        m_AvatarDropdown.value = 0;
        m_AvatarPreviewImg.sprite = DataMgr.Instance.GetLocalAvatarSprite(m_AvatarIdList[m_AvatarDropdown.value]);
    }

    private void OnPlayerAvatarChanged(string arg1, string arg2)
    {
        m_CurrentAvatarImg.sprite = DataMgr.Instance.GetLocalAvatarSprite(arg2);
    }

    private void OnPlayerNameChanged(string arg1, string arg2)
    {
        m_CurrentNameText.text = arg2;
    }

    private void OnAvatarDropdownValueChanged(int arg0)
    {
        var selectedAvatarId = m_AvatarIdList[arg0];
        m_AvatarPreviewImg.sprite = DataMgr.Instance.GetLocalAvatarSprite(selectedAvatarId);
    }

    private void OnApplyNameBtnClick()
    {
        if(string.IsNullOrEmpty(m_NameInputField.text))
        {
            UIMgr.Instance.ShowDefaultPopup("玩家名称不能为空");
            return;
        }

        DataMgr.Instance.LocalStorage.Name.Value = m_NameInputField.text;
    }

    private void OnApplyAvatarBtnClick()
    {
        var selectedAvatarId = m_AvatarIdList[m_AvatarDropdown.value];
        DataMgr.Instance.LocalStorage.AvatarId.Value = selectedAvatarId;
    }

    private void OnClickQuitBtn()
    {
        Hide();
    }

    protected override void OnRemove()
    {
        base.OnRemove();

        m_AvatarIdList.Clear();
        m_ApplyNameBtn.onClick.RemoveListener(OnApplyNameBtnClick);
        m_ApplyAvatarBtn.onClick.RemoveListener(OnApplyAvatarBtnClick);
        m_AvatarDropdown.onValueChanged.RemoveListener(OnAvatarDropdownValueChanged);
        m_QuitBtn.onClick.RemoveListener(OnClickQuitBtn);

        if(DataMgr.Instance != null && DataMgr.Instance.LocalStorage != null)
        {
            DataMgr.Instance.LocalStorage.Name.OnValueChanged -= OnPlayerNameChanged;
            DataMgr.Instance.LocalStorage.AvatarId.OnValueChanged -= OnPlayerAvatarChanged;
        }
    }
}
