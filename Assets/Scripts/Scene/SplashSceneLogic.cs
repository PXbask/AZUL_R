using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashSceneLogic : MonoBehaviour
{
    void Start()
    {
        SceneManager.LoadScene(SceneStatic.MenuSceneName, LoadSceneMode.Single);
        Debug.Log("SplashSceneLogic: Load MenuScene");
    }
}
