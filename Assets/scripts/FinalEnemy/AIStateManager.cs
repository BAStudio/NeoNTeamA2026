using UnityEngine;

public class AIStateManager : MonoBehaviour
{
    public enum AIState { Patrol, Alert, Stalking, Chase, Search }

    [Header("State Timers")]
    [SerializeField] private float alertDuration = 4f;
    [SerializeField] private float stalkerDuration = 4f;
    [SerializeField] private float searchDuration = 8f;
    [SerializeField] private float lookAroundDuration = 6f;

    private AIState currentState = AIState.Patrol;
    private float alertTimer;
    private float searchTimer;
    private float lookAroundTimer;
    private float stalkTimer;

    public void Tick(float deltaTime)
    {
        if (alertTimer > 0f) alertTimer -= deltaTime;
        if (stalkTimer > 0f) stalkTimer -= deltaTime;
        if (searchTimer > 0f) searchTimer -= deltaTime;
        if (lookAroundTimer > 0f) lookAroundTimer -= deltaTime;
    }

    public AIState GetCurrentState() => currentState;

    public void SetState(AIState newState)
    {
        currentState = newState;
    }

    public void EnterAlert()
    {
        currentState = AIState.Alert;
        alertTimer = alertDuration;
    }

    public void EnterStalking()
    {
        currentState = AIState.Stalking;
        stalkTimer = stalkerDuration;
    }

    public void EnterSearch()
    {
        currentState = AIState.Search;
        searchTimer = searchDuration;
        lookAroundTimer = lookAroundDuration;
    }

    public void EnterChase()
    {
        currentState = AIState.Chase;
    }

    public void EnterPatrol()
    {
        currentState = AIState.Patrol;
    }

    public bool IsAlertExpired() => alertTimer <= 0f;
    public bool IsStalkExpired() => stalkTimer <= 0f;
    public bool IsSearchExpired() => searchTimer <= 0f;
    public bool IsLookingAround() => lookAroundTimer > 0f;

    public void ExtendSearchTime(float additionalTime)
    {
        searchTimer += additionalTime;
    }

    public float GetSearchRadius() => 4f;
    public float GetSearchDuration() => searchDuration;
}