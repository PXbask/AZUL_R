using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreatePanel : UIPanel
{
    [SerializeField]
    private TMP_InputField AiPort;

    [SerializeField]
    private TMP_InputField PlayerPort;

    [SerializeField]
    private Slider TotalPlayerSlider;

    [SerializeField]
    private TextMeshProUGUI TotalPlayerNumText;

    [SerializeField]
    private Slider PlayerSlider;

    [SerializeField]
    private TextMeshProUGUI PlayerNumText;

    [SerializeField]
    private Slider AiSlider;

    [SerializeField]
    private TextMeshProUGUI AiNumText;

    [SerializeField]
    private Button CreateBtn;

    [SerializeField]
    private Button QuitBtn;

    public override string PoolKey => nameof(CreatePanel);

    public override void OnShow(object data)
    {
        base.OnShow(data);

        //暂时不写Ai相关逻辑
        AiPort.placeholder.GetComponent<TextMeshProUGUI>().text = GameStatic.AiDefaultPort.ToString();

        PlayerPort.placeholder.GetComponent<TextMeshProUGUI>().text = GameStatic.NgoDefaultPort.ToString();

        TotalPlayerSlider.minValue = GameStatic.MinPlayerNum;
        TotalPlayerSlider.maxValue = GameStatic.MaxPlayerNum;
        TotalPlayerSlider.onValueChanged.AddListener(OnTotalPlayerSliderValueChanged);

        PlayerSlider.minValue = 1;
        PlayerSlider.maxValue = GameStatic.MaxPlayerNum;
        PlayerSlider.onValueChanged.AddListener(OnPlayerSliderValueChanged);

        AiSlider.interactable = false;
        AiSlider.minValue = 0;
        AiSlider.maxValue = GameStatic.MaxPlayerNum;
        AiSlider.onValueChanged.AddListener(OnAiSliderValueChanged);

        TotalPlayerSlider.value = GameStatic.MinPlayerNum;
        PlayerSlider.value = 1;
        AiSlider.value = 1;
        TotalPlayerSlider.onValueChanged.Invoke(TotalPlayerSlider.value);

        CreateBtn.onClick.AddListener(OnClickCreateBtn);
        QuitBtn.onClick.AddListener(OnClickQuitBtn);
        UpdateView();
    }

    public override void OnHide()
    {
        base.OnHide();
        TotalPlayerSlider.onValueChanged.RemoveListener(OnTotalPlayerSliderValueChanged);
        PlayerSlider.onValueChanged.RemoveListener(OnPlayerSliderValueChanged);
        AiSlider.onValueChanged.RemoveListener(OnAiSliderValueChanged);
        CreateBtn.onClick.RemoveListener(OnClickCreateBtn);
        QuitBtn.onClick.RemoveListener(OnClickQuitBtn);
    }

    private void OnTotalPlayerSliderValueChanged(float f)
    {
        TotalPlayerNumText.text = f.ToString();

        PlayerSlider.minValue = 1;
        PlayerSlider.maxValue = (int)f;
        PlayerSlider.value = Mathf.Clamp(PlayerSlider.value, PlayerSlider.minValue, PlayerSlider.maxValue);

        AiSlider.value = f - PlayerSlider.value;
    }

    private void OnPlayerSliderValueChanged(float f)
    {
        PlayerNumText.text = f.ToString();

        AiSlider.value = TotalPlayerSlider.value - f;
    }

    private void OnAiSliderValueChanged(float f)
    {
        AiNumText.text = f.ToString();
    }

    private void OnClickCreateBtn()
    {
        //判断port是否为数字，是否在范围内等
        string playerPortStr = string.IsNullOrEmpty(PlayerPort.text)
            ? GameStatic.NgoDefaultPort.ToString()
            : PlayerPort.text;

        string aiPortStr = string.IsNullOrEmpty(AiPort.text)
            ? GameStatic.AiDefaultPort.ToString()
            : AiPort.text;

        if (!ushort.TryParse(playerPortStr, out ushort playerPort))
        {
            Debug.LogWarning($"PlayerPort 输入无效: {playerPortStr}，需为 0~65535 的整数");
            return;
        }

        if (!ushort.TryParse(aiPortStr, out ushort aiPort))
        {
            Debug.LogWarning($"AiPort 输入无效: {aiPortStr}，需为 0~65535 的整数");
            return;
        }

        //触发创建房间的事件
        EventMgr.Instance.Trigger(new CreateLobbyEvent
        {
            PlayerPort = playerPort,
            AiPort = aiPort,
            TotalPlayerNum = (int)TotalPlayerSlider.value,
            PlayerNum = (int)PlayerSlider.value,
            AiNum = (int)AiSlider.value,
        });
    }

    private void OnClickQuitBtn()
    {
        UIMgr.Instance.HideTopPanel();
    }

    private void UpdateView()
    {
        TotalPlayerNumText.text = TotalPlayerSlider.value.ToString();
        PlayerNumText.text = PlayerSlider.value.ToString();
        AiNumText.text = AiSlider.value.ToString();
    }
}
