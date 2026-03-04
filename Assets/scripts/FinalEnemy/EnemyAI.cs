using System.Collections.Generic;
using UnityEngine;

public class HorrorEnemyAI : MonoBehaviour
{
    private enum AIState { Patrol, Alert, Search, Chase }

    [Header("References")]
    [SerializeField] private Grid3DMaster masterGrid;
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask wallLayer;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float alertSpeed = 2.5f;
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float rotationSpeed = 4f;

    [Header("Vision / FOV")]
    [SerializeField] private float viewRadius = 14f;
    [SerializeField, Range(0, 360)] private float viewAngle = 85f;
    [SerializeField] private float eyeHeight = 1.6f;

    [Header("Multi-ray detection")]
    [SerializeField, Range(1, 11)] private int rayCount = 5;
    [SerializeField] private bool drawVisionRays = true;

    [Header("Detection Meter")]
    [SerializeField] private float detectionBuildSpeed = 60f;
    [SerializeField] private float detectionDecaySpeed = 10f;
    [SerializeField] private float maxDetection = 100f;

    [Header("Hearing")]
    [SerializeField] private float hearingRadius = 8f;
    [SerializeField] private float hearingDetectionBoost = 20f;

    [Header("Search")]
    [SerializeField] private float searchDuration = 6f;
    [SerializeField] private float searchRadius = 4f;
    [SerializeField] private float lookAroundDuration = 10f; 

    [Header("Pathfinding")]
    [SerializeField] private float pathRefreshRate = 0.3f;
    [SerializeField] private float nodeReachThreshold = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;
    [SerializeField] private bool showOnScreenDebug = true;

    // internal
    private AIState currentState = AIState.Patrol;
    private List<Node> allNodes = new List<Node>();
    private List<Vector3> currentPath;
    private int pathIndex;
    private float detectionMeter;
    private float searchTimer;
    private float pathTimer;
    private float alertTimer;
    private float lookAroundTimer;

    private Vector3 lastSeenPosition;
    private Vector3 lastPlayerPosition;
    private Vector3 lastPlayerVelocity;

    private void Start()
    {
        if (masterGrid == null)
        {
            Debug.LogError("[AI] masterGrid missing");
            enabled = false;
            return;
        }

        allNodes.AddRange(masterGrid.GetAllNodes());
        lastPlayerPosition = player != null ? player.position : Vector3.zero;
        PickRandomPatrol();
    }

    private void Update()
    {
        if (player == null) return;

        int rayHits = CastVisionRaysAndCountHits();
        bool seesPlayer = CheckVision();
        bool hearsPlayer = CheckHearing();

        if (hearsPlayer)
        {
            detectionMeter = Mathf.Clamp(detectionMeter + hearingDetectionBoost, 0f, maxDetection);
            lastSeenPosition = player.position;
        }

        UpdateDetectionMeter(seesPlayer);
        UpdateStateBasedOnDetection();

        // Behaviors
        switch (currentState)
        {
            case AIState.Patrol: Patrol(); break;
            case AIState.Alert: Alert(); break;
            case AIState.Chase: Chase(); break;
            case AIState.Search: Search(); break;
        }

        MoveAlongPath();

        // update player velocity smoothing
        Vector3 currentPlayerVelocity = (player.position - lastPlayerPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPlayerVelocity = Vector3.Lerp(lastPlayerVelocity, currentPlayerVelocity, 0.5f);
        lastPlayerPosition = player.position;
    }

    #region Detection / States
    private void UpdateDetectionMeter(bool seesPlayer)
    {
        if (seesPlayer)
            detectionMeter += detectionBuildSpeed * Time.deltaTime;
        else
            detectionMeter -= detectionDecaySpeed * Time.deltaTime;

        detectionMeter = Mathf.Clamp(detectionMeter, 0f, maxDetection);
    }

    private void UpdateStateBasedOnDetection()
    {
        if (detectionMeter >= maxDetection * 0.8f)
        {
            if (currentState != AIState.Chase)
                currentState = AIState.Chase;
        }
        else if (detectionMeter >= maxDetection * 0.4f)
        {
            if (currentState != AIState.Alert)
            {
                currentState = AIState.Alert;
                alertTimer = 2f;
                GeneratePath(lastSeenPosition);
            }
        }
        else
        {
            if (currentState != AIState.Patrol && currentState != AIState.Search)
            {
                currentState = AIState.Patrol;
                PickRandomPatrol();
            }
        }
    }

    private void EnterSearch()
    {
        currentState = AIState.Search;
        searchTimer = searchDuration;
        lookAroundTimer = lookAroundDuration;
        GeneratePath(lastSeenPosition);
    }
    #endregion

    #region Vision & Hearing
    private int CastVisionRaysAndCountHits()
    {
        int hits = 0;
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 centerDir = transform.forward;
        if (currentPath != null && pathIndex < currentPath.Count)
        {
            Vector3 nextNode = currentPath[pathIndex];
            Vector3 dirToNext = nextNode - transform.position;
            dirToNext.y = 0f;
            if (dirToNext.sqrMagnitude > 0.0001f) centerDir = dirToNext.normalized;
        }

        float baseAngle = Mathf.Atan2(centerDir.x, centerDir.z) * Mathf.Rad2Deg;

        for (int i = 0; i < rayCount; i++)
        {
            float t = (rayCount == 1) ? 0.5f : (float)i / (rayCount - 1);
            float angle = Mathf.Lerp(-viewAngle / 2f, viewAngle / 2f, t);
            Vector3 dir = DirFromAngle(baseAngle + angle, true);

            if (Physics.Raycast(eyePos, dir, out RaycastHit hit, viewRadius))
            {
                if (((1 << hit.collider.gameObject.layer) & wallLayer) != 0)
                {
                    if (drawVisionRays) Debug.DrawRay(eyePos, dir * hit.distance, Color.red);
                    continue; // hit wall, block vision
                }

                bool hitPlayer = IsHitPlayer(hit);
                if (drawVisionRays) Debug.DrawRay(eyePos, dir * hit.distance, hitPlayer ? Color.green : Color.red);

                if (hitPlayer) hits++;
            }
            else if (drawVisionRays)
            {
                Debug.DrawRay(eyePos, dir * viewRadius, Color.gray);
            }
        }

        return hits;
    }

    private bool IsHitPlayer(RaycastHit hit)
    {
        if (hit.collider == null) return false;
        if (!string.IsNullOrEmpty(playerTag) && hit.collider.CompareTag(playerTag)) return true;
        if (player != null && (hit.transform == player || hit.transform.IsChildOf(player))) return true;
        return false;
    }

    private bool CheckVision()
    {
        if (player == null) return false;
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPos = player.position + Vector3.up * eyeHeight;
        Vector3 dirToPlayer = targetPos - eyePos;
        float distance = dirToPlayer.magnitude;

        if (distance > viewRadius) return false;
        if (Vector3.Angle(transform.forward, dirToPlayer.normalized) > viewAngle / 2f) return false;

        // Wall blocking
        if (Physics.Linecast(eyePos, targetPos, wallLayer))
            return false;

        return true;
    }

    private bool CheckHearing()
    {
        if (player == null) return false;
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= hearingRadius)
        {
            lastSeenPosition = player.position;
            return true;
        }
        return false;
    }
    #endregion

    #region Behaviors
    private void Patrol()
    {
        if (currentPath == null || pathIndex >= currentPath.Count)
            PickRandomPatrol();
    }

    private void Alert()
    {
        if (currentPath == null || pathIndex >= currentPath.Count)
            GeneratePath(lastSeenPosition);

        alertTimer -= Time.deltaTime;
        if (alertTimer <= 0f)
        {
            if (detectionMeter >= maxDetection * 0.8f)
                currentState = AIState.Chase;
            else
                EnterSearch();
        }
    }

    private void Chase()
    {
        if (player == null) return;
        lastSeenPosition = player.position;

        pathTimer -= Time.deltaTime;
        if (pathTimer <= 0f)
        {
            GeneratePath(player.position);
            pathTimer = pathRefreshRate;
        }
    }

    private void Search()
    {
        if (lookAroundTimer > 0f)
        {
            // rotate in place to "look around"
            transform.Rotate(Vector3.up, 60f * Time.deltaTime);
            lookAroundTimer -= Time.deltaTime;
            return;
        }

        searchTimer -= Time.deltaTime;

        if (currentPath == null || pathIndex >= currentPath.Count)
        {
            Vector3 candidate = lastSeenPosition + Random.insideUnitSphere * searchRadius;
            candidate.y = transform.position.y;
            GeneratePath(candidate);
        }

        if (searchTimer <= 0f)
        {
            detectionMeter = 0f;
            currentState = AIState.Patrol;
            PickRandomPatrol();
        }
    }
    #endregion

    #region Movement & Pathfinding
    private void MoveAlongPath()
    {
        if (currentPath == null || pathIndex >= currentPath.Count) return;

        float speed = patrolSpeed;
        if (currentState == AIState.Alert) speed = alertSpeed;
        if (currentState == AIState.Chase) speed = chaseSpeed;

        Vector3 target = currentPath[pathIndex];
        Vector3 dir = target - transform.position;
        Vector3 moveDir = dir.normalized;

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (moveDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), rotationSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < nodeReachThreshold)
            pathIndex++;
    }

    private void PickRandomPatrol()
    {
        if (masterGrid == null) return;
        for (int i = 0; i < 12; i++)
        {
            Node n = masterGrid.GetRandomWalkableNode();
            if (n == null) continue;
            GeneratePath(n.WorldPosition);
            return;
        }
    }

    private void GeneratePath(Vector3 target)
    {
        var path = AStar(target);
        if (path != null && path.Count > 0)
        {
            currentPath = path;
            pathIndex = 0;
        }
        else
        {
            currentPath = null;
            pathIndex = 0;
        }
    }

    private List<Vector3> AStar(Vector3 targetPos)
    {
        if (masterGrid == null) return null;
        Node start = masterGrid.NodeFromWorld(transform.position);
        Node target = masterGrid.NodeFromWorld(targetPos);
        if (start == null || target == null) return null;

        foreach (var n in allNodes) n.ResetCosts();
        List<Node> openSet = new List<Node> { start };
        HashSet<Node> closedSet = new HashSet<Node>();
        start.GCost = 0;

        while (openSet.Count > 0)
        {
            Node current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < current.FCost || (openSet[i].FCost == current.FCost && openSet[i].HCost < current.HCost))
                    current = openSet[i];
            }

            openSet.Remove(current);
            closedSet.Add(current);

            if (current == target)
                return RetraceToVector3(start, target);

            foreach (var neighbor in masterGrid.GetNeighbors(current))
            {
                if (neighbor == null || !neighbor.IsWalkable || closedSet.Contains(neighbor)) continue;
                int newCost = current.GCost + 10;
                if (newCost < neighbor.GCost)
                {
                    neighbor.GCost = newCost;
                    neighbor.HCost = Mathf.RoundToInt(Vector3.Distance(neighbor.WorldPosition, target.WorldPosition));
                    neighbor.Parent = current;
                    if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                }
            }
        }

        return null;
    }

    private List<Vector3> RetraceToVector3(Node start, Node end)
    {
        List<Vector3> path = new List<Vector3>();
        Node current = end;
        while (current != null && current != start)
        {
            path.Add(current.WorldPosition);
            current = current.Parent;
        }
        path.Reverse();
        return path;
    }

    private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
            angleInDegrees += transform.eulerAngles.y;
        float rad = angleInDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }
    #endregion

    #region Debug
    private void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;

        // Hearing
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, hearingRadius);

        // FOV radius
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(pos, viewRadius);

        // FOV cone
        Vector3 centerDir = transform.forward;
        if (Application.isPlaying && currentPath != null && pathIndex < currentPath.Count)
        {
            Vector3 nextNode = currentPath[pathIndex];
            Vector3 dirToNext = nextNode - transform.position;
            dirToNext.y = 0f;
            if (dirToNext.sqrMagnitude > 0.0001f) centerDir = dirToNext.normalized;
        }

        Vector3 left = Quaternion.AngleAxis(-viewAngle / 2f, Vector3.up) * centerDir;
        Vector3 right = Quaternion.AngleAxis(viewAngle / 2f, Vector3.up) * centerDir;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pos, pos + left * viewRadius);
        Gizmos.DrawLine(pos, pos + right * viewRadius);

        // Path nodes
        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.magenta;
            for (int i = pathIndex; i < currentPath.Count; i++)
            {
                Gizmos.DrawSphere(currentPath[i], 0.1f);
                if (i == pathIndex)
                    Gizmos.DrawLine(pos, currentPath[i]);
                else
                    Gizmos.DrawLine(currentPath[i - 1], currentPath[i]);
            }
        }
    }

    private void OnGUI()
    {
        if (!showOnScreenDebug) return;
        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white } };
        Rect rect = new Rect(10, 10, 400, 200);
        string s = $"AI State: {currentState}\n" +
                   $"Detection: {detectionMeter:F1}/{maxDetection}\n" +
                   $"LastSeen: {lastSeenPosition}\n" +
                   $"Path nodes: {(currentPath == null ? 0 : currentPath.Count)}\n" +
                   $"RayCount: {rayCount}";
        GUI.Label(rect, s, style);
    }
    #endregion
}