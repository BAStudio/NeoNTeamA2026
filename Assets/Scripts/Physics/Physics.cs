using System.Collections;
using UnityEngine;

/// <summary>
/// BasePhysics
/// ----------------
/// Simple transform-based physics helper for 2.5D projects (XZ horizontal movement + Y gravity).
/// - Provides: CurrentVelocity, gravity handling, basic grounding check, SafeMove (capsule sweep),
///   impulse/force helpers, PushAway helpers, overlap resolution and a simple Knockback coroutine.
/// - This is intentionally transform-driven (not Rigidbody-driven) to keep things deterministic and simple.
/// - Child classes can override Awake/Update but should call base.Awake()/base.Update() to preserve core behaviour.
/// </summary>
[DisallowMultipleComponent]
public class BasePhysics : MonoBehaviour
{
    #region Collision Shape (casts & overlap checks)

    [Header("Collision Shape (used for casts & overlap checks)")]
    [Tooltip("Height of the capsule used for casts and overlap checks.")]
    [SerializeField] protected float capsuleHeight = 2.0f;

    [Tooltip("Radius of the capsule used for casts and overlap checks.")]
    [SerializeField] protected float capsuleRadius = 0.45f;

    [Tooltip("Local-space offset for the capsule center (usually Vector3.up * 1).")]
    [SerializeField] protected Vector3 capsuleCenter = new Vector3(0f, 1f, 0f);

    [Tooltip("Layers considered obstacles for safe-move / overlaps.")]
    [SerializeField] protected LayerMask obstacleLayers = ~0;

    #endregion

    #region Gravity & Grounding

    [Header("Gravity & Grounding")]
    [Tooltip("Multiplier for gravity (1 = ~9.81 m/s?).")]
    [SerializeField] protected float gravityScale = 1f;

    [Tooltip("Maximum downward speed (terminal velocity).")]
    [SerializeField] protected float terminalVelocity = 20f;

    [Tooltip("Distance below the capsule center to check for ground.")]
    [SerializeField] protected float groundCheckDistance = 0.15f;

    [Tooltip("Which layers count as ground for grounding checks.")]
    [SerializeField] protected LayerMask groundLayers = ~0;

    [Tooltip("When crouched, ground check distance is multiplied by this value.")]
    [SerializeField, Range(0.1f, 1f)] protected float crouchGroundCheckMultiplier = 0.5f;

    [Tooltip("Small upward push applied when grounded and standing, to prevent sinking into the floor.")]
    [SerializeField] protected float standingGroundSnapUp = 0.03f;

    #endregion

    #region General physics

    [Header("General physics")]
    [Tooltip("Mass used to scale impulses/forces.")]
    [SerializeField] protected float mass = 1f;

    [Tooltip("Simple horizontal damping applied to velocity each frame. (Higher = more damping)")]
    [SerializeField] protected float linearDrag = 6f;

    [Tooltip("Small offset used to avoid immediate collision penetration when moving (skin/padding).")]
    [SerializeField] protected float skinWidth = 0.05f;

    #endregion

    /// <summary>
    /// Current velocity vector in world space.
    /// - X,Z are horizontal axes used for movement.
    /// - Y is the vertical axis and affected by gravity.
    /// </summary>
    public Vector3 CurrentVelocity => currentVelocity;

    /// <summary>backing field for velocity</summary>
    protected Vector3 currentVelocity = Vector3.zero;

    /// <summary>True if ground was detected in Update()</summary>
    public bool IsGrounded { get; protected set; } = false;

    /// <summary>Reference to an active knockback coroutine so we can cancel it cleanly</summary>
    protected Coroutine knockbackRoutine;

    #region MonoBehaviour lifecycle

    /// <summary>
    /// Hook for derived classes: return true when crouched, so grounding can use a shorter check.
    /// </summary>
    protected virtual bool IsCrouchingForGrounding => false;

    /// <summary>
    /// Returns the active ground check distance, adjusted when crouched.
    /// </summary>
    protected virtual float GetGroundCheckDistance()
    {
        return groundCheckDistance *
               (IsCrouchingForGrounding
                    ? crouchGroundCheckMultiplier
                    : 1f);
    }

    /// <summary>
    /// Basic corrections and clamping so the component has sane defaults.
    /// </summary>
    protected virtual void Awake()
    {
        if (mass <= 0f) mass = 1f;
        if (capsuleRadius < 0.01f) capsuleRadius = 0.45f;

        if (capsuleHeight < capsuleRadius * 2f)
        {
            capsuleHeight = capsuleRadius * 2.01f;
            Debug.LogWarning($"{name}: capsuleHeight was too small; adjusted to {capsuleHeight:F2}");
        }

        if (skinWidth <= 0f) skinWidth = 0.05f;
    }

    /// <summary>
    /// Core per-frame physics update.
    /// - Performs ground raycast and updates vertical velocity using gravity.
    /// - Applies horizontal damping (simple linear drag approximation).
    /// </summary>
    protected virtual void Update()
    {
        Vector3 worldCenter = transform.TransformPoint(capsuleCenter);

        RaycastHit hit;
        float checkDistance = GetGroundCheckDistance();

        IsGrounded = Physics.Raycast(
            worldCenter,
            Vector3.down,
            out hit,
            checkDistance + 0.01f,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );

        if (IsGrounded)
        {
            if (currentVelocity.y < 0f)
                currentVelocity.y = 0f;

            if (!IsCrouchingForGrounding && hit.collider != null)
            {
                float correction = groundCheckDistance - hit.distance;
                if (correction > 0f)
                {
                    transform.position += Vector3.up * Mathf.Min(correction, standingGroundSnapUp);
                }
            }
        }
        else
        {
            float gravityValue = -9.81f * gravityScale;
            currentVelocity.y += gravityValue * Time.deltaTime;

            if (currentVelocity.y < -Mathf.Abs(terminalVelocity))
                currentVelocity.y = -Mathf.Abs(terminalVelocity);
        }

        float dragFactor = Mathf.Clamp01(1f - linearDrag * Time.deltaTime);
        currentVelocity.x *= dragFactor;
        currentVelocity.z *= dragFactor;

        Debug.DrawRay(
            transform.position + capsuleCenter,
            Vector3.down * (groundCheckDistance + 0.01f),
            IsGrounded ? Color.green : Color.red
        );
    }

    #endregion

    #region Movement / Cast helpers

    public virtual bool SafeMove(Vector3 delta)
    {
        if (delta.sqrMagnitude <= Mathf.Epsilon) return true;

        Vector3 worldCenter = transform.TransformPoint(capsuleCenter);

        Vector3 bottom = worldCenter + Vector3.down * (capsuleHeight * 0.5f - capsuleRadius);
        Vector3 top = worldCenter + Vector3.up * (capsuleHeight * 0.5f - capsuleRadius);

        float distance = delta.magnitude;

        RaycastHit hit;
        if (Physics.CapsuleCast(bottom, top, capsuleRadius, delta.normalized, out hit, distance + skinWidth, obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            float moveDist = Mathf.Max(0f, hit.distance - skinWidth);
            transform.position += delta.normalized * moveDist;
            OnSafeMoveBlocked(hit);
            return false;
        }
        else
        {
            transform.position += delta;
            return true;
        }
    }

    public virtual void ResolveOverlap(float pushStrength = 0.5f)
    {
        Vector3 worldCenter = transform.TransformPoint(capsuleCenter);
        Vector3 bottom = worldCenter + Vector3.down * (capsuleHeight * 0.5f - capsuleRadius);
        Vector3 top = worldCenter + Vector3.up * (capsuleHeight * 0.5f - capsuleRadius);

        Collider[] hits = Physics.OverlapCapsule(bottom, top, capsuleRadius, obstacleLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider other = hits[i];
            if (other == null) continue;

            Vector3 closest = other.ClosestPoint(worldCenter);

            Vector3 pushDir = (worldCenter - closest);
            if (pushDir.sqrMagnitude <= 0.0001f)
                pushDir = (worldCenter - other.transform.position).normalized;
            else
                pushDir.Normalize();

            transform.position += pushDir * pushStrength;
        }
    }

    #endregion

    #region Force / Impulse / Knockback

    public virtual void ApplyForce(Vector3 force)
    {
        if (mass <= 0f) mass = 1f;
        currentVelocity += (force / mass) * Time.deltaTime;
    }

    public virtual void ApplyImpulse(Vector3 impulse)
    {
        if (mass <= 0f) mass = 1f;
        currentVelocity += impulse / mass;
    }

    public virtual void StartKnockback(Vector3 direction, float strength, float duration, AnimationCurve falloff = null)
    {
        if (knockbackRoutine != null) StopCoroutine(knockbackRoutine);
        knockbackRoutine = StartCoroutine(KnockbackCoroutine(direction.normalized, Mathf.Abs(strength), Mathf.Max(0.01f, duration), falloff));
    }

    protected virtual IEnumerator KnockbackCoroutine(Vector3 dir, float strength, float duration, AnimationCurve falloff)
    {
        float t = 0f;
        if (falloff == null) falloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        while (t < duration)
        {
            float normalized = t / duration;
            float scale = falloff.Evaluate(normalized);

            currentVelocity = dir * (strength * scale);
            SafeMove(currentVelocity * Time.deltaTime);

            t += Time.deltaTime;
            yield return null;
        }

        currentVelocity = Vector3.zero;
        knockbackRoutine = null;
        OnKnockbackEnded();
    }

    #endregion

    #region Push helpers

    public virtual void PushAwayFromCollider(Collider other, float strength)
    {
        if (other == null) return;
        Vector3 worldCenter = transform.TransformPoint(capsuleCenter);
        Vector3 closest = other.ClosestPoint(worldCenter);
        Vector3 dir = (worldCenter - closest);
        if (dir.sqrMagnitude < 0.0001f)
            dir = (worldCenter - other.transform.position).normalized;
        else dir.Normalize();

        ApplyImpulse(dir * Mathf.Abs(strength));
    }

    public virtual void PushAwayFromPoint(Vector3 point, float strength)
    {
        Vector3 dir = (transform.position - point);
        if (dir.sqrMagnitude <= 0.0001f) dir = Vector3.up;
        dir.Normalize();
        ApplyImpulse(dir * Mathf.Abs(strength));
    }

    #endregion

    #region Utility & hooks

    public virtual bool IsOverlapping()
    {
        Vector3 worldCenter = transform.TransformPoint(capsuleCenter);
        Vector3 bottom = worldCenter + Vector3.down * (capsuleHeight * 0.5f - capsuleRadius);
        Vector3 top = worldCenter + Vector3.up * (capsuleHeight * 0.5f - capsuleRadius);

        var hits = Physics.OverlapCapsule(bottom, top, capsuleRadius, obstacleLayers, QueryTriggerInteraction.Ignore);
        return hits != null && hits.Length > 0;
    }

    protected virtual void OnSafeMoveBlocked(RaycastHit hit) { }

    protected virtual void OnKnockbackEnded() { }

    public virtual void StopMovement()
    {
        currentVelocity = Vector3.zero;
        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
            knockbackRoutine = null;
        }
    }

    #endregion
}