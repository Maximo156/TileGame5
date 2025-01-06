using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraZoom : MonoBehaviour
{
    public float min = 3;
    public float max = 10;
    public float speed = 0.3f;
    public float smoothTime = 0.2f;

    private float targetZoom;
    private float velocity = 0;

    Camera cam;
    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        targetZoom = cam.orthographicSize;
    }

    private void FixedUpdate()
    {
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetZoom, ref velocity, smoothTime);
    }

    public void OnLook(InputAction.CallbackContext value)
    {
        if (Keyboard.current.ctrlKey.isPressed)
        {
            var dir = value.ReadValue<Vector2>();
            if (dir.magnitude > 0)
            {
                targetZoom = Mathf.Clamp(targetZoom - Mathf.Sign(dir.y)*speed, min, max);
            }
        }
    }
}
