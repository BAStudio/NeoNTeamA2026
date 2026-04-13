using UnityEngine;

public class AIStateManager : MonoBehaviour
{
    public enum AIState { Patrol, Alert, Stalking, Chase, Search }

    private AIState currentState = AIState.Patrol;
    private float alertTimer;
    private float searchTimer;
    private float lookAroundTimer;
    private float stalkTimer;

    [Header("State Timers")]
    [SerializeField] private float alertDuration = 2f;
    [SerializeField] private float stalkerDuration = 3f; // How long to stalk before committing to chase
    [SerializeField] private float searchDuration = 6f;
    [SerializeField] private float lookAroundDuration = 10f;

    /// <summary>
    /// Updates state timers
    /// </summary>
    public void Update()
    {
        if (alertTimer > 0f)
            alertTimer -= Time.deltaTime;

        if (stalkTimer > 0f)
            stalkTimer -= Time.deltaTime;

        if (searchTimer > 0f)
            searchTimer -= Time.deltaTime;

        if (lookAroundTimer > 0f)
            lookAroundTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Gets the current AI state
    /// </summary>
    public AIState GetCurrentState() => currentState;

    /// <summary>
    /// Changes the AI state
    /// </summary>
    public void SetState(AIState newState)
    {
        currentState = newState;
    }

    /// <summary>
    /// Enters Alert state
    /// </summary>
    public void EnterAlert()
    {
        currentState = AIState.Alert;
        alertTimer = alertDuration;
    }

    /// <summary>
    /// Enters Stalking state (creeping slowly toward player)
    /// </summary>
    public void EnterStalking()
    {
        currentState = AIState.Stalking;
        stalkTimer = stalkerDuration;
    }

    /// <summary>
    /// Enters Search state
    /// </summary>
    public void EnterSearch()
    {
        currentState = AIState.Search;
        searchTimer = searchDuration;
        lookAroundTimer = lookAroundDuration;
    }

    /// <summary>
    /// Enters Chase state
    /// </summary>
    public void EnterChase()
    {
        currentState = AIState.Chase;
    }

    /// <summary>
    /// Enters Patrol state
    /// </summary>
    public void EnterPatrol()
    {
        currentState = AIState.Patrol;
    }

    /// <summary>
    /// Checks if alert timer has expired
    /// </summary>
    public bool IsAlertExpired() => alertTimer <= 0f;

    /// <summary>
    /// Checks if stalk timer has expired
    /// </summary>
    public bool IsStalkExpired() => stalkTimer <= 0f;

    /// <summary>
    /// Checks if search timer has expired
    /// </summary>
    public bool IsSearchExpired() => searchTimer <= 0f;

    /// <summary>
    /// Checks if still looking around
    /// </summary>
    public bool IsLookingAround() => lookAroundTimer > 0f;

    /// <summary>
    /// Extends search time (for determined stalking)
    /// </summary>
    public void ExtendSearchTime(float additionalTime)
    {
        searchTimer += additionalTime;
    }

    public float GetSearchRadius() => 4f; // Can be made a serialized field if needed
    public float GetSearchDuration() => searchDuration;
}

