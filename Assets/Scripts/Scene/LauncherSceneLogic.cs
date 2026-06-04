using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LauncherSceneLogic : MonoBehaviour
{
    void Start()
    {
        SceneManager.LoadScene(SceneStatic.SplashSceneName, LoadSceneMode.Single);
        Debug.Log("LauncherSceneLogic: Load SplashScene");
    }
}
