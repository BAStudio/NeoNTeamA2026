using System.Collections.Generic;
using UnityEngine;

public class HorrorEnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid3DMaster masterGrid;
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask wallLayer;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float chaseSpeed = 6f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 25f;
    [SerializeField] private float closeStopDistance = 3f;
    [SerializeField] private float chaseDuration = 6f;

    [Header("Roaming")]
    [SerializeField] private float scoutInterval = 2f;
    [SerializeField] private float minScoutDistance = 5f;
    [SerializeField] private float nodeReachThreshold = 0.2f;

    [Header("Raycast Detection")]
    [SerializeField] private float minChaseDistance = 5f;
    [SerializeField] private float raycastHeight = 1.5f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = true;

    private List<Node> allNodes = new List<Node>();
    private List<Vector3> currentPath;
    private int pathIndex;
    private bool chasing;
    private float chaseTimer;
    private float scoutTimer;

    private void Start()
    {
        if (masterGrid == null)
        {
            Debug.LogError("[AI] masterGrid missing");
            enabled = false;
            return;
        }

        allNodes.AddRange(masterGrid.GetAllNodes());

        if (allNodes.Count == 0)
            Debug.LogWarning("[AI] No nodes found in masterGrid!");

        scoutTimer = 0f;
        PickRandomScoutPoint();
    }

    private void Update()
    {
        if (player == null)
        {
            if (verboseLogs) Debug.LogWarning("[AI] Player not assigned");
            return;
        }

        if (CanSeePlayer())
        {
            if (!chasing && verboseLogs) Debug.Log("[AI] Player detected — chase start");
            chasing = true;
            chaseTimer = chaseDuration;
        }

        if (chasing)
        {
            ChasePlayer();
            chaseTimer -= Time.deltaTime;
            if (chaseTimer <= 0f)
            {
                chasing = false;
                if (verboseLogs) Debug.Log("[AI] Chase ended — returning to patrol");

                currentPath = null;
                pathIndex = 0;
                scoutTimer = 0f; // forces immediate scout pick
                PickRandomScoutPoint();
            }
        }
        else
        {
            ScoutRoam();
        }

        MoveAlongPath();
    }

    private void ScoutRoam()
    {
        scoutTimer -= Time.deltaTime;
        if (scoutTimer <= 0f)
        {
            PickRandomScoutPoint();
            scoutTimer = scoutInterval;
        }

        if (currentPath == null || pathIndex >= currentPath.Count)
            PickRandomScoutPoint();
    }

    private void PickRandomScoutPoint()
    {
        if (masterGrid == null || allNodes.Count == 0) return;

        for (int attempt = 0; attempt < 12; attempt++)
        {
            Node candidate = masterGrid.GetRandomWalkableNode();
            if (candidate == null) continue;

            float dist = Vector3.Distance(transform.position, candidate.WorldPosition);
            if (dist < minScoutDistance) continue;

            var path = AStar(candidate.WorldPosition);
            if (path != null && path.Count > 0)
            {
                currentPath = path;
                pathIndex = 0;
                if (verboseLogs) Debug.Log($"[AI] New scout path ({path.Count} nodes)");
                return;
            }
        }
        if (verboseLogs) Debug.LogWarning("[AI] Failed to find valid scout point with path");
    }

    private void ChasePlayer()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance < closeStopDistance && Physics.Linecast(transform.position, player.position, wallLayer))
        {
            if (verboseLogs) Debug.Log("[AI] Wall blocks close player — stop");
            return;
        }

        GeneratePath(player.position);
    }

    private void GeneratePath(Vector3 target)
    {
        var path = AStar(target);
        if (path != null && path.Count > 0)
        {
            currentPath = path;
            pathIndex = 0;
            if (verboseLogs) Debug.Log($"[AI] Path generated ({path.Count} nodes)");
        }
        else if (verboseLogs) Debug.LogWarning("[AI] A* failed to generate path");
    }

    // --- Forward-based raycast detection ---
    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 origin = transform.position + transform.forward * 0.5f + Vector3.up * raycastHeight;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, detectionRange))
        {
            float distToPlayer = Vector3.Distance(transform.position, player.position);
            if (hit.transform == player && distToPlayer <= minChaseDistance)
            {
                if (verboseLogs) Debug.DrawRay(origin, direction * detectionRange, Color.green, 0.1f);
                return true;
            }
        }

        if (verboseLogs) Debug.DrawRay(origin, direction * detectionRange, Color.red, 0.1f);

        return false;
    }

    private List<Vector3> AStar(Vector3 targetPos)
    {
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
                if (openSet[i].FCost < current.FCost ||
                    (openSet[i].FCost == current.FCost && openSet[i].HCost < current.HCost))
                    current = openSet[i];
            }

            openSet.Remove(current);
            closedSet.Add(current);

            if (current == target)
                return RetraceToVector3(start, target);

            foreach (var neighbor in masterGrid.GetNeighbors(current))
            {
                if (neighbor == null || !neighbor.IsWalkable || closedSet.Contains(neighbor)) continue;

                int newCost = current.GCost + GetDistance(current, neighbor);
                if (newCost < neighbor.GCost)
                {
                    neighbor.GCost = newCost;
                    neighbor.HCost = Mathf.RoundToInt(
                        Mathf.Abs(neighbor.WorldPosition.x - target.WorldPosition.x) +
                        Mathf.Abs(neighbor.WorldPosition.y - target.WorldPosition.y) +
                        Mathf.Abs(neighbor.WorldPosition.z - target.WorldPosition.z)
                    );
                    neighbor.Parent = current;
                    if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                }
            }
        }

        return null;
    }

    private int GetDistance(Node a, Node b)
    {
        int dx = Mathf.Abs(a.GridPosition.x - b.GridPosition.x);
        int dy = Mathf.Abs(a.GridPosition.y - b.GridPosition.y);
        int dz = Mathf.Abs(a.GridPosition.z - b.GridPosition.z);

        int min = Mathf.Min(dx, Mathf.Min(dy, dz));
        int max = Mathf.Max(dx, Mathf.Max(dy, dz));
        int mid = dx + dy + dz - min - max;

        return 17 * min + 14 * (mid - min) + 10 * (max - mid);
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

    private void MoveAlongPath()
    {
        if (currentPath == null || pathIndex >= currentPath.Count) return;

        float speed = chasing ? chaseSpeed : moveSpeed;
        Vector3 target = currentPath[pathIndex];
        Vector3 moveDir = (target - transform.position).normalized;

        // Move
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        // Rotate toward movement direction
        if (moveDir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, target) < nodeReachThreshold)
            pathIndex++;
    }
}