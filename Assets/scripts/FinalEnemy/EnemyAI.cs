using System.Collections.Generic;
using UnityEngine;

public class HorrorEnemyAI : MonoBehaviour, ISignalReceiver
{
    [Header("References")]
    [SerializeField] private Grid3DMaster masterGrid;
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("Optional Systems")]
    [SerializeField] private bool enableVision = true;
    [SerializeField] private bool enableHearing = true;
    [SerializeField] private bool enableSignalReceiver = true;
    [SerializeField] private bool autoAddMissingSensorComponents = true;

    [Header("Signal Detection")]
    [SerializeField] private float signalDetectionMultiplier = 40f; // detection points per unit strength
    [SerializeField] private float signalMemoryTime = 2f; // how long AI 'remembers' a signal
    [SerializeField] private float signalReceiveCooldown = 0.08f; // prevent spam stacking every rayframe

    [Header("Hearing")]
    [SerializeField] private float hearingDetectionMultiplier = 35f; // Hearing boost - stalker responds strongly to sounds

    [Header("Predictive Chasing")]
    [SerializeField] private float predictionLookAhead = 0.5f; // How far ahead to predict player movement
    [SerializeField] private float chasePathRefreshRate = 0.1f; // Faster refresh during chase (was 0.3f in pathfinding)

    [Header("Room Patrol")]
    [SerializeField] private float patrolScoutInterval = 9f;
    [SerializeField, Range(0f, 1f)] private float patrolScoutOtherRoomChance = 0.35f;

    [Header("Room/Search Look Around")]
    [SerializeField, Range(0.5f, 10f)] private float lookAroundPauseDuration = 6f;
    [SerializeField] private float lookAroundPauseRotateSpeed = 75f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;
    [SerializeField] private bool showOnScreenDebug = true;

    // Systems
    private VisionDetectionSystem visionSystem;
    private HearingDetectionSystem hearingSystem;
    private DetectionMeter detectionMeter;
    private AIStateManager stateManager;
    private PathfindingSystem pathfinding;
    private MovementController movement;

    // Internal state
    private Vector3 lastSeenPosition;
    private Vector3 lastPlayerPosition;
    private Vector3 lastPlayerVelocity;
    private float signalTimer;
    private float signalCooldownTimer;
    
    // Position history for memory-based search
    private List<Vector3> playerPositionHistory = new List<Vector3>();
    [SerializeField] private int maxPositionHistoryCount = 10;
    [SerializeField] private float positionHistoryUpdateInterval = 0.2f;
    private float positionHistoryTimer;
    
    // Search state extensions
    private int searchAttempts;
    [SerializeField] private float searchTimeExtensionPerAttempt = 2f;
    
    // Chase-specific timing
    private float chasePathTimer;
    private float patrolScoutTimer;
    private float lookAroundPauseTimer;
    private int lastKnownRoomId = -1;

    private void OnEnable()
    {
        // Get or create systems. Vision/hearing are optional.
        if (enableVision)
        {
            visionSystem = GetComponent<VisionDetectionSystem>();
            if (visionSystem == null && autoAddMissingSensorComponents)
                visionSystem = gameObject.AddComponent<VisionDetectionSystem>();
        }
        else
        {
            visionSystem = null;
        }

        if (enableHearing)
        {
            hearingSystem = GetComponent<HearingDetectionSystem>();
            if (hearingSystem == null && autoAddMissingSensorComponents)
                hearingSystem = gameObject.AddComponent<HearingDetectionSystem>();
        }
        else
        {
            hearingSystem = null;
        }

        detectionMeter = GetComponent<DetectionMeter>();
        if (detectionMeter == null) detectionMeter = gameObject.AddComponent<DetectionMeter>();

        stateManager = GetComponent<AIStateManager>();
        if (stateManager == null) stateManager = gameObject.AddComponent<AIStateManager>();

        pathfinding = GetComponent<PathfindingSystem>();
        if (pathfinding == null) pathfinding = gameObject.AddComponent<PathfindingSystem>();

        movement = GetComponent<MovementController>();
        if (movement == null) movement = gameObject.AddComponent<MovementController>();
    }

    private void Start()
    {
        if (masterGrid == null)
        {
            Debug.LogError("[AI] masterGrid missing");
            enabled = false;
            return;
        }

        // Initialize systems
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (visionSystem != null)
            visionSystem.Initialize(player);
        if (hearingSystem != null)
            hearingSystem.Initialize(player);
        pathfinding.SetMasterGrid(masterGrid);

        if (enableVision && visionSystem == null && verboseLogs)
            Debug.LogWarning("[AI] Vision enabled but VisionDetectionSystem is missing.");
        if (enableHearing && hearingSystem == null && verboseLogs)
            Debug.LogWarning("[AI] Hearing enabled but HearingDetectionSystem is missing.");

        lastPlayerPosition = player != null ? player.position : Vector3.zero;
        pathfinding.PickPatrolInCurrentRoom();
        patrolScoutTimer = patrolScoutInterval;
        lastKnownRoomId = pathfinding.GetRoomIdAtPosition(transform.position);
    }

    private void Update()
    {
        // Update sub-systems
        stateManager.Update();
        bool canUseVision = CanUseVisionSense();
        bool seesPlayer = canUseVision && visionSystem.CanSeePlayer();
        detectionMeter.Update(seesPlayer, stateManager.GetCurrentState());

        // Update position history for memory-based search
        if (player != null)
        {
            positionHistoryTimer -= Time.deltaTime;
            if (positionHistoryTimer <= 0f)
            {
                playerPositionHistory.Insert(0, player.position);
                if (playerPositionHistory.Count > maxPositionHistoryCount)
                    playerPositionHistory.RemoveAt(playerPositionHistory.Count - 1);
                positionHistoryTimer = positionHistoryUpdateInterval;
            }
        }

        // Update timers
        if (signalCooldownTimer > 0f)
            signalCooldownTimer -= Time.deltaTime;

        if (signalTimer > 0f)
        {
            signalTimer -= Time.deltaTime;
            // If AI was patrolling, bias to investigate the signal briefly
            if (stateManager.GetCurrentState() == AIStateManager.AIState.Patrol)
            {
                stateManager.EnterAlert();
                pathfinding.GeneratePath(lastSeenPosition);
            }
        }

        // Vision and hearing checks are optional.
        if (canUseVision)
            visionSystem.CastVisionRaysAndCountHits(GetLookDirection());

        bool canUseHearing = CanUseHearingSense();
        bool hearsPlayer = canUseHearing && hearingSystem.CanHearPlayer();

        if (hearsPlayer)
        {
            // Strong hearing response for stalker behavior
            detectionMeter.AddDetection(hearingDetectionMultiplier);
            lastSeenPosition = player.position;
        }

        // State machine - only update if not reacting to recent signal
        if (signalTimer <= 0f)
        {
            UpdateStateBasedOnDetection();
        }

        // Execute behavior based on current state
        ExecuteBehavior();

        // Trigger a brief scan when entering a different room while patrolling.
        CheckRoomEntryLookAround();

        // Update player velocity smoothing when player exists.
        if (player != null)
        {
            Vector3 currentPlayerVelocity = (player.position - lastPlayerPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
            lastPlayerVelocity = Vector3.Lerp(lastPlayerVelocity, currentPlayerVelocity, 0.5f);
            lastPlayerPosition = player.position;
        }
    }

    private void FixedUpdate()
    {
        if (lookAroundPauseTimer > 0f)
        {
            lookAroundPauseTimer -= Time.fixedDeltaTime;
            movement.RotateInPlace(lookAroundPauseRotateSpeed);
            return;
        }

        // High-frequency path recalculation during chase for smooth, responsive movement
        if (player != null && stateManager.GetCurrentState() == AIStateManager.AIState.Chase)
        {
            chasePathTimer -= Time.fixedDeltaTime;
            if (chasePathTimer <= 0f)
            {
                // Predict where player will be and reroute
                Vector3 predictedPosition = player.position + (lastPlayerVelocity * predictionLookAhead);
                pathfinding.GeneratePath(predictedPosition);
                chasePathTimer = chasePathRefreshRate;
            }
        }

        // Keep movement and path stepping on the same fixed timestep for smoother motion.
        Vector3? waypoint = pathfinding.GetNextWaypoint();
        if (waypoint.HasValue)
        {
            movement.MoveTowardsWaypoint(waypoint.Value, stateManager.GetCurrentState(), Time.fixedDeltaTime);
            pathfinding.UpdatePathProgress();
        }
    }

    private void UpdateStateBasedOnDetection()
    {
        float detection = detectionMeter.GetDetection();
        float maxDetection = detectionMeter.GetMaxDetection();

        // Chase state persists until detection drops below 95%
        if (stateManager.GetCurrentState() == AIStateManager.AIState.Chase)
        {
            if (detection < maxDetection * 0.95f)
            {
                stateManager.EnterSearch();
                searchAttempts = 0; // Reset search attempts on exit from chase
            }
            return;
        }

        // Fully detected - enter Chase (70% threshold, lowered from 80%)
        if (detection >= maxDetection * 0.7f)
        {
            stateManager.EnterChase();
        }
        // Stalking threshold (50% of max) - slow creep toward last seen position
        else if (detection >= maxDetection * 0.5f)
        {
            if (stateManager.GetCurrentState() != AIStateManager.AIState.Stalking)
            {
                stateManager.EnterStalking();
                pathfinding.GeneratePath(lastSeenPosition);
            }
        }
        // Alert threshold (30% of max, lowered from 40%)
        else if (detection >= maxDetection * 0.3f)
        {
            if (stateManager.GetCurrentState() != AIStateManager.AIState.Alert && stateManager.GetCurrentState() != AIStateManager.AIState.Stalking)
            {
                stateManager.EnterAlert();
                pathfinding.GeneratePath(lastSeenPosition);
            }
        }
        // Back to Patrol
        else
        {
            if (stateManager.GetCurrentState() != AIStateManager.AIState.Patrol && stateManager.GetCurrentState() != AIStateManager.AIState.Search)
            {
                stateManager.EnterPatrol();
                pathfinding.PickPatrolInCurrentRoom();
                searchAttempts = 0;
                patrolScoutTimer = patrolScoutInterval;
            }
        }
    }

    private void ExecuteBehavior()
    {
        var state = stateManager.GetCurrentState();

        switch (state)
        {
            case AIStateManager.AIState.Patrol:
                ExecutePatrol();
                break;
            case AIStateManager.AIState.Alert:
                ExecuteAlert();
                break;
            case AIStateManager.AIState.Stalking:
                ExecuteStalking();
                break;
            case AIStateManager.AIState.Chase:
                ExecuteChase();
                break;
            case AIStateManager.AIState.Search:
                ExecuteSearch();
                break;
        }
    }

    private void ExecutePatrol()
    {
        patrolScoutTimer -= Time.deltaTime;

        if (pathfinding.IsPathComplete())
        {
            bool shouldScoutOtherRoom = patrolScoutTimer <= 0f && Random.value <= patrolScoutOtherRoomChance;
            bool routedToOtherRoom = false;

            if (shouldScoutOtherRoom)
            {
                routedToOtherRoom = pathfinding.PickPatrolInDifferentRoom();
                patrolScoutTimer = patrolScoutInterval;
            }

            if (!routedToOtherRoom)
                pathfinding.PickPatrolInCurrentRoom();
        }
    }

    private void ExecuteAlert()
    {
        if (pathfinding.IsPathComplete())
            pathfinding.GeneratePath(lastSeenPosition);

        if (stateManager.IsAlertExpired())
        {
            if (detectionMeter.IsFullyDetected())
                stateManager.EnterChase();
            else if (detectionMeter.IsStalking())
                stateManager.EnterStalking();
            else
                stateManager.EnterSearch();
        }
    }

    /// <summary>
    /// Stalking: Slow creep toward last seen position - eerie and threatening
    /// </summary>
    private void ExecuteStalking()
    {
        if (pathfinding.IsPathComplete())
            pathfinding.GeneratePath(lastSeenPosition);

        if (stateManager.IsStalkExpired())
        {
            // If still detecting something, commit to chase
            if (detectionMeter.IsFullyDetected())
            {
                stateManager.EnterChase();
            }
            else if (detectionMeter.IsStalking())
            {
                // Extend stalking time - determined behavior
                stateManager.EnterStalking();
            }
            else
            {
                stateManager.EnterSearch();
            }
        }
    }

    /// <summary>
    /// Chase: Full pursuit with predictive pathfinding (handled in FixedUpdate for smooth recalculation)
    /// </summary>
    private void ExecuteChase()
    {
        if (player != null)
            lastSeenPosition = player.position;
        else
            stateManager.EnterSearch();
        
        // Path recalculation is handled in FixedUpdate for consistent, smooth updates
    }

    /// <summary>
    /// Search: Uses position history to track player's previous path
    /// </summary>
    private void ExecuteSearch()
    {
        if (stateManager.IsLookingAround())
        {
            // Rotate in place to "look around"
            movement.RotateInPlace(60f);
            return;
        }

        if (pathfinding.IsPathComplete())
        {
            // Use position history for memory-based search
            Vector3 searchTarget;
            if (playerPositionHistory.Count > 0)
            {
                // Follow the oldest known position in history (backtracking the player's path)
                searchTarget = playerPositionHistory[playerPositionHistory.Count - 1];
            }
            else
            {
                // Fallback to random search around last seen position
                searchTarget = lastSeenPosition + Random.insideUnitSphere * stateManager.GetSearchRadius();
            }
            searchTarget.y = transform.position.y;
            pathfinding.GeneratePath(searchTarget);
        }

        if (stateManager.IsSearchExpired())
        {
            // Extend search time - stalker is determined
            searchAttempts++;
            if (searchAttempts < 3) // Try up to 3 search cycles
            {
                stateManager.ExtendSearchTime(searchTimeExtensionPerAttempt);
            }
            else
            {
                // Give up after multiple attempts
                detectionMeter.Reset();
                stateManager.EnterPatrol();
                pathfinding.PickPatrolInCurrentRoom();
                searchAttempts = 0;
                patrolScoutTimer = patrolScoutInterval;
                StartLookAroundPause();
            }
        }
    }

    private void CheckRoomEntryLookAround()
    {
        if (stateManager.GetCurrentState() != AIStateManager.AIState.Patrol)
            return;

        int currentRoomId = pathfinding.GetRoomIdAtPosition(transform.position);
        if (currentRoomId < 0)
            return;

        if (lastKnownRoomId >= 0 && currentRoomId != lastKnownRoomId)
            StartLookAroundPause();

        lastKnownRoomId = currentRoomId;
    }

    private void StartLookAroundPause()
    {
        lookAroundPauseTimer = Mathf.Min(10f, lookAroundPauseDuration);
    }

    private Vector3 GetLookDirection()
    {
        Vector3 nextWaypoint = pathfinding.GetNextWaypoint() ?? transform.position + transform.forward;
        Vector3 dirToNext = nextWaypoint - transform.position;
        dirToNext.y = 0f;
        return dirToNext.sqrMagnitude > 0.0001f ? dirToNext.normalized : transform.forward;
    }

    private bool CanUseVisionSense()
    {
        return enableVision && visionSystem != null && player != null;
    }

    private bool CanUseHearingSense()
    {
        return enableHearing && hearingSystem != null && player != null;
    }

    private bool CanUseSignalSense()
    {
        return enableSignalReceiver && detectionMeter != null && stateManager != null && pathfinding != null;
    }

    #region Signal Receiver
    /// <summary>
    /// Implements ISignalReceiver. Called by wave emitter.
    /// strength is expected to be in 0..1
    /// </summary>
    public void OnSignalReceived(Vector3 source, float strength)
    {
        if (!CanUseSignalSense()) return;
        if (signalCooldownTimer > 0f) return;
        signalCooldownTimer = signalReceiveCooldown;

        float addedDetection = strength * signalDetectionMultiplier;
        detectionMeter.AddDetection(addedDetection);

        lastSeenPosition = source;
        signalTimer = signalMemoryTime;

        // If strong signal, escalate state
        if (detectionMeter.IsFullyDetected())
            stateManager.EnterChase();
        else
            stateManager.EnterAlert();

        pathfinding.GeneratePath(lastSeenPosition);

        if (verboseLogs)
            Debug.Log($"[AI] Signal received | Strength: {strength:F2} | Added: {addedDetection:F1} | New detection: {detectionMeter.GetDetection():F1}");
    }
    #endregion

    #region Debug
    private void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;

        if (!Application.isPlaying)
            return;

        // Get systems for debugging
        var vision = GetComponent<VisionDetectionSystem>();
        var hearing = GetComponent<HearingDetectionSystem>();
        var pathSys = GetComponent<PathfindingSystem>();

        if (enableVision && vision != null)
        {
            Vector3 lookDir = GetLookDirection();
            vision.DrawDebugGizmos(pos, lookDir);
        }

        if (enableHearing && hearing != null)
            hearing.DrawDebugGizmos(pos);

        if (pathSys != null)
            pathSys.DrawDebugGizmos();
    }

    private void OnGUI()
    {
        if (!showOnScreenDebug) return;
        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white } };
        Rect rect = new Rect(10, 10, 400, 250);
        string s = $"AI State: {stateManager.GetCurrentState()}\n" +
                   $"Detection: {detectionMeter.GetDetection():F1}/{detectionMeter.GetMaxDetection()}\n" +
                   $"LastSeen: {lastSeenPosition}\n" +
                   $"Path nodes: {(pathfinding.GetCurrentPath() == null ? 0 : pathfinding.GetCurrentPath().Count)}\n" +
                   $"Rooms: {pathfinding.GetDetectedRoomCount()}\n" +
                   $"PauseTimer: {lookAroundPauseTimer:F1}s\n" +
                   $"Position History: {playerPositionHistory.Count}\n" +
                   $"Search Attempts: {searchAttempts}";
        GUI.Label(rect, s, style);
    }
    #endregion
}