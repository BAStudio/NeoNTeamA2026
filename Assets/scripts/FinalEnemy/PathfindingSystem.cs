using System.Collections.Generic;
using UnityEngine;

public class PathfindingSystem : MonoBehaviour
{
    [SerializeField] private Grid3DMaster masterGrid;
    [SerializeField] private float pathRefreshRate = 0.3f;
    [SerializeField] private float nodeReachThreshold = 0.2f;
    [SerializeField] private int maxNodeAdvancePerUpdate = 4;
    [Header("Path Quality")]
    [SerializeField] private int wallProximityPenalty = 6;
    [SerializeField] private int turnPenalty = 4;
    [SerializeField] private int sharpTurnExtraPenalty = 6;
    [SerializeField] private bool preventDiagonalCornerCutting = true;
    [Header("Room Patrol")]
    [SerializeField] private int roomSeedBlockedWallThreshold = 3;
    [SerializeField] private int roomMinSeedClusterSize = 2;
    [SerializeField] private bool drawRoomHighlights = true;
    [SerializeField] private float roomNodeDrawSize = 0.35f;

    private List<Vector3> currentPath;
    private int pathIndex;
    private float pathTimer;
    private List<Node> allNodes = new List<Node>();
    private Transform ownerTransform;

    private readonly List<List<Node>> detectedRooms = new List<List<Node>>();
    private readonly Dictionary<Node, int> roomByNode = new Dictionary<Node, int>();

    private void OnEnable()
    {
        ownerTransform = transform;
    }

    private void Start()
    {
        if (masterGrid == null)
            masterGrid = FindObjectOfType<Grid3DMaster>();

        RebuildGridCache();
    }

    public void SetMasterGrid(Grid3DMaster grid)
    {
        masterGrid = grid;
        RebuildGridCache();
    }

    private void RebuildGridCache()
    {
        allNodes.Clear();
        detectedRooms.Clear();
        roomByNode.Clear();

        if (masterGrid == null)
            return;

        allNodes.AddRange(masterGrid.GetAllNodes());
        BuildRoomMap();
    }

    /// <summary>
    /// Gets the current path
    /// </summary>
    public List<Vector3> GetCurrentPath() => currentPath;

    /// <summary>
    /// Gets the current path index
    /// </summary>
    public int GetPathIndex() => pathIndex;

    /// <summary>
    /// Gets the next waypoint in the path
    /// </summary>
    public Vector3? GetNextWaypoint()
    {
        if (currentPath == null || pathIndex >= currentPath.Count)
            return null;
        return currentPath[pathIndex];
    }

    /// <summary>
    /// Checks if path is complete
    /// </summary>
    public bool IsPathComplete() => currentPath == null || pathIndex >= currentPath.Count;

    /// <summary>
    /// Generates a new path to the target
    /// </summary>
    public void GeneratePath(Vector3 targetPos)
    {
        var path = AStar(targetPos);
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

    /// <summary>
    /// Picks a random patrol destination
    /// </summary>
    public void PickRandomPatrol()
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

    /// <summary>
    /// Picks a patrol point in the room where the enemy currently is.
    /// Falls back to random patrol if no room data is available.
    /// </summary>
    public void PickPatrolInCurrentRoom()
    {
        Node current = masterGrid != null ? masterGrid.NodeFromWorld(ownerTransform.position) : null;
        if (current == null || !roomByNode.TryGetValue(current, out int roomId))
        {
            PickRandomPatrol();
            return;
        }

        PickDestinationInsideRoom(roomId);
    }

    /// <summary>
    /// Picks a patrol point in a different room than the current one.
    /// Returns false when no alternate room exists.
    /// </summary>
    public bool PickPatrolInDifferentRoom()
    {
        if (detectedRooms.Count <= 1)
            return false;

        Node current = masterGrid != null ? masterGrid.NodeFromWorld(ownerTransform.position) : null;
        int currentRoomId = -1;
        if (current != null && roomByNode.TryGetValue(current, out int mapped))
            currentRoomId = mapped;

        List<int> candidates = new List<int>();
        for (int i = 0; i < detectedRooms.Count; i++)
        {
            if (i == currentRoomId) continue;
            if (detectedRooms[i].Count == 0) continue;
            candidates.Add(i);
        }

        if (candidates.Count == 0)
            return false;

        int targetRoomId = candidates[Random.Range(0, candidates.Count)];
        PickDestinationInsideRoom(targetRoomId);
        return true;
    }

    public int GetDetectedRoomCount()
    {
        return detectedRooms.Count;
    }

    public int GetRoomIdAtPosition(Vector3 worldPos)
    {
        if (masterGrid == null)
            return -1;

        Node node = masterGrid.NodeFromWorld(worldPos);
        if (node == null)
            return -1;

        return roomByNode.TryGetValue(node, out int roomId) ? roomId : -1;
    }

    /// <summary>
    /// Updates path following - advances to next node when reached
    /// </summary>
    public void UpdatePathProgress()
    {
        if (currentPath == null || pathIndex >= currentPath.Count)
            return;

        int maxAdvance = Mathf.Max(1, maxNodeAdvancePerUpdate);
        int advanced = 0;

        while (pathIndex < currentPath.Count && advanced < maxAdvance)
        {
            Vector3 target = currentPath[pathIndex];
            if (Vector3.Distance(ownerTransform.position, target) >= nodeReachThreshold)
                break;

            pathIndex++;
            advanced++;
        }
    }

    /// <summary>
    /// Updates path refresh timer for chase behavior
    /// </summary>
    public void UpdatePathRefreshTimer()
    {
        pathTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Checks if path needs refresh
    /// </summary>
    public bool ShouldRefreshPath()
    {
        if (pathTimer <= 0f)
        {
            pathTimer = pathRefreshRate;
            return true;
        }
        return false;
    }

    public float GetNodeReachThreshold() => nodeReachThreshold;

    private void BuildRoomMap()
    {
        HashSet<Node> seedNodes = new HashSet<Node>();
        foreach (Node node in allNodes)
        {
            if (node == null || !node.IsWalkable)
                continue;

            // Treat cells with 3+ blocked cardinal sides as enclosed room seeds.
            int blockedSides = CountBlockedCardinalSides(node);
            if (blockedSides >= roomSeedBlockedWallThreshold)
                seedNodes.Add(node);
        }

        HashSet<Node> visitedSeeds = new HashSet<Node>();
        foreach (Node seed in seedNodes)
        {
            if (visitedSeeds.Contains(seed))
                continue;

            List<Node> cluster = new List<Node>();
            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(seed);
            visitedSeeds.Add(seed);

            while (queue.Count > 0)
            {
                Node current = queue.Dequeue();
                cluster.Add(current);

                foreach (Node neighbor in GetCardinalNeighbors(current))
                {
                    if (neighbor == null || !neighbor.IsWalkable)
                        continue;
                    if (!seedNodes.Contains(neighbor) || visitedSeeds.Contains(neighbor))
                        continue;

                    visitedSeeds.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }

            if (cluster.Count >= roomMinSeedClusterSize)
            {
                int roomId = detectedRooms.Count;
                detectedRooms.Add(cluster);
                foreach (Node n in cluster)
                    roomByNode[n] = roomId;
            }
        }
    }

    private int CountBlockedCardinalSides(Node node)
    {
        int blocked = 0;
        List<Node> cardinal = GetCardinalNeighbors(node);

        // Grid edge counts as blocked as well.
        if (cardinal.Count < 4)
            blocked += 4 - cardinal.Count;

        foreach (Node neighbor in cardinal)
        {
            if (neighbor == null || !neighbor.IsWalkable)
                blocked++;
        }

        return blocked;
    }

    private List<Node> GetCardinalNeighbors(Node node)
    {
        List<Node> result = new List<Node>();
        if (masterGrid == null || node == null)
            return result;

        List<Node> allNeighbors = masterGrid.GetNeighbors(node);
        for (int i = 0; i < allNeighbors.Count; i++)
        {
            Node n = allNeighbors[i];
            if (n == null)
                continue;

            Vector3Int delta = n.GridPosition - node.GridPosition;
            int manhattan = Mathf.Abs(delta.x) + Mathf.Abs(delta.y) + Mathf.Abs(delta.z);
            // Cardinal neighbors only: exactly 1-axis step, keep on same Y level.
            if (manhattan == 1 && delta.y == 0)
                result.Add(n);
        }

        return result;
    }

    private void PickDestinationInsideRoom(int roomId)
    {
        if (roomId < 0 || roomId >= detectedRooms.Count)
        {
            PickRandomPatrol();
            return;
        }

        List<Node> roomNodes = detectedRooms[roomId];
        if (roomNodes == null || roomNodes.Count == 0)
        {
            PickRandomPatrol();
            return;
        }

        Node targetNode = roomNodes[Random.Range(0, roomNodes.Count)];
        GeneratePath(targetNode.WorldPosition);
    }

    private List<Vector3> AStar(Vector3 targetPos)
    {
        if (masterGrid == null) return null;
        Node start = masterGrid.NodeFromWorld(ownerTransform.position);
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

            foreach (var neighbor in GetNavigationNeighbors(current))
            {
                if (neighbor == null || !neighbor.IsWalkable || closedSet.Contains(neighbor)) continue;
                int newCost = current.GCost + GetStepCost(current, neighbor);
                if (newCost < neighbor.GCost)
                {
                    neighbor.GCost = newCost;
                    neighbor.HCost = GetHeuristicCost(neighbor, target);
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

    private int GetHeuristicCost(Node from, Node to)
    {
        Vector3Int delta = to.GridPosition - from.GridPosition;
        int dx = Mathf.Abs(delta.x);
        int dz = Mathf.Abs(delta.z);

        int diagonal = Mathf.Min(dx, dz);
        int straight = Mathf.Max(dx, dz) - diagonal;
        return diagonal * 14 + straight * 10;
    }

    private int GetStepCost(Node current, Node neighbor)
    {
        Vector3Int step = neighbor.GridPosition - current.GridPosition;
        bool diagonal = Mathf.Abs(step.x) == 1 && Mathf.Abs(step.z) == 1;

        int cost = diagonal ? 14 : 10;

        // Prefer paths that stay slightly away from walls.
        cost += CountBlockedCardinalSides(neighbor) * wallProximityPenalty;

        // Penalize repeated direction changes so movement feels less robotic.
        if (current.Parent != null)
        {
            Vector3Int prevDir = current.GridPosition - current.Parent.GridPosition;
            Vector2 prevXZ = new Vector2(prevDir.x, prevDir.z).normalized;
            Vector2 nextXZ = new Vector2(step.x, step.z).normalized;

            if (prevXZ.sqrMagnitude > 0.0001f && nextXZ.sqrMagnitude > 0.0001f)
            {
                float dot = Vector2.Dot(prevXZ, nextXZ);
                if (dot < 0.999f)
                {
                    cost += turnPenalty;
                    if (dot < 0.5f)
                        cost += sharpTurnExtraPenalty;
                }
            }
        }

        return cost;
    }

    private List<Node> GetNavigationNeighbors(Node node)
    {
        List<Node> result = new List<Node>();
        List<Node> neighbors = masterGrid.GetNeighbors(node);

        for (int i = 0; i < neighbors.Count; i++)
        {
            Node neighbor = neighbors[i];
            if (neighbor == null || !neighbor.IsWalkable)
                continue;

            Vector3Int delta = neighbor.GridPosition - node.GridPosition;

            // Keep navigation on the same Y layer for stable floor movement.
            if (delta.y != 0)
                continue;

            int dx = Mathf.Abs(delta.x);
            int dz = Mathf.Abs(delta.z);
            if (dx == 0 && dz == 0)
                continue;
            if (dx > 1 || dz > 1)
                continue;

            if (preventDiagonalCornerCutting && dx == 1 && dz == 1)
            {
                if (!HasWalkableOffset(node, neighbors, delta.x, 0) || !HasWalkableOffset(node, neighbors, 0, delta.z))
                    continue;
            }

            result.Add(neighbor);
        }

        return result;
    }

    private bool HasWalkableOffset(Node current, List<Node> neighbors, int dx, int dz)
    {
        for (int i = 0; i < neighbors.Count; i++)
        {
            Node n = neighbors[i];
            if (n == null || !n.IsWalkable)
                continue;

            Vector3Int d = n.GridPosition - current.GridPosition;
            if (d.x == dx && d.y == 0 && d.z == dz)
                return true;
        }

        return false;
    }

    public void DrawDebugGizmos()
    {
        if (drawRoomHighlights)
        {
            for (int i = 0; i < detectedRooms.Count; i++)
            {
                List<Node> room = detectedRooms[i];
                if (room == null || room.Count == 0)
                    continue;

                Color roomColor = GetRoomColor(i);
                Gizmos.color = new Color(roomColor.r, roomColor.g, roomColor.b, 0.28f);
                for (int j = 0; j < room.Count; j++)
                {
                    Gizmos.DrawCube(room[j].WorldPosition, Vector3.one * roomNodeDrawSize);
                }

                Gizmos.color = new Color(roomColor.r, roomColor.g, roomColor.b, 0.9f);
                Vector3 center = Vector3.zero;
                for (int j = 0; j < room.Count; j++)
                    center += room[j].WorldPosition;
                center /= room.Count;
                Gizmos.DrawSphere(center, 0.15f);
            }
        }

        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.magenta;
            for (int i = pathIndex; i < currentPath.Count; i++)
            {
                Gizmos.DrawSphere(currentPath[i], 0.1f);
                if (i == pathIndex)
                    Gizmos.DrawLine(ownerTransform.position, currentPath[i]);
                else
                    Gizmos.DrawLine(currentPath[i - 1], currentPath[i]);
            }
        }
    }

    private Color GetRoomColor(int roomId)
    {
        // Deterministic hue per room for stable visualization across frames.
        float hue = Mathf.Repeat(roomId * 0.173f, 1f);
        return Color.HSVToRGB(hue, 0.8f, 1f);
    }
}
