using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public float verticalLookLimit = 90f;

    public float smoothTime = 0.08f;
    public float swayAmount = 2.5f;
    public float swaySmooth = 5f;

    public float bobPitchAmount = 0.8f;
    public float bobSpeed = 2.5f;

    public float randomStrength = 0.6f;
    public float randomSmooth = 1.5f;

    private float xRotation = 0f;
    private float currentXRotation = 0f;
    private float rotationVelocity;

    private float swayX;
    private float swayTargetX;

    private float bobTimer;

    private float noiseTime;
    private float noiseOffsetX;
    private float noiseOffsetY;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        float mouseX = Input.GetAxis("Mouse X");

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalLookLimit, verticalLookLimit);

        bobTimer += Time.deltaTime * bobSpeed;

        noiseTime += Time.deltaTime * randomSmooth;

        noiseOffsetX = (Mathf.PerlinNoise(noiseTime, 0.1f) - 0.5f) * randomStrength;
        noiseOffsetY = (Mathf.PerlinNoise(0.1f, noiseTime) - 0.5f) * randomStrength;

        float bobPitch = Mathf.Sin(bobTimer) * bobPitchAmount;

        float finalPitch = xRotation + bobPitch + noiseOffsetY;

        currentXRotation = Mathf.SmoothDampAngle(
            currentXRotation,
            finalPitch,
            ref rotationVelocity,
            smoothTime
        );

        swayTargetX = (-mouseX * swayAmount) + noiseOffsetX;
        swayX = Mathf.Lerp(swayX, swayTargetX, Time.deltaTime * swaySmooth);

        transform.localRotation = Quaternion.Euler(currentXRotation, swayX, 0f);
    }
}