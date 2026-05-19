using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 150f;
    public float mouseDeadZone = 0.001f;
    public float rollLerpSpeed = 10f;

    public Transform playerBody;
    public Transform cameraPivot;

    private float xRotation = 0f;
    private float targetRoll = 0f;
    private float currentRoll = 0f;

    void Start()
{
    float savedSensitivity = PlayerPrefs.GetInt("MouseSensitivity", 15);
    mouseSensitivity = savedSensitivity * 10f;

    if (playerBody == null)
    {
        playerBody = transform;
    }

    if (cameraPivot == null)
    {
        cameraPivot = transform.Find("Camera pivot");
    }

    if (cameraPivot == null)
    {
        Debug.LogError("MouseLook needs a cameraPivot assigned, or a child object named 'Camera pivot'.", this);
        enabled = false;
        return;
    }

    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
}

    void LateUpdate()
    {
        Vector2 mouseDelta = Mouse.current != null
            ? Mouse.current.delta.ReadValue()
            : Vector2.zero;

        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

        if (Mathf.Abs(mouseX) < mouseDeadZone)
        {
            mouseX = 0f;
        }

        if (Mathf.Abs(mouseY) < mouseDeadZone)
        {
            mouseY = 0f;
        }

        playerBody.Rotate(Vector3.up * mouseX, Space.Self);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        currentRoll = Mathf.Lerp(currentRoll, targetRoll, rollLerpSpeed * Time.deltaTime);

        cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, currentRoll);
    }

    public void SetCameraRoll(float roll)
    {
        targetRoll = roll;
    }

    public void AddYawAssist(float yawDegrees)
    {
        if (playerBody != null && Mathf.Abs(yawDegrees) > 0.001f)
        {
            playerBody.Rotate(Vector3.up * yawDegrees, Space.Self);
        }
    }
}