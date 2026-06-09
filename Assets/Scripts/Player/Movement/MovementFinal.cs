using System.Collections;
using UnityEngine;

public class PlayerMovement : BasePhysics
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform playerVisual;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float sprintSpeed = 12f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField, Range(0f, 1f)] private float airControl = 0.4f;

    [Header("Sprint")]
    [SerializeField] private float sprintStaminaDrain = 1f;

    [Header("Crouch")]
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    private Vector3 originalVisualScale;
    private bool isCrouching;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float jumpStaminaCost = 1f;

    [Header("Wall Climb")]
    [SerializeField] private LayerMask climbLayer;
    [SerializeField] private float wallDetectDistance = 1f;
    [SerializeField] private float climbSpeed = 4f;
    [SerializeField] private float chestHeight = 1f;
    [SerializeField] private float topRayHeight = 2f;

    [Header("Climb Stamina")]
    [SerializeField] private float climbStaminaDrain = 1f;

    [Header("Ledge Grab")]
    [SerializeField] private float topSurfaceCheckDistance = 3f;
    [SerializeField] private float ledgeForwardOffset = 0.5f;
    [SerializeField] private float hangOffsetDown = 1.2f;
    [SerializeField] private float hangOffsetBack = 0.5f;
    [SerializeField] private float ledgeClimbDuration = 0.25f;

    [Header("Ledge Stamina")]
    [SerializeField] private float ledgeHangDrain = 0.75f;

    private bool allowedToMove = true;

    private enum MovementState
    {
        Normal,
        Climbing,
        LedgeGrab,
        ClimbUp
    }

    private MovementState state = MovementState.Normal;

    private Vector3 inputDirection;
    private Vector3 wallNormal;
    private Vector3 ledgeTargetPosition;

    private bool isSprinting;
    private PlayerStamina stamina;
    private Coroutine ledgeRoutine;

    public bool IsSprinting => isSprinting;
    public bool IsCrouching
    {
        get => isCrouching;
        set => isCrouching = value;
    }

    public float CurrentHorizontalSpeed =>
        new Vector3(currentVelocity.x, 0f, currentVelocity.z).magnitude;

    public float MoveSpeed => moveSpeed;
    public float SprintSpeed => sprintSpeed;
    public float CrouchSpeed => Mathf.Min(crouchSpeed, moveSpeed);

    protected override bool IsCrouchingForGrounding => IsCrouching;

    public float MovementIntensity =>
        Mathf.Clamp01(
            new Vector2(currentVelocity.x, currentVelocity.z).magnitude /
            Mathf.Max(0.0001f, sprintSpeed)
        );

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
            orientation = Camera.main.transform;

        if (playerVisual == null)
            playerVisual = transform;

        originalVisualScale = playerVisual.localScale;
    }

    protected override void Update()
    {
        HandleInput();
        HandleCrouch();

        base.Update();

        if (stamina == null)
            stamina = PlayerStamina.Instance;

        switch (state)
        {
            case MovementState.Normal:
                HandleMovement();
                HandleJump();
                HandleWallCheck();
                break;

            case MovementState.Climbing:
                HandleClimbing();
                break;

            case MovementState.LedgeGrab:
                HandleLedgeGrab();
                break;

            case MovementState.ClimbUp:
                currentVelocity = Vector3.zero;
                break;
        }

        Vector3 moveDelta = currentVelocity * Time.deltaTime;
        SafeMove(moveDelta);

        if (!allowedToMove)
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
            inputDirection = new Vector3(h, 0f, v).normalized;
            return;
        }

        Vector3 forward = orientation.forward;
        Vector3 right = orientation.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        inputDirection = (forward * -v + right * -h).normalized;
    }

    private void HandleCrouch()
    {
        IsCrouching = Input.GetKey(crouchKey);

        Vector3 targetScale = originalVisualScale;
        if (IsCrouching)
            targetScale.y = crouchHeight;

        playerVisual.localScale = Vector3.Lerp(
            playerVisual.localScale,
            targetScale,
            crouchTransitionSpeed * Time.deltaTime
        );
    }

    private void HandleMovement()
    {
        float control = IsGrounded ? 1f : airControl;

        float effectiveCrouchSpeed = Mathf.Min(crouchSpeed, moveSpeed);

        bool canSprint =
            isSprinting &&
            !IsCrouching &&
            IsGrounded &&
            inputDirection.sqrMagnitude > 0.001f &&
            stamina != null &&
            stamina.stamina > 0f;

        if (canSprint)
        {
            stamina.ConsumeStamina(sprintStaminaDrain * Time.deltaTime);
        }

        float currentSpeed =
            IsCrouching ? effectiveCrouchSpeed :
            canSprint ? sprintSpeed :
            moveSpeed;

        Vector3 targetVelocity = inputDirection * currentSpeed;

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
        if (IsCrouching)
            return;

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

    private void HandleWallCheck()
    {
        if (!Input.GetKey(KeyCode.Space))
            return;

        if (IsGrounded)
            return;

        if (IsCrouching)
            return;

        if (stamina == null || stamina.stamina <= 0f)
            return;

        Vector3 lowerRayOrigin = transform.position + Vector3.up * chestHeight;
        Vector3 upperRayOrigin = transform.position + Vector3.up * topRayHeight;

        Debug.DrawRay(lowerRayOrigin, transform.forward * wallDetectDistance, Color.red);
        Debug.DrawRay(upperRayOrigin, transform.forward * wallDetectDistance, Color.yellow);

        bool lowerHit = Physics.Raycast(
            lowerRayOrigin,
            transform.forward,
            out RaycastHit wallHit,
            wallDetectDistance,
            climbLayer
        );

        bool upperHit = Physics.Raycast(
            upperRayOrigin,
            transform.forward,
            wallDetectDistance,
            climbLayer
        );

        if (!lowerHit)
            return;

        wallNormal = wallHit.normal;

        if (!upperHit)
        {
            TryLedgeGrab(wallHit);
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
        {
            state = MovementState.Normal;
        }
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

        Vector3 lowerRayOrigin = transform.position + Vector3.up * chestHeight;
        Vector3 upperRayOrigin = transform.position + Vector3.up * topRayHeight;

        bool lowerHit = Physics.Raycast(
            lowerRayOrigin,
            transform.forward,
            out RaycastHit wallHit,
            wallDetectDistance,
            climbLayer
        );

        if (!lowerHit)
        {
            StopClimbing();
            return;
        }

        stamina.ConsumeStamina(climbStaminaDrain * Time.deltaTime);

        currentVelocity.x = 0f;
        currentVelocity.z = 0f;
        currentVelocity.y = climbSpeed;

        wallNormal = wallHit.normal;

        bool upperHit = Physics.Raycast(
            upperRayOrigin,
            transform.forward,
            wallDetectDistance,
            climbLayer
        );

        if (!upperHit)
        {
            TryLedgeGrab(wallHit);
        }
    }

    private void TryLedgeGrab(RaycastHit wallHit)
    {
        Vector3 topOrigin =
            wallHit.point +
            Vector3.up * topRayHeight +
            transform.forward * ledgeForwardOffset;

        Debug.DrawRay(
            topOrigin,
            Vector3.down * topSurfaceCheckDistance,
            Color.blue
        );

        bool foundTop = Physics.Raycast(
            topOrigin,
            Vector3.down,
            out RaycastHit topHit,
            topSurfaceCheckDistance,
            climbLayer
        );

        if (!foundTop)
            return;

        EnterLedgeGrab(topHit.point);
    }

    private void EnterLedgeGrab(Vector3 ledgePoint)
    {
        if (state == MovementState.LedgeGrab || state == MovementState.ClimbUp)
            return;

        state = MovementState.LedgeGrab;
        currentVelocity = Vector3.zero;

        Vector3 hangPosition =
            ledgePoint -
            transform.forward * hangOffsetBack -
            Vector3.up * hangOffsetDown;

        transform.position = hangPosition;

        ledgeTargetPosition = ledgePoint + Vector3.up * 1f;
    }

    private void HandleLedgeGrab()
    {
        currentVelocity = Vector3.zero;

        if (stamina != null)
        {
            stamina.ConsumeStamina(ledgeHangDrain * Time.deltaTime);

            if (stamina.stamina <= 0f)
            {
                ReleaseLedgeGrab();
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && ledgeRoutine == null)
        {
            ledgeRoutine = StartCoroutine(ClimbUpLedge());
            return;
        }

        if (Input.GetKeyDown(crouchKey))
        {
            ReleaseLedgeGrab();
        }
    }

    private void ReleaseLedgeGrab()
    {
        state = MovementState.Normal;
        currentVelocity = Vector3.zero;
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

            transform.position = Vector3.Lerp(startPos, ledgeTargetPosition, t);
            yield return null;
        }

        transform.position = ledgeTargetPosition;

        state = MovementState.Normal;
        ledgeRoutine = null;
    }

    private void StopMovement()
    {
        allowedToMove = false;
    }

    private void ContinueMovement()
    {
        allowedToMove = true;
        acceleration = 20f;
    }

    private void StoppingMovenent()
    {
        currentVelocity = Vector3.zero;
        acceleration = 0f;
    }
}