using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatePlayer : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public float smoothTime = 0.08f;
    public float swayAmount = 2.5f;
    public float swaySmooth = 5f;

    private float targetRotation;
    private float currentRotation;
    private float rotationVelocity;

    private float swayZ;
    private float swayTargetZ;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;

        targetRotation += mouseX;

        currentRotation = Mathf.SmoothDampAngle(currentRotation, targetRotation, ref rotationVelocity, smoothTime);

        swayTargetZ = -mouseX * swayAmount;
        swayZ = Mathf.Lerp(swayZ, swayTargetZ, Time.deltaTime * swaySmooth);

        transform.rotation = Quaternion.Euler(0f, currentRotation, swayZ);
    }
}