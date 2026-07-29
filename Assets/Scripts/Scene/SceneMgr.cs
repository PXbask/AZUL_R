using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMgr : MonoSingleton<SceneMgr>
{
    public Scene GetActiveScene()
    {
        return SceneManager.GetActiveScene();
    }

    public void LoadScene(string sceneName)
    {
        UIMgr.Instance.HideAllPanels();
        EventMgr.Instance.Trigger(NoneArgEventEnum.ClearSceneObjectEvent);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
