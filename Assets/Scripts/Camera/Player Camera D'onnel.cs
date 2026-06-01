using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Camera : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public float verticalLookLimit = 90f;

    [SerializeField] private float horizontalLookLimit = 60f;
    [SerializeField] private float smoothSpeed = 10f;

    [Header("Movement Sway")]
    [SerializeField] private float swayPositionAmount = 0.03f;
    [SerializeField] private float sprintSwayBoost = 0.02f;
    [SerializeField] private float swayRollAmount = 2f;
    [SerializeField] private float swaySmooth = 8f;

    private float xRotation = 0f;
    private float yRotation = 0f;

    private bool _limitedLook = false;

    private Quaternion _targetRotation;
    private Vector3 _defaultLocalPosition;
    private Vector3 _currentSwayPosition;
    private float _swayTimer;

    private PlayerMovement _playerMovement;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        _targetRotation = transform.localRotation;
        _defaultLocalPosition = transform.localPosition;
        _currentSwayPosition = _defaultLocalPosition;

        if (transform.parent != null)
            _playerMovement = transform.parent.GetComponentInParent<PlayerMovement>();

        if (_playerMovement == null)
            _playerMovement = FindObjectOfType<PlayerMovement>();
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

        float movementIntensity = _playerMovement != null ? _playerMovement.MovementIntensity : 0f;
        bool sprinting = _playerMovement != null && _playerMovement.IsSprinting;

        float swayStrength = movementIntensity * (sprinting ? 1f + sprintSwayBoost : 1f);
        _swayTimer += Time.deltaTime * Mathf.Lerp(1.5f, 5f, movementIntensity);

        float swayX = Mathf.Sin(_swayTimer * 4f) * swayPositionAmount * swayStrength;
        float swayY = Mathf.Abs(Mathf.Sin(_swayTimer * 8f)) * swayPositionAmount * 0.5f * swayStrength;
        float swayRoll = -swayX * swayRollAmount;

        Vector3 swayTargetPosition = _defaultLocalPosition + new Vector3(swayX, swayY, 0f);
        _currentSwayPosition = Vector3.Lerp(_currentSwayPosition, swayTargetPosition, swaySmooth * Time.deltaTime);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            _targetRotation * Quaternion.Euler(0f, 0f, swayRoll),
            smoothSpeed * Time.deltaTime
        );

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            _currentSwayPosition,
            swaySmooth * Time.deltaTime
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