using UnityEngine;

public class AIStateManager : MonoBehaviour
{
    public enum AIState
    {
        Patrol,
        Alert,
        Stalking,
        Chase,
        Search
    }

    [Header("State Timers")]
    [SerializeField] private float alertDuration = 5f;
    [SerializeField] private float stalkingDuration = 6f;
    [SerializeField] private float searchDuration = 10f;
    [SerializeField] private float lookAroundDuration = 6f;

    [Header("Memory")]
    [SerializeField] private float playerMemoryDuration = 8f;

    [Header("Search")]
    [SerializeField] private float searchRadius = 4f;

    private AIState currentState = AIState.Patrol;

    private float alertTimer;
    private float stalkingTimer;
    private float searchTimer;
    private float lookAroundTimer;
    private float memoryTimer;

    private Vector3 lastKnownPlayerPosition;

    public void Tick(float deltaTime)
    {
        if (alertTimer > 0f) alertTimer -= deltaTime;
        if (stalkingTimer > 0f) stalkingTimer -= deltaTime;
        if (searchTimer > 0f) searchTimer -= deltaTime;
        if (lookAroundTimer > 0f) lookAroundTimer -= deltaTime;
        if (memoryTimer > 0f) memoryTimer -= deltaTime;
    }

    public AIState GetCurrentState() => currentState;

    public void RegisterPlayerPosition(Vector3 pos)
    {
        lastKnownPlayerPosition = pos;
        memoryTimer = playerMemoryDuration;
    }

    public Vector3 GetLastKnownPlayerPosition()
    {
        return lastKnownPlayerPosition;
    }

    public bool HasPlayerMemory()
    {
        return memoryTimer > 0f;
    }

    public void EnterPatrol()
    {
        currentState = AIState.Patrol;
    }

    public void EnterAlert()
    {
        if (currentState == AIState.Chase)
            return;

        currentState = AIState.Alert;
        alertTimer = alertDuration;
    }

    public void EnterStalking()
    {
        if (currentState == AIState.Chase)
            return;

        currentState = AIState.Stalking;
        stalkingTimer = stalkingDuration;
    }

    public void EnterChase()
    {
        currentState = AIState.Chase;
    }

    public void EnterSearch()
    {
        currentState = AIState.Search;
        searchTimer = searchDuration;

        if (HasPlayerMemory())
            searchTimer += 3f;

        lookAroundTimer = lookAroundDuration;
    }

    public void RestartLookAround(float duration)
    {
        lookAroundTimer = duration;
    }

    public void ExtendSearchTime(float additionalTime)
    {
        searchTimer += additionalTime;
    }

    public bool IsAlertExpired()
    {
        return alertTimer <= 0f;
    }

    public bool IsStalkExpired()
    {
        return stalkingTimer <= 0f;
    }

    public bool IsSearchExpired()
    {
        return searchTimer <= 0f;
    }

    public bool IsLookingAround()
    {
        return lookAroundTimer > 0f;
    }

    public float GetSearchRadius()
    {
        return searchRadius;
    }

    public float GetLookAroundDuration()
    {
        return lookAroundDuration;
    }
}