using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameInPanel : MonoBehaviour
{
    [SerializeField]
    private Button m_DisbindBtn;

    private void Start()
    {
        m_DisbindBtn.onClick.AddListener(OnDisbindBtnClick);
    }

    private void OnDestroy()
    {
        m_DisbindBtn.onClick.RemoveListener(OnDisbindBtnClick);
    }

    private void OnDisbindBtnClick()
    {
        Debug.Log("离开房间");
        NgoMgr.Instance.LeaveGame();
    }
}
