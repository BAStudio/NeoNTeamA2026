using System.Collections;
using UnityEngine;

public class PlayerMovement : BasePhysics
{
    [Header("References")]
    [SerializeField] private Transform orientation;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float sprintSpeed = 12f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField, Range(0f, 1f)] private float airControl = 0.4f;

    public bool IsSprinting => isSprinting;
    public float MovementIntensity => Mathf.Clamp01(new Vector2(currentVelocity.x, currentVelocity.z).magnitude / Mathf.Max(0.0001f, sprintSpeed));

    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float jumpStaminaCost = 1f;

    [Header("Wall Climb")]
    [SerializeField] private LayerMask climbLayer;
    [SerializeField] private float wallDetectDistance = 1f;
    [SerializeField] private float climbSpeed = 4f;

    [SerializeField] private float chestHeight = 1f;
    [SerializeField] private float headHeight = 1.8f;

    [Header("Climb Stamina")]
    [SerializeField] private float climbStaminaDrain = 0.5f;

    [Header("Ledge Grab")]
    [SerializeField] private float ledgeCheckHeight = 2f;
    [SerializeField] private float ledgeForwardOffset = 0.5f;

    [SerializeField] private float hangOffsetDown = 1f;
    [SerializeField] private float hangOffsetBack = 0.4f;

    [SerializeField] private float ledgeClimbDuration = 0.25f;

    private bool _allowedToMove = true;

    private enum MovementState
    {
        Normal,
        Climbing,
        LedgeGrab,
        ClimbUp
    }

    private MovementState state = MovementState.Normal;

    private Vector3 inputDirection;
    private bool isSprinting;

    private PlayerStamina stamina;

    private Vector3 wallNormal;
    private Vector3 ledgeTargetPosition;

    private Coroutine ledgeRoutine;

    private void Start()
    {
        NotePad.StopMovement += StopMovement;
        NotePad.ContinueMovement += ContinueMovement;
    }

    protected override void Awake()
    {
        base.Awake();

        stamina = PlayerStamina.Instance;

        if (orientation == null && Camera.main != null)
            orientation = Camera.main.transform.parent;
    }

    protected override void Update()
    {
        base.Update();

        if (stamina == null)
            stamina = PlayerStamina.Instance;

        HandleInput();

        switch (state)
        {
            case MovementState.Normal:
                HandleMovement();
                HandleJump();
                HandleWallDetection();
                break;

            case MovementState.Climbing:
                HandleClimbing();
                break;
        }

        Vector3 moveDelta = currentVelocity * Time.deltaTime;

        Debug.DrawRay(transform.position, moveDelta, Color.cyan);

        SafeMove(moveDelta);

        if(!_allowedToMove)
        {
            StoppingMovenent();
        }
    }

    private void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        isSprinting = Input.GetKey(KeyCode.LeftShift);

        if (orientation == null)
        {
            inputDirection = new Vector3(-h, 0f, -v).normalized;
            return;
        }

        Vector3 forward = orientation.forward;
        Vector3 right = orientation.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        inputDirection = (-forward * v - right * h).normalized;
    }

    private void HandleMovement()
    {
        float control = IsGrounded ? 1f : airControl;

        float currentSpeed =
            isSprinting && IsGrounded
            ? sprintSpeed
            : moveSpeed;

        Vector3 targetVelocity =
            inputDirection * currentSpeed;

        currentVelocity.x = Mathf.MoveTowards(
            currentVelocity.x,
            targetVelocity.x,
            acceleration * control * Time.deltaTime
        );

        currentVelocity.z = Mathf.MoveTowards(
            currentVelocity.z,
            targetVelocity.z,
            acceleration * control * Time.deltaTime
        );
    }

    private void HandleJump()
    {
        if (stamina == null)
            return;

        if (IsGrounded &&
            Input.GetKeyDown(KeyCode.Space) &&
            stamina.stamina >= jumpStaminaCost)
        {
            stamina.ConsumeStamina(jumpStaminaCost);

            currentVelocity.y = 0f;

            ApplyImpulse(Vector3.up * jumpForce);
        }
    }

    private void HandleWallDetection()
    {
        if (!Input.GetKey(KeyCode.Space))
            return;

        if (stamina == null || stamina.stamina <= 0f)
            return;

        Vector3 chestOrigin =
            transform.position +
            Vector3.up * chestHeight;

        Vector3 headOrigin =
            transform.position +
            Vector3.up * headHeight;

        Debug.DrawRay(
            chestOrigin,
            transform.forward * wallDetectDistance,
            Color.red
        );

        Debug.DrawRay(
            headOrigin,
            transform.forward * wallDetectDistance,
            Color.yellow
        );

        bool chestHit = Physics.Raycast(
            chestOrigin,
            transform.forward,
            out RaycastHit chestInfo,
            wallDetectDistance,
            climbLayer
        );

        bool headHit = Physics.Raycast(
            headOrigin,
            transform.forward,
            wallDetectDistance,
            climbLayer
        );

        if (!chestHit)
            return;

        wallNormal = chestInfo.normal;

        if (!headHit)
        {
            TryLedgeGrab(chestInfo);
            return;
        }

        StartClimbing();
    }

    private void StartClimbing()
    {
        state = MovementState.Climbing;

        currentVelocity.x = 0f;
        currentVelocity.z = 0f;
    }

    private void StopClimbing()
    {
        if (state == MovementState.Climbing)
            state = MovementState.Normal;
    }

    private void HandleClimbing()
    {
        if (!Input.GetKey(KeyCode.Space))
        {
            StopClimbing();
            return;
        }

        if (stamina == null || stamina.stamina <= 0f)
        {
            StopClimbing();
            return;
        }

        Vector3 chestOrigin =
            transform.position +
            Vector3.up * chestHeight;

        Debug.DrawRay(
            chestOrigin,
            transform.forward * wallDetectDistance,
            Color.green
        );

        bool wallStillExists = Physics.Raycast(
            chestOrigin,
            transform.forward,
            out RaycastHit wallInfo,
            wallDetectDistance,
            climbLayer
        );

        if (!wallStillExists)
        {
            StopClimbing();
            return;
        }

        stamina.ConsumeStamina(
            climbStaminaDrain * Time.deltaTime
        );

        currentVelocity.x = 0f;
        currentVelocity.z = 0f;
        currentVelocity.y = climbSpeed;

        Vector3 headOrigin =
            transform.position +
            Vector3.up * headHeight;

        Debug.DrawRay(
            headOrigin,
            transform.forward * wallDetectDistance,
            Color.magenta
        );

        bool headHit = Physics.Raycast(
            headOrigin,
            transform.forward,
            wallDetectDistance,
            climbLayer
        );

        if (!headHit)
        {
            TryLedgeGrab(wallInfo);
        }
    }

    private void TryLedgeGrab(RaycastHit wallHit)
    {
        Vector3 topCheckOrigin =
            wallHit.point +
            Vector3.up * ledgeCheckHeight +
            wallNormal * ledgeForwardOffset;

        Debug.DrawRay(
            topCheckOrigin,
            Vector3.down * ledgeCheckHeight,
            Color.blue
        );

        if (Physics.Raycast(
            topCheckOrigin,
            Vector3.down,
            out RaycastHit topHit,
            ledgeCheckHeight,
            climbLayer))
        {
            EnterLedgeGrab(topHit.point);
        }
    }

    private void EnterLedgeGrab(Vector3 ledgePoint)
    {
        if (state == MovementState.LedgeGrab ||
            state == MovementState.ClimbUp)
            return;

        state = MovementState.LedgeGrab;

        currentVelocity = Vector3.zero;

        Vector3 hangPosition =
            ledgePoint +
            wallNormal * hangOffsetBack +
            Vector3.down * hangOffsetDown;

        transform.position = hangPosition;

        ledgeTargetPosition =
            ledgePoint +
            Vector3.up * 1f +
            wallNormal * 0.75f;

        if (ledgeRoutine == null)
        {
            ledgeRoutine = StartCoroutine(ClimbUpLedge());
        }
    }

    private IEnumerator ClimbUpLedge()
    {
        state = MovementState.ClimbUp;

        Vector3 startPos = transform.position;

        float timer = 0f;

        while (timer < ledgeClimbDuration)
        {
            timer += Time.deltaTime;

            float t = timer / ledgeClimbDuration;

            transform.position = Vector3.Lerp(
                startPos,
                ledgeTargetPosition,
                t
            );

            yield return null;
        }

        transform.position = ledgeTargetPosition;

        state = MovementState.Normal;

        ledgeRoutine = null;
    }
    private void StopMovement()
    {
        _allowedToMove = false;
    }

    private void ContinueMovement()
    {
        _allowedToMove = true;

        acceleration = 20f;
    }

    private void StoppingMovenent()
    {
        currentVelocity = Vector3.zero;

        acceleration = 0f;
    }
}