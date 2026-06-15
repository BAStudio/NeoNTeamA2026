using System.Collections;
using UnityEngine;

public class PlayerMovement : BasePhysics
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform playerVisual;

    [Header("Input")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode traversalKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Movement Toggles")]
    [SerializeField] private bool enableSprint = true;
    [SerializeField] private bool enableJump = true;
    [SerializeField] private bool enableCrouch = true;
    [SerializeField] private bool enableWallClimb = true;
    [SerializeField] private bool enableMantle = true;
    [SerializeField] private bool enableLedgeGrab = true;   

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float sprintSpeed = 12f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField, Range(0f, 1f)] private float airControl = 0.4f;

    [Header("Sprint")]
    [SerializeField] private float sprintStaminaDrain = 1f;

    [Header("Crouch")]
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [Header("Forced Crouch")]
    [SerializeField] private bool enableForcedCrouch = true;
    [SerializeField] private LayerMask crouchBlockLayer;
    [SerializeField] private float standingHeightCheck = 2f;
    [SerializeField] private float crouchCheckRadius = 0.3f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float jumpStaminaCost = 1f;

    [Header("Wall Climb")]
    [SerializeField] private LayerMask climbLayer;
    [SerializeField] private float wallDetectDistance = 1f;
    [SerializeField] private float chestHeight = 1f;
    [SerializeField] private float topRayHeight = 2f;
    [SerializeField] private float climbSpeed = 4f;
    [SerializeField] private float climbStaminaDrain = 1f;

    [Header("Mantle")]
    [SerializeField] private float mantleMinHeight = 0.6f;
    [SerializeField] private float mantleMaxHeight = 1.35f;
    [SerializeField] private float mantleForwardCheck = 0.7f;       // FIX: now actually used
    [SerializeField] private float mantleTopSearchDistance = 1.8f;  // FIX: now actually used
    [SerializeField] private float mantleForwardOffset = 0.5f;
    [SerializeField] private float mantleClimbDuration = 0.2f;

    [Header("Ledge Grab")]
    [SerializeField] private float ledgeMinHeight = 1.25f;
    [SerializeField] private float ledgeMaxHeight = 2.4f;
    [SerializeField] private float topSurfaceCheckDistance = 3f;
    [SerializeField] private float ledgeForwardOffset = 0.5f;
    [SerializeField] private float hangOffsetDown = 1.2f;
    [SerializeField] private float hangOffsetBack = 0.5f;
    [SerializeField] private float ledgeClimbDuration = 0.25f;
    [SerializeField] private float ledgeStandUpOffset = 1.05f;
    [SerializeField] private float ledgeClimbForwardOffset = 0.15f;
    [SerializeField] private float ledgeHangDrain = 0.75f;

    [Header("Surface Rules")]
    [SerializeField, Range(0f, 1f)] private float surfaceUpDotThreshold = 0.7f;

    [Header("Debug Rays")]
    [SerializeField] private bool drawDebugRaysTraversal = true;
    [SerializeField] private float debugRayDurationTraversal = 0.08f;
    [SerializeField] private Color lowerWallRayColor = Color.cyan;
    [SerializeField] private Color upperWallRayColor = Color.yellow;
    [SerializeField] private Color ledgeSearchRayColor = Color.blue;
    [SerializeField] private Color ledgeHangTargetColor = Color.green;
    [SerializeField] private Color ledgeClimbTargetColor = new Color(1f, 0.4f, 0f);
    [SerializeField] private Color ledgeNormalColor = Color.magenta;
    [SerializeField] private Color crouchCheckColor = Color.red;
    [SerializeField] private Color mantleCheckColor = Color.white;
    [SerializeField] private Color mantleTargetColor = Color.cyan;

    private enum MovementState
    {
        Normal,
        Climbing,
        LedgeGrab,
        ClimbUp,
        Mantling
    }

    private MovementState state = MovementState.Normal;

    private Vector3 originalVisualScale;
    private Vector3 inputDirection;
    private Vector3 wallNormal;
    private Vector3 ledgeTargetPosition;

    private bool isCrouching;
    private bool isSprinting;
    private bool forcedCrouch;
    private bool allowedToMove = true;

    private PlayerStamina stamina;
    private Coroutine ledgeRoutine;
    private Coroutine mantleRoutine;

    public bool IsSprinting => isSprinting;

    public bool IsCrouching
    {
        get => isCrouching;
        set => isCrouching = value;
    }

    public bool IsForcedCrouching => forcedCrouch;

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

    private void OnDisable()
    {
        NotePad.StopMovement -= StopMovement;
        NotePad.ContinueMovement -= ContinueMovement;
    }

    private void OnDestroy()
    {
        NotePad.StopMovement -= StopMovement;
        NotePad.ContinueMovement -= ContinueMovement;
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

        if (!allowedToMove)
        {
            StopMotionImmediately();
            return;
        }

        switch (state)
        {
            case MovementState.Normal:
                HandleMovement();
                HandleJump();
                HandleTraversal();
                break;

            case MovementState.Climbing:
                HandleClimbing();
                break;

            case MovementState.LedgeGrab:
                HandleLedgeGrab();
                break;

            case MovementState.ClimbUp:
            case MovementState.Mantling:
                currentVelocity = Vector3.zero;
                break;
        }

        SafeMove(currentVelocity * Time.deltaTime);
    }

    private void DrawRay(Vector3 origin, Vector3 direction, Color color)
    {
        if (!drawDebugRaysTraversal)
            return;

        Debug.DrawRay(origin, direction, color, debugRayDurationTraversal);
    }

    private void DrawLine(Vector3 start, Vector3 end, Color color)
    {
        if (!drawDebugRaysTraversal)
            return;

        Debug.DrawLine(start, end, color, debugRayDurationTraversal);
    }

    private Vector3 GetFlatForward()
    {
        Vector3 forward = orientation != null ? orientation.forward : transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            forward = transform.forward;

        forward.Normalize();
        return forward;
    }

    private bool IsWithinHeightRange(float height, float min, float max)
    {
        return height >= Mathf.Min(min, max) && height <= Mathf.Max(min, max);
    }

    private void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        isSprinting = Input.GetKey(sprintKey);

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
        // FIX: respect enableCrouch toggle; uncrouched scale is restored when disabled
        if (!enableCrouch)
        {
            forcedCrouch = false;
            IsCrouching = false;
            playerVisual.localScale = Vector3.Lerp(
                playerVisual.localScale,
                originalVisualScale,
                crouchTransitionSpeed * Time.deltaTime
            );
            return;
        }

        bool manualCrouch = Input.GetKey(crouchKey);
        forcedCrouch = false;

        if (enableForcedCrouch)
        {
            Vector3 checkOrigin = transform.position + Vector3.up * crouchHeight;
            float checkDistance = Mathf.Max(0f, standingHeightCheck - crouchHeight);

            bool blocked = Physics.SphereCast(
                checkOrigin,
                crouchCheckRadius,
                Vector3.up,
                out RaycastHit hit,
                checkDistance,
                crouchBlockLayer,
                QueryTriggerInteraction.Ignore
            );

            if (drawDebugRaysTraversal)
            {
                Color rayColor = blocked ? Color.red : Color.green;
                DrawRay(checkOrigin, Vector3.up * checkDistance, rayColor);

                Debug.DrawLine(
                    checkOrigin + Vector3.right * crouchCheckRadius,
                    checkOrigin + Vector3.right * crouchCheckRadius + Vector3.up * checkDistance,
                    crouchCheckColor,
                    debugRayDurationTraversal
                );

                Debug.DrawLine(
                    checkOrigin - Vector3.right * crouchCheckRadius,
                    checkOrigin - Vector3.right * crouchCheckRadius + Vector3.up * checkDistance,
                    crouchCheckColor,
                    debugRayDurationTraversal
                );

                if (blocked)
                    DrawLine(hit.point, hit.point + hit.normal * 0.5f, crouchCheckColor);
            }

            forcedCrouch = blocked;
        }

        IsCrouching = manualCrouch || forcedCrouch;

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

        // FIX: respect enableSprint toggle
        bool canSprint =
            enableSprint &&
            isSprinting &&
            !IsCrouching &&
            IsGrounded &&
            inputDirection.sqrMagnitude > 0.001f &&
            stamina != null &&
            stamina.stamina > 0f;

        if (canSprint)
            stamina.ConsumeStamina(sprintStaminaDrain * Time.deltaTime);

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
        // FIX: respect enableJump toggle
        if (!allowedToMove || !enableJump || IsCrouching || stamina == null)
            return;

        if (IsGrounded &&
            Input.GetKeyDown(jumpKey) &&
            stamina.stamina >= jumpStaminaCost)
        {
            stamina.ConsumeStamina(jumpStaminaCost);
            currentVelocity.y = 0f;
            ApplyImpulse(Vector3.up * jumpForce);
        }
    }

    private void HandleTraversal()
    {
        if (!Input.GetKey(traversalKey))
            return;

        if (IsGrounded || IsCrouching || stamina == null || stamina.stamina <= 0f)
            return;

        Vector3 forward = GetFlatForward();

        Vector3 lowerRayOrigin = transform.position + Vector3.up * chestHeight;
        Vector3 upperRayOrigin = transform.position + Vector3.up * topRayHeight;

        DrawRay(lowerRayOrigin, forward * wallDetectDistance, lowerWallRayColor);
        DrawRay(upperRayOrigin, forward * wallDetectDistance, upperWallRayColor);

        bool lowerHit = Physics.Raycast(
            lowerRayOrigin,
            forward,
            out RaycastHit wallHit,
            wallDetectDistance,
            climbLayer,
            QueryTriggerInteraction.Ignore
        );

        if (!lowerHit)
            return;

        wallNormal = wallHit.normal;
        DrawLine(wallHit.point, wallHit.point + wallHit.normal, ledgeNormalColor);

        bool upperHit = Physics.Raycast(
            upperRayOrigin,
            forward,
            wallDetectDistance,
            climbLayer,
            QueryTriggerInteraction.Ignore
        );

        if (enableWallClimb && upperHit)
        {
            StartClimbing();
            return;
        }

        if (upperHit)
            return;

        // FIX: mantle and ledge grab now each use their own forward-offset and
        // search-distance parameters instead of both sharing ledgeForwardOffset /
        // topSurfaceCheckDistance. mantleForwardCheck and mantleTopSearchDistance
        // were declared but previously ignored entirely.

        if (enableMantle &&
            TryGetTopSurface(upperRayOrigin, forward, mantleForwardCheck,
                             mantleTopSearchDistance, out RaycastHit mantleHit))
        {
            float surfaceHeight = mantleHit.point.y - transform.position.y;
            if (IsWithinHeightRange(surfaceHeight, mantleMinHeight, mantleMaxHeight))
            {
                TryStartMantle(mantleHit.point, wallHit.normal, forward);
                return;
            }
        }

        // FIX: guard enableLedgeGrab (was missing) and do a fresh surface search
        // with the ledge-specific parameters
        if (enableLedgeGrab &&
            TryGetTopSurface(upperRayOrigin, forward, ledgeForwardOffset,
                             topSurfaceCheckDistance, out RaycastHit ledgeHit))
        {
            float surfaceHeight = ledgeHit.point.y - transform.position.y;
            if (IsWithinHeightRange(surfaceHeight, ledgeMinHeight, ledgeMaxHeight))
            {
                // FIX: stamina guard before grabbing so a 0-stamina player does
                // not teleport to the hang position then immediately drop
                if (stamina.stamina > 0f)
                    TryStartLedgeGrab(ledgeHit.point, wallHit.normal, ledgeHit.normal);
            }
        }
    }

    // FIX: now accepts explicit forwardOffset and searchDistance so mantle and
    // ledge grab each receive their own tuned values instead of sharing one set
    private bool TryGetTopSurface(
        Vector3 upperRayOrigin,
        Vector3 forward,
        float forwardOffset,
        float searchDistance,
        out RaycastHit topHit)
    {
        Vector3 searchOrigin = upperRayOrigin + forward * forwardOffset;

        DrawRay(searchOrigin, Vector3.down * searchDistance, ledgeSearchRayColor);

        return Physics.Raycast(
                   searchOrigin,
                   Vector3.down,
                   out topHit,
                   searchDistance,
                   climbLayer,
                   QueryTriggerInteraction.Ignore
               ) && Vector3.Dot(topHit.normal, Vector3.up) >= surfaceUpDotThreshold;
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
        if (!enableWallClimb)
        {
            StopClimbing();
            return;
        }

        if (!Input.GetKey(traversalKey))
        {
            StopClimbing();
            return;
        }

        if (stamina == null || stamina.stamina <= 0f)
        {
            StopClimbing();
            return;
        }

        Vector3 forward = GetFlatForward();

        Vector3 lowerRayOrigin = transform.position + Vector3.up * chestHeight;
        Vector3 upperRayOrigin = transform.position + Vector3.up * topRayHeight;

        DrawRay(lowerRayOrigin, forward * wallDetectDistance, lowerWallRayColor);
        DrawRay(upperRayOrigin, forward * wallDetectDistance, upperWallRayColor);

        bool lowerHit = Physics.Raycast(
            lowerRayOrigin,
            forward,
            out RaycastHit wallHit,
            wallDetectDistance,
            climbLayer,
            QueryTriggerInteraction.Ignore
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
        DrawLine(wallHit.point, wallHit.point + wallHit.normal, ledgeNormalColor);

        bool upperHit = Physics.Raycast(
            upperRayOrigin,
            forward,
            wallDetectDistance,
            climbLayer,
            QueryTriggerInteraction.Ignore
        );

        // Debug-only: visualise the ledge search ray while climbing
        if (!upperHit)
            TryGetTopSurface(upperRayOrigin, forward, ledgeForwardOffset,
                             topSurfaceCheckDistance, out _);

        if (inputDirection.sqrMagnitude > 0.1f)
        {
            Vector3 moveDir = inputDirection.normalized;

            if (Vector3.Dot(moveDir, wallNormal) > 0.3f)
            {
                StopClimbing();
                return;
            }
        }
    }

    private void TryStartMantle(Vector3 topPoint, Vector3 hitNormal, Vector3 forward)
    {
        if (!enableMantle || mantleRoutine != null)
            return;

        mantleRoutine = StartCoroutine(MantleRoutine(topPoint, hitNormal, forward));
    }

    private IEnumerator MantleRoutine(Vector3 topPoint, Vector3 hitNormal, Vector3 forward)
    {
        state = MovementState.Mantling;
        currentVelocity = Vector3.zero;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 faceDir = new Vector3(-hitNormal.x, 0f, -hitNormal.z);
        Quaternion targetRot = faceDir.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(faceDir.normalized, Vector3.up)
            : transform.rotation;

        Vector3 targetPos =
            topPoint +
            Vector3.up * 0.05f +
            forward * mantleForwardOffset;

        DrawLine(startPos, targetPos, mantleTargetColor);

        float timer = 0f;

        while (timer < mantleClimbDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / mantleClimbDuration);

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        state = MovementState.Normal;
        mantleRoutine = null;
    }

    private void TryStartLedgeGrab(Vector3 ledgePoint, Vector3 wallNormal, Vector3 topNormal)
    {
        if (ledgeRoutine != null || state != MovementState.Normal)
            return;

        if (Vector3.Dot(topNormal, Vector3.up) < surfaceUpDotThreshold)
            return;

        state = MovementState.LedgeGrab;
        currentVelocity = Vector3.zero;

        this.wallNormal = wallNormal;

        Vector3 faceDir = new Vector3(-wallNormal.x, 0f, -wallNormal.z);
        if (faceDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(faceDir.normalized, Vector3.up);

        Vector3 hangPosition =
            ledgePoint +
            wallNormal * hangOffsetBack +
            Vector3.down * hangOffsetDown;

        transform.position = hangPosition;

        ledgeTargetPosition =
            ledgePoint +
            wallNormal * ledgeClimbForwardOffset +
            Vector3.up * ledgeStandUpOffset;

        DrawLine(transform.position, ledgeTargetPosition, ledgeHangTargetColor);
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

        if (Input.GetKeyDown(traversalKey) && ledgeRoutine == null)
        {
            ledgeRoutine = StartCoroutine(ClimbUpLedge());
            return;
        }

        if (Input.GetKeyDown(crouchKey))
            ReleaseLedgeGrab();
    }

    private void ReleaseLedgeGrab()
    {
        state = MovementState.Normal;
        currentVelocity = Vector3.zero;
        ledgeRoutine = null;
    }

    private IEnumerator ClimbUpLedge()
    {
        state = MovementState.ClimbUp;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 faceDir = new Vector3(-wallNormal.x, 0f, -wallNormal.z);
        Quaternion targetRot = faceDir.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(faceDir.normalized, Vector3.up)
            : transform.rotation;

        DrawLine(startPos, ledgeTargetPosition, ledgeClimbTargetColor);

        float timer = 0f;

        while (timer < ledgeClimbDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / ledgeClimbDuration);

            transform.position = Vector3.Lerp(startPos, ledgeTargetPosition, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        transform.position = ledgeTargetPosition;
        transform.rotation = targetRot;

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

    private void StopMotionImmediately()
    {
        currentVelocity = Vector3.zero;
        acceleration = 0f;

        if (ledgeRoutine != null)
        {
            StopCoroutine(ledgeRoutine);
            ledgeRoutine = null;
        }

        if (mantleRoutine != null)
        {
            StopCoroutine(mantleRoutine);
            mantleRoutine = null;
        }

        if (state == MovementState.Climbing ||
            state == MovementState.LedgeGrab ||
            state == MovementState.ClimbUp ||
            state == MovementState.Mantling)
        {
            state = MovementState.Normal;
        }
    }
}