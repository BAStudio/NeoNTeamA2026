using UnityEngine;

public class MovementController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float alertSpeed = 3f;
    [SerializeField] private float stalkSpeed = 1.2f;
    [SerializeField] private float chaseSpeed = 5f;

    [Header("Rotation")]
    [SerializeField] private float patrolRotationSpeed = 180f;
    [SerializeField] private float stalkRotationSpeed = 140f;
    [SerializeField] private float chaseRotationSpeed = 360f;

    [Header("Waypoint Smoothing")]
    [SerializeField] private float patrolSlowdownDistance = 0.55f;
    [SerializeField] private float alertSlowdownDistance = 0.6f;
    [SerializeField] private float stalkSlowdownDistance = 0.35f;
    [SerializeField] private float chaseSlowdownDistance = 0.75f;

    [SerializeField] private float patrolMinMultiplier = 0.50f;
    [SerializeField] private float alertMinMultiplier = 0.60f;
    [SerializeField] private float stalkMinMultiplier = 0.85f;
    [SerializeField] private float chaseMinMultiplier = 0.68f;

    [Header("Arrival")]
    [SerializeField] private float arriveThreshold = 0.05f;

    private Transform ownerTransform;

    private void OnEnable()
    {
        ownerTransform = transform;
    }

    public bool MoveTowardsWaypoint(Vector3 target, AIStateManager.AIState currentState, float deltaTime)
    {
        Vector3 currentPos = ownerTransform.position;

        Vector3 targetPos = new Vector3(
            target.x,
            currentPos.y,
            target.z);

        Vector3 toTarget = targetPos - currentPos;
        float distance = toTarget.magnitude;

        if (distance <= arriveThreshold)
        {
            ownerTransform.position = targetPos;
            return true;
        }

        Vector3 moveDir = toTarget.normalized;

        float speed = GetSpeedForState(currentState);
        float slowdownDistance = GetSlowdownDistance(currentState);
        float minMultiplier = GetMinSpeedMultiplier(currentState);

        float t = slowdownDistance > 0f
            ? Mathf.Clamp01(distance / slowdownDistance)
            : 1f;

        t = t * t * (3f - 2f * t);

        float adjustedSpeed = Mathf.Lerp(speed * minMultiplier, speed, t);
        float moveStep = adjustedSpeed * deltaTime;

        if (moveStep >= distance)
        {
            ownerTransform.position = targetPos;
            return true;
        }

        ownerTransform.position = currentPos + moveDir * moveStep;

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            float rotSpeed = GetRotationSpeed(currentState);
            Quaternion targetRotation = Quaternion.LookRotation(-moveDir, Vector3.up);

            ownerTransform.rotation = Quaternion.RotateTowards(
                ownerTransform.rotation,
                targetRotation,
                rotSpeed * deltaTime);
        }

        return false;
    }

    public void RotateInPlace(float rotationDegreesPerSecond, float deltaTime)
    {
        ownerTransform.Rotate(Vector3.up, rotationDegreesPerSecond * deltaTime);
    }

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

    private float GetSlowdownDistance(AIStateManager.AIState state)
    {
        return state switch
        {
            AIStateManager.AIState.Patrol => patrolSlowdownDistance,
            AIStateManager.AIState.Alert => alertSlowdownDistance,
            AIStateManager.AIState.Stalking => stalkSlowdownDistance,
            AIStateManager.AIState.Chase => chaseSlowdownDistance,
            AIStateManager.AIState.Search => patrolSlowdownDistance,
            _ => patrolSlowdownDistance
        };
    }

    private float GetMinSpeedMultiplier(AIStateManager.AIState state)
    {
        return state switch
        {
            AIStateManager.AIState.Patrol => patrolMinMultiplier,
            AIStateManager.AIState.Alert => alertMinMultiplier,
            AIStateManager.AIState.Stalking => stalkMinMultiplier,
            AIStateManager.AIState.Chase => chaseMinMultiplier,
            AIStateManager.AIState.Search => patrolMinMultiplier,
            _ => patrolMinMultiplier
        };
    }

    private float GetRotationSpeed(AIStateManager.AIState state)
    {
        return state switch
        {
            AIStateManager.AIState.Stalking => stalkRotationSpeed,
            AIStateManager.AIState.Chase => chaseRotationSpeed,
            _ => patrolRotationSpeed
        };
    }
}