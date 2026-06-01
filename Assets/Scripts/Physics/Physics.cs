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
///
/// Design notes:
/// - Uses capsule casts and overlap checks for simple collision avoidance (no Rigidbody required).
/// - Gravity is applied to the vertical (Y) component only.
/// - Horizontal motion receives a per-frame linear damping to emulate drag.
/// - Impulses are instantaneous velocity changes (?v = impulse / mass).
/// - Forces are integrated per-frame (v += (F/m) * dt).
/// - Terminal velocity is enforced explicitly to cap downward speed.
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
    /// Basic corrections and clamping so the component has sane defaults.
    /// </summary>
    protected virtual void Awake()
    {
        // Ensure mass is positive to avoid divide-by-zero in impulses/forces.
        if (mass <= 0f) mass = 1f;

        // Reasonable defaults for capsule shape.
        if (capsuleRadius < 0.01f) capsuleRadius = 0.45f;

        // Capsule height must be >= diameter; otherwise capsule math breaks.
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
    /// Notes:
    /// - For deterministic physics step separation it's common to move this logic to FixedUpdate(),
    ///   but this transform-based system can work in Update() when consistent Time.deltaTime is used.
    /// </summary>
    protected virtual void Update()
    {
        // Convert capsule center from local-space to world-space
        Vector3 worldCenter = transform.TransformPoint(capsuleCenter);

        // Grounding: raycast downwards small distance from capsule center to decide if grounded.
        RaycastHit hit;
        IsGrounded = Physics.Raycast(worldCenter, Vector3.down, out hit, groundCheckDistance + 0.01f, groundLayers, QueryTriggerInteraction.Ignore);

        if (IsGrounded)
        {
            // If grounded and moving downward, zero the Y velocity to prevent gravity accumulation.
            if (currentVelocity.y < 0f)
                currentVelocity.y = 0f;
        }
        else
        {
            // Apply gravity to Y component: dv = g * dt
            float gravityValue = -9.81f * gravityScale;
            currentVelocity.y += gravityValue * Time.deltaTime;

            // Clamp downward speed to terminalVelocity to keep numbers stable
            if (currentVelocity.y < -Mathf.Abs(terminalVelocity))
                currentVelocity.y = -Mathf.Abs(terminalVelocity);
        }

        // Simple per-frame horizontal damping (applied to X and Z only).
        // dragFactor = 1 - linearDrag * dt ; clamped to [0,1] for stability.
        float dragFactor = Mathf.Clamp01(1f - linearDrag * Time.deltaTime);
        currentVelocity.x *= dragFactor;
        currentVelocity.z *= dragFactor;

        // Debug drawing of the ground check ray for visual debugging in the editor.
        Debug.DrawRay(transform.position + capsuleCenter, Vector3.down * (groundCheckDistance + 0.01f), IsGrounded ? Color.green : Color.red);
    }

    #endregion

    #region Movement / Cast helpers

    /// <summary>
    /// Tries to move the object by delta while performing a capsule cast to detect obstacles.
    /// - If an obstacle is hit, move as far as possible (hit.distance - skinWidth), call OnSafeMoveBlocked(hit) and return false.
    /// - If no obstacle, move full delta and return true.
    /// 
    /// Important implementation detail:
    /// - We compute capsule endpoints in world-space from capsuleHeight, capsuleRadius and capsuleCenter.
    /// - Physics.CapsuleCast returns the distance to the collision point; we subtract skinWidth so we don't clip into obstacles.
    /// </summary>
    /// <param name="delta">World-space desired displacement for this frame.</param>
    /// <returns>True if moved full delta; false if movement was blocked (partial move applied).</returns>
    public virtual bool SafeMove(Vector3 delta)
    {
        if (delta.sqrMagnitude <= Mathf.Epsilon) return true; // nothing to do

        // World-space center of the capsule
        Vector3 worldCenter = transform.TransformPoint(capsuleCenter);

        // Calculate the capsule segment endpoints (bottom & top) in world-space.
        // The capsule segment length is (capsuleHeight - 2 * capsuleRadius); center +/- half of that.
        Vector3 bottom = worldCenter + Vector3.down * (capsuleHeight * 0.5f - capsuleRadius);
        Vector3 top = worldCenter + Vector3.up * (capsuleHeight * 0.5f - capsuleRadius);

        float distance = delta.magnitude;

        RaycastHit hit;
        // Perform capsule cast along delta direction for 'distance + skinWidth' to anticipate collisions slightly ahead.
        if (Physics.CapsuleCast(bottom, top, capsuleRadius, delta.normalized, out hit, distance + skinWidth, obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            // Stop before the obstacle by skinWidth to avoid penetration.
            float moveDist = Mathf.Max(0f, hit.distance - skinWidth);
            transform.position += delta.normalized * moveDist;

            // Hook for derived classes to react to blocked movement (e.g., play bump sound or slide).
            OnSafeMoveBlocked(hit);
            return false;
        }
        else
        {
            // No obstacle; perform full move.
            transform.position += delta;
            return true;
        }
    }

    /// <summary>
    /// Resolves overlap by pushing the object out of colliders overlapping the capsule.
    /// - Iterates all overlaps and pushes the transform away from the closest point found on each collider.
    /// - Uses a simple fixed pushStrength per collider. This is not a physically accurate separation but is fast and robust.
    /// </summary>
    /// <param name="pushStrength">How much to push per overlapping collider (world units).</param>
    public virtual void ResolveOverlap(float pushStrength = 0.5f)
    {
        Vector3 worldCenter = transform.TransformPoint(capsuleCenter);
        Vector3 bottom = worldCenter + Vector3.down * (capsuleHeight * 0.5f - capsuleRadius);
        Vector3 top = worldCenter + Vector3.up * (capsuleHeight * 0.5f - capsuleRadius);

        // Get all colliders overlapping the capsule
        Collider[] hits = Physics.OverlapCapsule(bottom, top, capsuleRadius, obstacleLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider other = hits[i];
            if (other == null) continue;

            // Find closest point on the other collider to our capsule center
            Vector3 closest = other.ClosestPoint(worldCenter);

            // Direction to push ourselves away
            Vector3 pushDir = (worldCenter - closest);
            if (pushDir.sqrMagnitude <= 0.0001f)
                // If the closest point equals worldCenter (rare), use vector away from collider transform as fallback
                pushDir = (worldCenter - other.transform.position).normalized;
            else
                pushDir.Normalize();

            transform.position += pushDir * pushStrength;
        }
    }

    #endregion

    #region Force / Impulse / Knockback

    /// <summary>
    /// Apply a continuous force for one frame. Force is integrated using: v += (F / m) * dt
    /// - Use for per-frame forces (e.g., engine thrust) called from Update/FixedUpdate.
    /// </summary>
    /// <param name="force">Force in Newtons (conceptual). Treated as world-space vector.</param>
    public virtual void ApplyForce(Vector3 force)
    {
        if (mass <= 0f) mass = 1f;
        currentVelocity += (force / mass) * Time.deltaTime;
    }

    /// <summary>
    /// Apply an instantaneous impulse (instant delta-v): ?v = impulse / m
    /// - Use for collision impulses, knockbacks, jumps, etc.
    /// </summary>
    /// <param name="impulse">Impulse vector (N*s). Treated as world-space vector.</param>
    public virtual void ApplyImpulse(Vector3 impulse)
    {
        if (mass <= 0f) mass = 1f;
        currentVelocity += impulse / mass;
    }

    /// <summary>
    /// Starts a timed knockback using a coroutine.
    /// - direction: world-space direction to push.
    /// - strength: peak speed (in world units per second) applied at t=0 before falloff.
    /// - duration: how long the knockback lasts (seconds).
    /// - falloff: optional AnimationCurve from 0..1 input to 0..1 multiplier; defaults to EaseInOut (1 -> 0).
    /// 
    /// The coroutine sets currentVelocity each frame and calls SafeMove() to attempt to move while avoiding obstacles.
    /// </summary>
    public virtual void StartKnockback(Vector3 direction, float strength, float duration, AnimationCurve falloff = null)
    {
        if (knockbackRoutine != null) StopCoroutine(knockbackRoutine);
        knockbackRoutine = StartCoroutine(KnockbackCoroutine(direction.normalized, Mathf.Abs(strength), Mathf.Max(0.01f, duration), falloff));
    }

    /// <summary>
    /// Knockback coroutine implementation:
    /// - At each frame: compute velocity = dir * (strength * scale) where scale is falloff.Evaluate(t/duration).
    /// - Call SafeMove(velocity * dt) to perform collision-aware translation.
    /// - After duration ends, reset velocity to zero and call OnKnockbackEnded().
    /// </summary>
    protected virtual IEnumerator KnockbackCoroutine(Vector3 dir, float strength, float duration, AnimationCurve falloff)
    {
        float t = 0f;
        if (falloff == null) falloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        while (t < duration)
        {
            float normalized = t / duration;
            float scale = falloff.Evaluate(normalized);

            // Setting currentVelocity each frame ensures other systems reading CurrentVelocity see knockback
            currentVelocity = dir * (strength * scale);

            // Use SafeMove to respect collisions while under knockback.
            SafeMove(currentVelocity * Time.deltaTime);

            t += Time.deltaTime;
            yield return null;
        }

        // End knockback: reset velocity and clear coroutine reference
        currentVelocity = Vector3.zero;
        knockbackRoutine = null;
        OnKnockbackEnded();
    }

    #endregion

    #region Push helpers

    /// <summary>
    /// Applies an impulse pushing this object away from the surface of the given collider.
    /// - Finds the closest point on the collider relative to our capsule center and applies impulse away from that.
    /// Useful for knockback from collisions or explosive forces.
    /// </summary>
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

    /// <summary>
    /// Applies an impulse pushing this object away from a given point (e.g., explosion center).
    /// </summary>
    public virtual void PushAwayFromPoint(Vector3 point, float strength)
    {
        Vector3 dir = (transform.position - point);
        if (dir.sqrMagnitude <= 0.0001f) dir = Vector3.up;
        dir.Normalize();
        ApplyImpulse(dir * Mathf.Abs(strength));
    }

    #endregion

    #region Utility & hooks

    /// <summary>
    /// Returns true if the capsule shape overlaps any obstacle layers.
    /// </summary>
    public virtual bool IsOverlapping()
    {
        Vector3 worldCenter = transform.TransformPoint(capsuleCenter);
        Vector3 bottom = worldCenter + Vector3.down * (capsuleHeight * 0.5f - capsuleRadius);
        Vector3 top = worldCenter + Vector3.up * (capsuleHeight * 0.5f - capsuleRadius);

        var hits = Physics.OverlapCapsule(bottom, top, capsuleRadius, obstacleLayers, QueryTriggerInteraction.Ignore);
        return hits != null && hits.Length > 0;
    }

    /// <summary>
    /// Hook called when SafeMove is blocked by an obstacle.
    /// Derived classes can override to provide custom behavior (e.g., set a flag, play sound, trigger sliding).
    /// </summary>
    protected virtual void OnSafeMoveBlocked(RaycastHit hit) { }

    /// <summary>
    /// Hook called when a knockback coroutine finishes. Derived classes can override.
    /// </summary>
    protected virtual void OnKnockbackEnded() { }

    /// <summary>
    /// Immediately stops all motion and cancels active knockbacks.
    /// </summary>
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