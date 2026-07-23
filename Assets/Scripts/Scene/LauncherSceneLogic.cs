using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LauncherSceneLogic : MonoBehaviour
{
    void Start()
    {
        SceneMgr.Instance.LoadScene(SceneStatic.SplashSceneName);
        Debug.Log("LauncherSceneLogic: Load SplashScene");
    }
}
