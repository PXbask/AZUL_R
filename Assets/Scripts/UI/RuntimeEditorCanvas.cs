using RuntimeInspectorNamespace;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuntimeEditorCanvas : MonoBehaviour
{
    [SerializeField]
    private RuntimeHierarchy runtimeHierarchy;

    [SerializeField]
    private RuntimeInspector runtimeInspector;

    private bool m_ShowDetails = false;
    public bool ShowDetails
    {
        get => m_ShowDetails;
        set
        {
            m_ShowDetails = value;
            runtimeHierarchy.gameObject.SetActive(value);
            runtimeInspector.gameObject.SetActive(value);
        }
    }

    private void Awake()
    {
        ShowDetails = false;
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            ShowDetails  = !ShowDetails;
        }
    }
}
