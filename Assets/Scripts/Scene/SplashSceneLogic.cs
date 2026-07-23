using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashSceneLogic : MonoBehaviour
{
    void Start()
    {
        SceneMgr.Instance.LoadScene(SceneStatic.MenuSceneName);
        Debug.Log("SplashSceneLogic: Load MenuScene");
    }
}
