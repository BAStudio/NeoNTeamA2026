using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Camera : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public float verticalLookLimit = 90f;

    [SerializeField] private float horizontalLookLimit = 60f;
    [SerializeField] private float smoothSpeed = 10f;

    private float xRotation = 0f;
    private float yRotation = 0f;

    private bool _limitedLook = false;

    private Quaternion _targetRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        _targetRotation = transform.localRotation;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalLookLimit, verticalLookLimit);

        if (_limitedLook)
        {
            yRotation += mouseX;

            yRotation = Mathf.Clamp(yRotation, -horizontalLookLimit, horizontalLookLimit);
        }
        else
        {
            yRotation = 0f;
        }

        _targetRotation = Quaternion.Euler(xRotation, yRotation, 0f);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            _targetRotation,
            smoothSpeed * Time.deltaTime
        );
    }

    // ---------------- ADDED ----------------

    public void AddXRotation(float amount)
    {
        xRotation += amount;

        xRotation = Mathf.Clamp(xRotation, -verticalLookLimit, verticalLookLimit);
    }

    public float GetXRotation()
    {
        return xRotation;
    }

    // ---------------------------------------

    public void EnableLimitedLook()
    {
        _limitedLook = true;

        yRotation = 0f;
    }

    public void DisableLimitedLook()
    {
        _limitedLook = false;

        yRotation = 0f;
    }
} 