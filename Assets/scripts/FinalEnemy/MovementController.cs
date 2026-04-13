using UnityEngine;

public class MovementController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float alertSpeed = 2.5f;
    [SerializeField] private float stalkSpeed = 1f; // Slow creep when stalking
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float rotationSpeed = 4f;
    [SerializeField] private float movementSmoothTime = 0.08f;

    private Transform ownerTransform;
    private Vector3 movementVelocity;

    private void OnEnable()
    {
        ownerTransform = transform;
    }

    /// <summary>
    /// Moves the enemy along a path towards the target waypoint with smooth interpolation
    /// </summary>
    public void MoveTowardsWaypoint(Vector3 target, AIStateManager.AIState currentState, float deltaTime)
    {
        float speed = GetSpeedForState(currentState);
        Vector3 dir = target - ownerTransform.position;
        float distanceToTarget = dir.magnitude;
        Vector3 moveDir = distanceToTarget > 0.0001f ? dir.normalized : Vector3.zero;

        // Smooth movement with slight deceleration near waypoint
        float adjustedSpeed = speed;
        if (distanceToTarget < 1f && distanceToTarget > 0.0001f)
        {
            adjustedSpeed = speed * Mathf.Clamp01(distanceToTarget); // Decelerate smoothly near target
        }

        // Damped movement improves fluidity and removes step-like motion.
        ownerTransform.position = Vector3.SmoothDamp(
            ownerTransform.position,
            target,
            ref movementVelocity,
            movementSmoothTime,
            adjustedSpeed,
            deltaTime
        );

        // Smooth rotation to face direction - faster in chase
        float rotSpeed = currentState == AIStateManager.AIState.Chase ? 8f : 4f;
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            ownerTransform.rotation = Quaternion.Slerp(
                ownerTransform.rotation,
                Quaternion.LookRotation(moveDir),
                rotSpeed * deltaTime
            );
        }
    }

    /// <summary>
    /// Rotates in place (for search behavior)
    /// </summary>
    public void RotateInPlace(float rotationDegrees)
    {
        ownerTransform.Rotate(Vector3.up, rotationDegrees * Time.deltaTime);
    }

    /// <summary>
    /// Gets the appropriate speed for the current AI state
    /// </summary>
    private float GetSpeedForState(AIStateManager.AIState state)
    {
        return state switch
        {
            AIStateManager.AIState.Patrol => patrolSpeed,
            AIStateManager.AIState.Alert => alertSpeed,
            AIStateManager.AIState.Stalking => stalkSpeed,
            AIStateManager.AIState.Chase => chaseSpeed,
            AIStateManager.AIState.Search => patrolSpeed,
            _ => patrolSpeed
        };
    }

    public float GetPatrolSpeed() => patrolSpeed;
    public float GetAlertSpeed() => alertSpeed;
    public float GetStalkSpeed() => stalkSpeed;
    public float GetChaseSpeed() => chaseSpeed;
}
