using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerBody; 

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private float verticalLookLimit = 90f;

    [Header("Sway / Bob / Noise")]
    [SerializeField] private float smoothTime = 0.08f;
    [SerializeField] private float swayAmount = 2.5f;
    [SerializeField] private float swaySmooth = 5f;

    [SerializeField] private float bobPitchAmount = 0.8f;
    [SerializeField] private float bobSpeed = 2.5f;

    [SerializeField] private float randomStrength = 0.6f;
    [SerializeField] private float randomSmooth = 1.5f;

    private float xRotation;
    private float currentXRotation;
    private float rotationVelocity;

    private float swayX;
    private float swayTargetX;

    private float bobTimer;
    private float noiseTime;
    private float noiseOffsetX;
    private float noiseOffsetY;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        if (playerBody == null && transform.parent != null)
            playerBody = transform.parent;
    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);

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