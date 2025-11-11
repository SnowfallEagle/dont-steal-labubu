using UnityEngine;

public class GUICameraController : MonoBehaviour
{
    private Camera guiCamera;

    void Start()
    {
        guiCamera = GetComponent<Camera>();
        if (guiCamera == null)
        {
            Debug.LogError("GUICameraController: No camera component found!");
            return;
        }

        UpdateCameraSize();
    }

    void Update()
    {
        UpdateCameraSize();
    }

    void UpdateCameraSize()
    {
        if (guiCamera == null) return;

        guiCamera.orthographicSize = Screen.height * 0.5f;
    }
} 