using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HorrorEnemyAI : MonoBehaviour, ISignalReceiver
{
    [Header("References")]
    [SerializeField] private Grid3DMaster masterGrid;
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("Perception")]
    [SerializeField] private bool enableSoundSense = true;
    [SerializeField] private bool enableCameraSignals = true;
    [SerializeField] private bool enableVisionSense = true;
    [SerializeField] private bool autoAddMissingSensorComponents = true;
    [SerializeField] private float cameraDetectionMultiplier = 45f;
    [SerializeField] private float visionDetectionMultiplier = 30f;
    [SerializeField] private float soundFallbackMultiplier = 20f;
    [SerializeField] private float signalMemoryTime = 2.5f;
    [SerializeField] private float signalReceiveCooldown = 0.08f;
    [SerializeField] private float visionScanInterval = 0.12f;

    [Header("Predictive Chasing")]
    [SerializeField] private float predictionLookAhead = 0.5f;
    [SerializeField] private float chasePathRefreshRate = 0.18f;

    [Header("Room Patrol")]
    [SerializeField] private float patrolScoutInterval = 9f;
    [SerializeField, Range(0f, 1f)] private float patrolScoutOtherRoomChance = 0.35f;

    [Header("Room/Search Look Around")]
    [SerializeField, Range(0.5f, 10f)] private float lookAroundPauseDuration = 6f;
    [SerializeField] private float lookAroundPauseRotateSpeed = 75f;
    [SerializeField] private float searchTimeExtensionPerAttempt = 2f;
    [SerializeField] private int maxSearchAttempts = 3;

    [Header("Thresholds")]
    [SerializeField, Range(0f, 1f)] private float alertThresholdPercent = 0.25f;
    [SerializeField, Range(0f, 1f)] private float stalkingThresholdPercent = 0.5f;
    [SerializeField, Range(0f, 1f)] private float chaseThresholdPercent = 0.7f;
    [SerializeField, Range(0f, 1f)] private float chaseDropThresholdPercent = 0.55f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;
    [SerializeField] private bool showOnScreenDebug = true;

    private HearingDetectionSystem hearingSystem;
    private VisionDetectionSystem visionSystem;
    private DetectionMeter detectionMeter;
    private AIStateManager stateManager;
    private PathfindingSystem pathfinding;
    private MovementController movement;

    private Vector3 lastKnownPosition;
    private Vector3 lastPlayerPosition;
    private Vector3 lastPlayerVelocity;
    private float memoryTimer;
    private float signalCooldownTimer;

    private readonly List<Vector3> playerPositionHistory = new List<Vector3>();
    [SerializeField] private int maxPositionHistoryCount = 10;
    [SerializeField] private float positionHistoryUpdateInterval = 0.2f;
    private float positionHistoryTimer;

    private int searchAttempts;
    private float chasePathTimer;
    private float visionScanTimer;
    private float patrolScoutTimer;
    private float lookAroundPauseTimer;
    private int lastKnownRoomId = -1;

    private bool killTriggered;

    private Transform ownerTransform;

    private void OnEnable()
    {
        ownerTransform = transform;
        killTriggered = false;
        visionScanTimer = 0f;

        hearingSystem = GetComponent<HearingDetectionSystem>();
        if (enableSoundSense && hearingSystem == null && autoAddMissingSensorComponents)
            hearingSystem = gameObject.AddComponent<HearingDetectionSystem>();

        visionSystem = GetComponent<VisionDetectionSystem>();
        if (enableVisionSense && visionSystem == null && autoAddMissingSensorComponents)
            visionSystem = gameObject.AddComponent<VisionDetectionSystem>();

        detectionMeter = GetComponent<DetectionMeter>();
        if (detectionMeter == null)
            detectionMeter = gameObject.AddComponent<DetectionMeter>();

        stateManager = GetComponent<AIStateManager>();
        if (stateManager == null)
            stateManager = gameObject.AddComponent<AIStateManager>();

        pathfinding = GetComponent<PathfindingSystem>();
        if (pathfinding == null)
            pathfinding = gameObject.AddComponent<PathfindingSystem>();

        movement = GetComponent<MovementController>();
        if (movement == null)
            movement = gameObject.AddComponent<MovementController>();
    }

    private void Start()
    {
        if (masterGrid == null)
        {
            Debug.LogError("[AI] masterGrid missing");
            enabled = false;
            return;
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
                player = playerObj.transform;
        }

        pathfinding.SetMasterGrid(masterGrid);

        lastKnownPosition = transform.position;
        if (player != null)
        {
            lastPlayerPosition = player.position;
            lastKnownPosition = player.position;
        }

        if (visionSystem != null && player != null)
            visionSystem.Initialize(player);

        pathfinding.PickPatrolInCurrentRoom();
        patrolScoutTimer = patrolScoutInterval;
        lastKnownRoomId = pathfinding.GetRoomIdAtPosition(transform.position);
    }

    private void Update()
    {
        if (stateManager == null || detectionMeter == null || pathfinding == null || movement == null)
            return;

        float dt = Time.deltaTime;

        stateManager.Tick(dt);
        detectionMeter.Tick(stateManager.GetCurrentState(), dt);

        if (signalCooldownTimer > 0f)
            signalCooldownTimer -= dt;

        if (memoryTimer > 0f)
            memoryTimer -= dt;

        if (player != null)
        {
            positionHistoryTimer -= dt;
            if (positionHistoryTimer <= 0f)
            {
                playerPositionHistory.Insert(0, player.position);
                if (playerPositionHistory.Count > maxPositionHistoryCount)
                    playerPositionHistory.RemoveAt(playerPositionHistory.Count - 1);

                positionHistoryTimer = positionHistoryUpdateInterval;
            }
        }

        if (player != null)
        {
            Vector3 currentPlayerVelocity = (player.position - lastPlayerPosition) / Mathf.Max(dt, 0.0001f);
            lastPlayerVelocity = Vector3.Lerp(lastPlayerVelocity, currentPlayerVelocity, 0.5f);
            lastPlayerPosition = player.position;

            if (visionSystem != null && visionSystem.GetPlayerTransform() == null)
                visionSystem.Initialize(player);
        }

        UpdateVisionDetection(dt);

        UpdateStateBasedOnDetection();
        ExecuteBehavior();
        CheckRoomEntryLookAround();
    }

    private void FixedUpdate()
    {
        if (stateManager == null || pathfinding == null || movement == null)
            return;

        if (lookAroundPauseTimer > 0f)
        {
            lookAroundPauseTimer -= Time.fixedDeltaTime;
            movement.RotateInPlace(lookAroundPauseRotateSpeed, Time.fixedDeltaTime);
            return;
        }

        if (stateManager.GetCurrentState() == AIStateManager.AIState.Search && stateManager.IsLookingAround())
        {
            movement.RotateInPlace(60f, Time.fixedDeltaTime);
            return;
        }

        if (player != null && stateManager.GetCurrentState() == AIStateManager.AIState.Chase)
        {
            chasePathTimer -= Time.fixedDeltaTime;

            if (chasePathTimer <= 0f)
            {
                Vector3 predictedPosition = player.position + (lastPlayerVelocity * predictionLookAhead);
                predictedPosition.y = transform.position.y;

                pathfinding.GeneratePath(predictedPosition);
                chasePathTimer = chasePathRefreshRate;
            }
        }

        pathfinding.UpdatePathProgress();

        Vector3? waypoint = pathfinding.GetNextWaypoint();
        if (waypoint.HasValue)
        {
            movement.MoveTowardsWaypoint(
                waypoint.Value,
                stateManager.GetCurrentState(),
                Time.fixedDeltaTime
            );
        }

        if (pathfinding != null)
            transform.position = pathfinding.ClampToWalkableGrid(transform.position);

        UpdatePassiveHearing(Time.fixedDeltaTime);
    }

    /// <summary>
    /// Sound input from footsteps, impacts, sprinting, etc.
    /// volume01 should be 0..1.
    /// </summary>
    public void OnSoundHeard(Vector3 source, float volume01)
    {
        if (!enableSoundSense) return;
        if (signalCooldownTimer > 0f) return;

        signalCooldownTimer = signalReceiveCooldown;

        float addedDetection = hearingSystem != null
            ? hearingSystem.CalculateStimulus(source, volume01)
            : Mathf.Max(0f, volume01) * soundFallbackMultiplier;

        if (addedDetection <= 0f)
            return;

        RegisterStimulus(source, addedDetection, false);

        if (verboseLogs)
            Debug.Log($"[AI] Sound stimulus | Volume: {volume01:F2} | Added: {addedDetection:F1}");
    }

    /// <summary>
    /// Camera / alarm / scripted sighting input.
    /// strength should be 0..1.
    /// </summary>
    public void OnSignalReceived(Vector3 source, float strength)
    {
        if (!enableCameraSignals) return;
        if (signalCooldownTimer > 0f) return;

        signalCooldownTimer = signalReceiveCooldown;

        float addedDetection = Mathf.Clamp01(strength) * cameraDetectionMultiplier;
        if (addedDetection <= 0f)
            return;

        RegisterStimulus(source, addedDetection, true);

        if (verboseLogs)
            Debug.Log($"[AI] Camera signal | Strength: {strength:F2} | Added: {addedDetection:F1}");
    }

    private void UpdateVisionDetection(float deltaTime)
    {
        if (!enableVisionSense || visionSystem == null || player == null || signalCooldownTimer > 0f)
            return;

        visionScanTimer -= deltaTime;
        if (visionScanTimer > 0f)
            return;

        visionScanTimer = visionScanInterval;

        if (!visionSystem.CanSeeTarget(player.position))
            return;

        signalCooldownTimer = signalReceiveCooldown;
        RegisterStimulus(player.position, visionDetectionMultiplier * deltaTime, true);

        if (verboseLogs)
            Debug.Log($"[AI] Vision stimulus | Added: {(visionDetectionMultiplier * deltaTime):F1}");
    }

    private void RegisterStimulus(Vector3 source, float addedDetection, bool fromCamera)
    {
        if (detectionMeter == null || stateManager == null || pathfinding == null)
            return;

        detectionMeter.AddDetection(addedDetection);

        lastKnownPosition = source;
        memoryTimer = signalMemoryTime;
        searchAttempts = 0;

        int roomId = pathfinding.GetRoomIdAtPosition(source);
        if (roomId >= 0)
            lastKnownRoomId = roomId;

        if (detectionMeter.IsFullyDetected())
            stateManager.EnterChase();
        else if (detectionMeter.IsStalking())
            stateManager.EnterStalking();
        else
            stateManager.EnterAlert();

        pathfinding.GeneratePath(lastKnownPosition);

        if (!fromCamera && verboseLogs)
            Debug.Log($"[AI] Heard source at {source}");
    }

    private void UpdatePassiveHearing(float deltaTime)
    {
        if (!enableSoundSense || hearingSystem == null || player == null || detectionMeter == null || pathfinding == null)
            return;

        float addedDetection = hearingSystem.CalculateStimulus(player.position, 1f) * deltaTime;
        if (addedDetection <= 0f)
            return;

        detectionMeter.AddDetection(addedDetection);

        lastKnownPosition = player.position;
        memoryTimer = signalMemoryTime;

        int roomId = pathfinding.GetRoomIdAtPosition(player.position);
        if (roomId >= 0)
            lastKnownRoomId = roomId;
    }

    private void UpdateStateBasedOnDetection()
    {
        float detection = detectionMeter.GetDetection();
        float maxDetection = detectionMeter.GetMaxDetection();
        var currentState = stateManager.GetCurrentState();

        if (currentState == AIStateManager.AIState.Chase)
        {
            if (detection < maxDetection * chaseDropThresholdPercent && memoryTimer <= 0f)
            {
                stateManager.EnterSearch();
                searchAttempts = 0;
            }
            return;
        }

        if (detection >= maxDetection * chaseThresholdPercent)
        {
            if (currentState != AIStateManager.AIState.Chase)
                stateManager.EnterChase();

            return;
        }

        if (detection >= maxDetection * stalkingThresholdPercent)
        {
            if (currentState != AIStateManager.AIState.Stalking)
                stateManager.EnterStalking();

            return;
        }

        if (detection >= maxDetection * alertThresholdPercent || memoryTimer > 0f)
        {
            if (currentState == AIStateManager.AIState.Patrol)
                stateManager.EnterAlert();

            return;
        }
    }

    private void ExecuteBehavior()
    {
        switch (stateManager.GetCurrentState())
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
            pathfinding.GeneratePath(lastKnownPosition);

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

    private void ExecuteStalking()
    {
        if (pathfinding.IsPathComplete())
            pathfinding.GeneratePath(lastKnownPosition);

        if (stateManager.IsStalkExpired())
        {
            if (detectionMeter.IsFullyDetected())
                stateManager.EnterChase();
            else if (detectionMeter.IsStalking())
                stateManager.EnterStalking();
            else
                stateManager.EnterSearch();
        }
    }

    private void ExecuteChase()
    {
        if (player != null)
            lastKnownPosition = player.position;
        else
            stateManager.EnterSearch();
    }

    private void ExecuteSearch()
    {
        if (pathfinding.IsPathComplete())
        {
            Vector3 searchTarget;
            if (playerPositionHistory.Count > 0)
            {
                searchTarget = playerPositionHistory[playerPositionHistory.Count - 1];
            }
            else
            {
                searchTarget = lastKnownPosition + Random.insideUnitSphere * stateManager.GetSearchRadius();
            }

            searchTarget.y = transform.position.y;
            pathfinding.GeneratePath(searchTarget);
        }

        if (stateManager.IsSearchExpired())
        {
            searchAttempts++;

            if (searchAttempts < maxSearchAttempts)
            {
                stateManager.ExtendSearchTime(searchTimeExtensionPerAttempt);
            }
            else
            {
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
        if (pathfinding == null)
            return transform.forward;

        Vector3? nextWaypoint = pathfinding.GetNextWaypoint();
        Vector3 target = nextWaypoint ?? (transform.position + transform.forward);
        Vector3 dirToNext = target - transform.position;
        dirToNext.y = 0f;

        return dirToNext.sqrMagnitude > 0.0001f ? dirToNext.normalized : transform.forward;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        Vector3 pos = transform.position;

        if (hearingSystem != null && enableSoundSense)
            hearingSystem.DrawDebugGizmos(pos);

        if (pathfinding != null)
            pathfinding.DrawDebugGizmos();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || killTriggered)
            return;

        TryKillPlayer(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || killTriggered)
            return;

        TryKillPlayer(other);
    }

    private void TryKillPlayer(Collider hitCollider)
    {
        if (!IsPlayerCollider(hitCollider))
            return;

        killTriggered = true;

        PlayerDamageManager damageManager = hitCollider.GetComponentInParent<PlayerDamageManager>();
        if (damageManager == null && player != null)
            damageManager = player.GetComponentInParent<PlayerDamageManager>();

        if (damageManager != null)
        {
            damageManager.KillAndRestart();
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private bool IsPlayerCollider(Collider hitCollider)
    {
        if (hitCollider == null)
            return false;

        if (!string.IsNullOrEmpty(playerTag) && hitCollider.CompareTag(playerTag))
            return true;

        if (player != null && (hitCollider.transform == player || hitCollider.transform.IsChildOf(player)))
            return true;

        return false;
    }

    private void OnGUI()
    {
        if (!showOnScreenDebug || stateManager == null || detectionMeter == null || pathfinding == null)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };

        Rect rect = new Rect(10, 10, 450, 280);
        string s =
            $"AI State: {stateManager.GetCurrentState()}\n" +
            $"Detection: {detectionMeter.GetDetection():F1}/{detectionMeter.GetMaxDetection()}\n" +
            $"LastKnown: {lastKnownPosition}\n" +
            $"Path nodes: {(pathfinding.GetCurrentPath() == null ? 0 : pathfinding.GetCurrentPath().Count)}\n" +
            $"Rooms: {pathfinding.GetDetectedRoomCount()}\n" +
            $"MemoryTimer: {memoryTimer:F1}s\n" +
            $"LookAroundPause: {lookAroundPauseTimer:F1}s\n" +
            $"Position History: {playerPositionHistory.Count}\n" +
            $"Search Attempts: {searchAttempts}";

        GUI.Label(rect, s, style);
    }
}