using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneMgr : MonoSingleton<SceneMgr>
{
    public void LoadScene(string sceneName)
    {
        EventMgr.Instance.Trigger(NoneArgEventEnum.ClearSceneObjectEvent);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
