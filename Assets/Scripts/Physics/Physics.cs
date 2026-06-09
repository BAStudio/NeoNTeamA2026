using System.Collections;
using UnityEngine;

/// <summary>
/// BasePhysics
/// ----------------
/// Transform-based physics helper.
/// Uses SphereCast grounding instead of Raycast.
/// </summary>
[DisallowMultipleComponent]
public class BasePhysics : MonoBehaviour
{
    #region Collision Shape

    [Header("Collision Shape (used for casts & overlap checks)")]

    [Tooltip("Height of the capsule used for casts and overlap checks.")]
    [SerializeField] protected float capsuleHeight = 2.0f;

    [Tooltip("Radius of the capsule used for casts and overlap checks.")]
    [SerializeField] protected float capsuleRadius = 0.45f;

    [Tooltip("Local-space offset for the capsule center.")]
    [SerializeField] protected Vector3 capsuleCenter = new Vector3(0f, 1f, 0f);

    [Tooltip("Layers considered obstacles.")]
    [SerializeField] protected LayerMask obstacleLayers = ~0;

    #endregion

    #region Gravity & Grounding

    [Header("Gravity & Grounding")]

    [Tooltip("Gravity multiplier.")]
    [SerializeField] protected float gravityScale = 1f;

    [Tooltip("Maximum falling speed.")]
    [SerializeField] protected float terminalVelocity = 20f;

    [Tooltip("Base grounding distance.")]
    [SerializeField] protected float groundCheckDistance = 0.15f;

    [Tooltip("Extra grounding length.")]
    [SerializeField] protected float groundCheckLength = 0.2f;

    [Tooltip("Width of the grounding sphere.")]
    [SerializeField] protected float groundCheckRadiusMultiplier = 0.9f;

    [Tooltip("Ground layers.")]
    [SerializeField] protected LayerMask groundLayers = ~0;

    [Tooltip("Crouch grounding multiplier.")]
    [SerializeField, Range(0.1f, 1f)]
    protected float crouchGroundCheckMultiplier = 0.5f;

    [Tooltip("Small upward correction.")]
    [SerializeField] protected float standingGroundSnapUp = 0.03f;

    #endregion

    #region General Physics

    [Header("General Physics")]

    [Tooltip("Mass.")]
    [SerializeField] protected float mass = 1f;

    [Tooltip("Horizontal damping.")]
    [SerializeField] protected float linearDrag = 6f;

    [Tooltip("Skin width.")]
    [SerializeField] protected float skinWidth = 0.05f;

    #endregion

    public Vector3 CurrentVelocity => currentVelocity;

    protected Vector3 currentVelocity = Vector3.zero;

    public bool IsGrounded { get; protected set; } = false;

    protected Coroutine knockbackRoutine;

    protected virtual bool IsCrouchingForGrounding => false;

    protected virtual float GetGroundCheckDistance()
    {
        return groundCheckDistance *
               (IsCrouchingForGrounding
                    ? crouchGroundCheckMultiplier
                    : 1f);
    }

    protected virtual void Awake()
    {
        if (mass <= 0f)
            mass = 1f;

        if (capsuleRadius < 0.01f)
            capsuleRadius = 0.45f;

        if (capsuleHeight < capsuleRadius * 2f)
        {
            capsuleHeight = capsuleRadius * 2.01f;

            Debug.LogWarning(
                $"{name}: capsuleHeight was too small; adjusted to {capsuleHeight:F2}"
            );
        }

        if (skinWidth <= 0f)
            skinWidth = 0.05f;
    }

    protected virtual void Update()
    {
        Vector3 worldCenter = transform.TransformPoint(capsuleCenter);

        RaycastHit hit;

        float checkDistance =
            GetGroundCheckDistance() + groundCheckLength;

        float sphereRadius =
            capsuleRadius * groundCheckRadiusMultiplier;

        Vector3 sphereOrigin =
            worldCenter + Vector3.up * 0.05f;

        IsGrounded = Physics.SphereCast(
            sphereOrigin,
            sphereRadius,
            Vector3.down,
            out hit,
            checkDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );
        if (IsGrounded)
        {
            if (currentVelocity.y < 0f)
                currentVelocity.y = 0f;

            if (!IsCrouchingForGrounding && hit.collider != null)
            {
                float correction =
                    groundCheckDistance - hit.distance;

                if (correction > 0f)
                {
                    transform.position +=
                        Vector3.up *
                        Mathf.Min(correction, standingGroundSnapUp);
                }
            }
        }
        else
        {
            float gravityValue = -9.81f * gravityScale;

            currentVelocity.y +=
                gravityValue * Time.deltaTime;

            if (currentVelocity.y < -Mathf.Abs(terminalVelocity))
            {
                currentVelocity.y =
                    -Mathf.Abs(terminalVelocity);
            }
        }

        float dragFactor =
            Mathf.Clamp01(1f - linearDrag * Time.deltaTime);

        currentVelocity.x *= dragFactor;
        currentVelocity.z *= dragFactor;

        DrawGroundDebug(
            sphereOrigin,
            sphereRadius,
            checkDistance
        );
    }

    #region Movement / Collision

    public virtual bool SafeMove(Vector3 delta)
    {
        if (delta.sqrMagnitude <= Mathf.Epsilon)
            return true;

        Vector3 worldCenter =
            transform.TransformPoint(capsuleCenter);

        Vector3 bottom =
            worldCenter +
            Vector3.down *
            (capsuleHeight * 0.5f - capsuleRadius);

        Vector3 top =
            worldCenter +
            Vector3.up *
            (capsuleHeight * 0.5f - capsuleRadius);

        float distance = delta.magnitude;

        RaycastHit hit;

        if (Physics.CapsuleCast(
            bottom,
            top,
            capsuleRadius,
            delta.normalized,
            out hit,
            distance + skinWidth,
            obstacleLayers,
            QueryTriggerInteraction.Ignore))
        {
            float moveDist =
                Mathf.Max(0f, hit.distance - skinWidth);

            transform.position +=
                delta.normalized * moveDist;

            OnSafeMoveBlocked(hit);

            return false;
        }

        transform.position += delta;
        return true;
    }

    public virtual void ResolveOverlap(float pushStrength = 0.5f)
    {
        Vector3 worldCenter =
            transform.TransformPoint(capsuleCenter);

        Vector3 bottom =
            worldCenter +
            Vector3.down *
            (capsuleHeight * 0.5f - capsuleRadius);

        Vector3 top =
            worldCenter +
            Vector3.up *
            (capsuleHeight * 0.5f - capsuleRadius);

        Collider[] hits = Physics.OverlapCapsule(
            bottom,
            top,
            capsuleRadius,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider other = hits[i];

            if (other == null)
                continue;

            Vector3 closest =
                other.ClosestPoint(worldCenter);

            Vector3 pushDir =
                (worldCenter - closest);

            if (pushDir.sqrMagnitude <= 0.0001f)
            {
                pushDir =
                    (worldCenter - other.transform.position)
                    .normalized;
            }
            else
            {
                pushDir.Normalize();
            }

            transform.position +=
                pushDir * pushStrength;
        }
    }

    #endregion

    #region Forces

    public virtual void ApplyForce(Vector3 force)
    {
        if (mass <= 0f)
            mass = 1f;

        currentVelocity +=
            (force / mass) * Time.deltaTime;
    }

    public virtual void ApplyImpulse(Vector3 impulse)
    {
        if (mass <= 0f)
            mass = 1f;

        currentVelocity += impulse / mass;
    }

    public virtual void StartKnockback(
        Vector3 direction,
        float strength,
        float duration,
        AnimationCurve falloff = null)
    {
        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine = StartCoroutine(
            KnockbackCoroutine(
                direction.normalized,
                Mathf.Abs(strength),
                Mathf.Max(0.01f, duration),
                falloff
            )
        );
    }

    protected virtual IEnumerator KnockbackCoroutine(
        Vector3 dir,
        float strength,
        float duration,
        AnimationCurve falloff)
    {
        float t = 0f;

        if (falloff == null)
        {
            falloff = AnimationCurve.EaseInOut(
                0f,
                1f,
                1f,
                0f
            );
        }

        while (t < duration)
        {
            float normalized = t / duration;
            float scale = falloff.Evaluate(normalized);

            currentVelocity =
                dir * (strength * scale);

            SafeMove(currentVelocity * Time.deltaTime);

            t += Time.deltaTime;
            yield return null;
        }

        currentVelocity = Vector3.zero;
        knockbackRoutine = null;

        OnKnockbackEnded();
    }

    #endregion

    #region Push Helpers

    public virtual void PushAwayFromCollider(
        Collider other,
        float strength)
    {
        if (other == null)
            return;

        Vector3 worldCenter =
            transform.TransformPoint(capsuleCenter);

        Vector3 closest =
            other.ClosestPoint(worldCenter);

        Vector3 dir =
            (worldCenter - closest);

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir =
                (worldCenter - other.transform.position)
                .normalized;
        }
        else
        {
            dir.Normalize();
        }

        ApplyImpulse(dir * Mathf.Abs(strength));
    }

    public virtual void PushAwayFromPoint(
        Vector3 point,
        float strength)
    {
        Vector3 dir =
            (transform.position - point);

        if (dir.sqrMagnitude <= 0.0001f)
            dir = Vector3.up;

        dir.Normalize();

        ApplyImpulse(dir * Mathf.Abs(strength));
    }

    #endregion

    #region Utility

    public virtual bool IsOverlapping()
    {
        Vector3 worldCenter =
            transform.TransformPoint(capsuleCenter);

        Vector3 bottom =
            worldCenter +
            Vector3.down *
            (capsuleHeight * 0.5f - capsuleRadius);

        Vector3 top =
            worldCenter +
            Vector3.up *
            (capsuleHeight * 0.5f - capsuleRadius);

        Collider[] hits = Physics.OverlapCapsule(
            bottom,
            top,
            capsuleRadius,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );

        return hits != null && hits.Length > 0;
    }

    protected virtual void OnSafeMoveBlocked(RaycastHit hit) { }

    protected virtual void OnKnockbackEnded() { }

    public virtual void StopMoving()
    {
        currentVelocity = Vector3.zero;

        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
            knockbackRoutine = null;
        }
    }

    #endregion

    #region Debug

    private void DrawGroundDebug(
        Vector3 origin,
        float radius,
        float distance)
    {
        Color debugColor =
            IsGrounded ? Color.green : Color.red;

        Debug.DrawRay(
            origin,
            Vector3.down * distance,
            debugColor
        );

        Debug.DrawLine(
            origin + Vector3.right * radius,
            origin + Vector3.right * radius +
            Vector3.down * distance,
            debugColor
        );

        Debug.DrawLine(
            origin - Vector3.right * radius,
            origin - Vector3.right * radius +
            Vector3.down * distance,
            debugColor
        );
    }
    private void OnDrawGizmosSelected()
    {
        Vector3 worldCenter = transform.TransformPoint(capsuleCenter);

        float checkDistance =
            GetGroundCheckDistance() + groundCheckLength;

        float sphereRadius =
            capsuleRadius * groundCheckRadiusMultiplier;

        Vector3 sphereOrigin =
            worldCenter + Vector3.up * 0.05f;

        Vector3 sphereEnd =
            sphereOrigin + Vector3.down * checkDistance;

        Gizmos.color = IsGrounded ? Color.green : Color.red;

        // Start sphere
        Gizmos.DrawWireSphere(
            sphereOrigin,
            sphereRadius
        );

        // End sphere
        Gizmos.DrawWireSphere(
            sphereEnd,
            sphereRadius
        );

        // Connecting lines
        Gizmos.DrawLine(
            sphereOrigin + Vector3.right * sphereRadius,
            sphereEnd + Vector3.right * sphereRadius
        );

        Gizmos.DrawLine(
            sphereOrigin - Vector3.right * sphereRadius,
            sphereEnd - Vector3.right * sphereRadius
        );

        Gizmos.DrawLine(
            sphereOrigin + Vector3.forward * sphereRadius,
            sphereEnd + Vector3.forward * sphereRadius
        );

        Gizmos.DrawLine(
            sphereOrigin - Vector3.forward * sphereRadius,
            sphereEnd - Vector3.forward * sphereRadius
        );
    }
    #endregion
}