using UnityEngine;

public class HeadBobbbing : MonoBehaviour
{
    [SerializeField] private Transform player;

    [Header("Movement Bob")]
    [SerializeField] private float walkBobSpeed = 3.5f;
    [SerializeField] private float runBobSpeed = 5.5f;

    [SerializeField] private float walkBobAmount = 0.03f;
    [SerializeField] private float runBobAmount = 0.06f;

    [Header("Heavy Feel")]
    [SerializeField] private float positionSmooth = 4f;
    [SerializeField] private float rotationSmooth = 3f;

    [SerializeField] private float inertiaInfluence = 1.5f;

    [Header("Look Roll (IMPORTANT)")]
    [SerializeField] private float rollAmount = 8f;   
    [SerializeField] private float rollSmooth = 2.5f;

    [Header("Sway (side weight shift)")]
    [SerializeField] private float swayAmount = 0.04f;
    [SerializeField] private float swaySmooth = 2f;

    [Header("Idle Breathing")]
    [SerializeField] private float idleSpeed = 1f;
    [SerializeField] private float idleAmount = 0.015f;

    private Rigidbody playerRb;

    private Vector3 startPos;
    private Quaternion startRot;

    private float bobTimer;
    private float idleTimer;

    private float currentRoll;
    private float targetRoll;

    private float swayX;
    private float targetSwayX;

    private Vector3 lastVelocity;

    void Start()
    {
        startPos = transform.localPosition;
        startRot = transform.localRotation;

        playerRb = player.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!playerRb) return;

        Vector3 velocity = playerRb.linearVelocity;
        Vector3 horizontal = new Vector3(velocity.x, 0, velocity.z);

        float speed = horizontal.magnitude;
        bool isMoving = speed > 0.1f;
        bool isRunning = speed > 10f;

        HandleLookRoll();
        HandleMovement(horizontal, isMoving, isRunning);
    }

    private void HandleLookRoll()
    {
        float mouseX = Input.GetAxis("Mouse X");

        float moveSpeedFactor = playerRb.linearVelocity.magnitude * 0.02f;

        targetRoll = -mouseX * rollAmount * (1f + moveSpeedFactor);

        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * rollSmooth);

        targetSwayX = mouseX * swayAmount;
        swayX = Mathf.Lerp(swayX, targetSwayX, Time.deltaTime * swaySmooth);

        transform.localRotation = startRot * Quaternion.Euler(
            0f,
            0f,
            currentRoll
        );
    }

    private void HandleMovement(Vector3 horizontal, bool isMoving, bool isRunning)
    {
        if (isMoving)
        {
            idleTimer = 0f;

            bobTimer += Time.deltaTime * (isRunning ? runBobSpeed : walkBobSpeed);

            float bobAmount = isRunning ? runBobAmount : walkBobAmount;

            float bobY = Mathf.Abs(Mathf.Sin(bobTimer)) * bobAmount;
            float bobX = Mathf.Sin(bobTimer * 0.5f) * swayAmount;

            float inertia = horizontal.magnitude * 0.01f;

            Vector3 targetPos = startPos + new Vector3(
                bobX + swayX,
                bobY - inertia * inertiaInfluence,
                0f
            );

            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                targetPos,
                Time.deltaTime * positionSmooth
            );
        }
        else
        {
            ApplyIdle();
        }
    }

    private void ApplyIdle()
    {
        idleTimer += Time.deltaTime * idleSpeed;

        float breathY = Mathf.Sin(idleTimer) * idleAmount;
        float breathX = Mathf.Sin(idleTimer * 0.5f) * idleAmount;

        Vector3 idleOffset = new Vector3(breathX, breathY, 0);

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            startPos + idleOffset,
            Time.deltaTime * 2.5f
        );

        currentRoll = Mathf.Lerp(currentRoll, 0f, Time.deltaTime * 2f);
    }
}