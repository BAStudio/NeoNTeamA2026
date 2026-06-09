using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WallClimbing : MonoBehaviour
{
    public event Action<bool> OnWallClimb;

    private enum ClimbState
    {
        None,
        Climbing,
        LedgeGrab
    }

    [Header("Wall Climb")]
    [SerializeField] private float climbSpeed = 4f;
    [SerializeField] private float detectDistance = 1f;
    [SerializeField] private LayerMask climbLayer;
    [SerializeField] private float chestRayHeight = 0.8f;
    [SerializeField] private float headRayHeight = 1.7f;

    [Header("Ledge Grab")]
    [SerializeField] private float ledgeProbeHeight = 1.4f;
    [SerializeField] private float ledgeProbeForwardOffset = 0.35f;
    [SerializeField] private float hangBackOffset = 0.35f;
    [SerializeField] private float hangDownOffset = 1.05f;
    [SerializeField] private float standBackOffset = 0.55f;
    [SerializeField] private float standUpOffset = 1.2f;
    [SerializeField] private float ledgeClimbDuration = 0.25f;
    [SerializeField] private float ledgeGrabTimeout = 10f;

    private ClimbState state = ClimbState.None;
    private Rigidbody rb;
    private PlayerStamina playerStamina;

    private Vector3 wallNormal = Vector3.forward;
    private Vector3 ledgeHangPoint;
    private Vector3 ledgeStandPoint;
    private float ledgeGrabTimer;
    private Coroutine climbUpRoutine;

    private void Start()
    {
        playerStamina = PlayerStamina.Instance;
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError($"{name}: WallClimbing requires a Rigidbody.");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (playerStamina == null)
            playerStamina = PlayerStamina.Instance;

        if (playerStamina == null)
            return;

        if (climbUpRoutine != null)
            return;

        bool spaceHeld = Input.GetKey(KeyCode.Space);
        bool spaceDown = Input.GetKeyDown(KeyCode.Space);

        if (state == ClimbState.LedgeGrab)
        {
            if (spaceDown && climbUpRoutine == null)
            {
                climbUpRoutine = StartCoroutine(ClimbUpLedge());
                return;
            }

            if (Input.anyKey)
            {
                ledgeGrabTimer = 0f;
            }
            else
            {
                ledgeGrabTimer += Time.deltaTime;

                if (ledgeGrabTimer >= ledgeGrabTimeout)
                {
                    StopClimbing();
                }
            }

            return;
        }

        DetectWallAndClimb(spaceHeld);
    }

    private void FixedUpdate()
    {
        if (state != ClimbState.Climbing) return;
        if (playerStamina == null || playerStamina.stamina <= 0f)
        {
            StopClimbing();
            return;
        }

        float y = Input.GetKey(KeyCode.Space) ? climbSpeed : -climbSpeed;

        playerStamina.ConsumeStamina(0.5f * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(0f, y, 0f);
    }

    private void DetectWallAndClimb(bool spaceHeld)
    {
        if (!spaceHeld || playerStamina.stamina <= 0f)
        {
            StopClimbing();
            return;
        }

        Vector3 chestOrigin = transform.position + Vector3.up * chestRayHeight;
        Vector3 headOrigin = transform.position + Vector3.up * headRayHeight;

        bool chestHit = Physics.Raycast(
            chestOrigin,
            transform.forward,
            out RaycastHit chestRay,
            detectDistance,
            climbLayer,
            QueryTriggerInteraction.Ignore);

        bool headHit = Physics.Raycast(
            headOrigin,
            transform.forward,
            detectDistance,
            climbLayer,
            QueryTriggerInteraction.Ignore);

        if (!chestHit)
        {
            StopClimbing();
            return;
        }

        wallNormal = chestRay.normal;

        if (!headHit && TryStartLedgeGrab(chestRay))
        {
            return;
        }

        StartClimbing();
    }

    private bool TryStartLedgeGrab(RaycastHit wallHit)
    {
        Vector3 topProbeOrigin =
            wallHit.point +
            Vector3.up * ledgeProbeHeight +
            wallNormal * ledgeProbeForwardOffset;

        if (Physics.Raycast(
            topProbeOrigin,
            Vector3.down,
            out RaycastHit topHit,
            ledgeProbeHeight + 0.75f,
            climbLayer,
            QueryTriggerInteraction.Ignore))
        {
            EnterLedgeGrab(topHit.point);
            return true;
        }

        return false;
    }

    private void EnterLedgeGrab(Vector3 topPoint)
    {
        state = ClimbState.LedgeGrab;
        ledgeGrabTimer = 0f;

        rb.linearVelocity = Vector3.zero;
        rb.useGravity = false;

        ledgeHangPoint = topPoint + wallNormal * hangBackOffset + Vector3.down * hangDownOffset;
        ledgeStandPoint = topPoint + wallNormal * standBackOffset + Vector3.up * standUpOffset;

        rb.position = ledgeHangPoint;

        OnWallClimb?.Invoke(true);
    }

    private void StartClimbing()
    {
        if (state == ClimbState.Climbing) return;

        state = ClimbState.Climbing;
        rb.useGravity = false;

        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;

        OnWallClimb?.Invoke(true);
    }

    private void StopClimbing()
    {
        if (state == ClimbState.None) return;

        state = ClimbState.None;
        rb.useGravity = true;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, rb.linearVelocity.z);

        OnWallClimb?.Invoke(false);
    }

    private IEnumerator ClimbUpLedge()
    {
        state = ClimbState.None;

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;

        Vector3 startPos = rb.position;
        float t = 0f;

        while (t < ledgeClimbDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / ledgeClimbDuration);

            rb.MovePosition(Vector3.Lerp(startPos, ledgeStandPoint, a));
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(ledgeStandPoint);

        rb.useGravity = true;
        climbUpRoutine = null;
        ledgeGrabTimer = 0f;

        OnWallClimb?.Invoke(false);
    }
}