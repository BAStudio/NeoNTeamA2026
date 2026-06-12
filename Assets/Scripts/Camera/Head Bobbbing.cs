using UnityEngine;

public class HeadBobbbing : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;

    [Header("Movement Bob")]
    [SerializeField] private float walkBobSpeed = 6f;
    [SerializeField] private float sprintBobSpeed = 9f;
    [SerializeField] private float crouchBobSpeed = 3f;

    [SerializeField] private float walkBobAmount = 0.04f;
    [SerializeField] private float sprintBobAmount = 0.08f;
    [SerializeField] private float crouchBobAmount = 0.02f;

    [Header("Vertical Boost")]
    [SerializeField] private float sprintVerticalBoost = 1.45f;
    [SerializeField] private float walkVerticalBoost = 1.05f;

    [Header("Idle Breathing")]
    [SerializeField] private float idleSpeed = 1f;
    [SerializeField] private float idleAmount = 0.015f;

    [Header("Look Roll")]
    [SerializeField] private float rollAmount = 5f;
    [SerializeField] private float rollSmooth = 8f;

    [Header("Smoothing")]
    [SerializeField] private float positionSmooth = 10f;

    private Vector3 startPos;
    private Quaternion startRot;

    private float bobTimer;
    private float idleTimer;
    private float currentRoll;

    private void Start()
    {
        startPos = transform.localPosition;
        startRot = transform.localRotation;

        if (movement == null)
            movement = GetComponentInParent<PlayerMovement>();
    }

    private void Update()
    {
        if (movement == null)
            return;

        HandleRotation();
        HandleHeadbob();
    }

    private void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float targetRoll = -mouseX * rollAmount;

        currentRoll = Mathf.Lerp(
            currentRoll,
            targetRoll,
            Time.deltaTime * rollSmooth
        );

        transform.localRotation = startRot * Quaternion.Euler(0f, 0f, currentRoll);
    }

    private void HandleHeadbob()
    {
        float speed = movement.CurrentHorizontalSpeed;
        bool isMoving = speed > 0.1f;

        if (!isMoving)
        {
            IdleBob();
            return;
        }

        bool isCrouching = movement.IsCrouching;
        bool isSprinting = !isCrouching && movement.IsSprinting && speed > movement.MoveSpeed * 0.85f;

        float bobSpeed;
        float bobAmount;
        float verticalBoost;

        if (isCrouching)
        {
            bobSpeed = crouchBobSpeed;
            bobAmount = crouchBobAmount;
            verticalBoost = 0.9f;
        }
        else if (isSprinting)
        {
            bobSpeed = sprintBobSpeed;
            bobAmount = sprintBobAmount;
            verticalBoost = sprintVerticalBoost;
        }
        else
        {
            bobSpeed = walkBobSpeed;
            bobAmount = walkBobAmount;
            verticalBoost = walkVerticalBoost;
        }

        bobTimer += Time.deltaTime * bobSpeed;

        float bobY = Mathf.Sin(bobTimer * 2f) * bobAmount * verticalBoost;
        float bobX = Mathf.Cos(bobTimer) * bobAmount * 0.5f;

        Vector3 targetPos = startPos + new Vector3(bobX, Mathf.Abs(bobY), 0f);

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPos,
            Time.deltaTime * positionSmooth
        );
    }

    private void IdleBob()
    {
        idleTimer += Time.deltaTime * idleSpeed;

        Vector3 idleOffset = new Vector3(
            Mathf.Sin(idleTimer * 0.5f) * idleAmount,
            Mathf.Sin(idleTimer) * idleAmount,
            0f
        );

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            startPos + idleOffset,
            Time.deltaTime * positionSmooth
        );
    }
}