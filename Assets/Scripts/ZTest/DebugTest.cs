using UnityEngine;

public class DebugTest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
            Debug.Log("This is a Log message.");

        if (Input.GetKeyDown(KeyCode.S))
            Debug.LogWarning("This is a Warning message.");

        if (Input.GetKeyDown(KeyCode.D))
            Debug.LogError("This is an Error message.");
    }
}
