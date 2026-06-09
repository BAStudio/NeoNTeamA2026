using System.Collections.Generic;
using UnityEngine;

public class Grid3DMaster : MonoBehaviour
{
    [SerializeField] private Vector3 gridSize = new Vector3(60, 1, 60);
    [SerializeField] private float nodeSize = 1f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private bool showNodes = false;
    [SerializeField] private Color gizmoColor = Color.green;

    [Header("Door Detection")]
    [SerializeField] private string doorTag = "Door";

    private Node[,,] grid;
    private int xSize, ySize, zSize;

    private readonly List<Node> cachedAllNodes = new List<Node>();
    private readonly List<Node> cachedWalkableNodes = new List<Node>();
    private readonly Dictionary<Door, List<Node>> doorNodeMap = new Dictionary<Door, List<Node>>();

    private void OnEnable()
    {
        Door.DoorStateChanged += HandleDoorStateChanged;
    }

    private void OnDisable()
    {
        Door.DoorStateChanged -= HandleDoorStateChanged;
    }

    private void Awake()
    {
        CreateGrid();
    }

    private void HandleDoorStateChanged(Door door)
    {
        RefreshDoorNodes(door);
        RebuildWalkableCache();
    }

    public void CreateGrid()
    {
        xSize = Mathf.Max(1, Mathf.RoundToInt(gridSize.x / nodeSize));
        ySize = Mathf.Max(1, Mathf.RoundToInt(gridSize.y / nodeSize));
        zSize = Mathf.Max(1, Mathf.RoundToInt(gridSize.z / nodeSize));

        grid = new Node[xSize, ySize, zSize];
        cachedAllNodes.Clear();
        cachedWalkableNodes.Clear();
        doorNodeMap.Clear();

        Vector3 origin = transform.position - gridSize / 2f + Vector3.one * (nodeSize * 0.5f);
        int walkableCount = 0;

        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                for (int z = 0; z < zSize; z++)
                {
                    Vector3 worldPos = origin + new Vector3(x * nodeSize, y * nodeSize, z * nodeSize);

                    Node node = CreateNodeAt(worldPos, x, y, z);
                    if (node.IsWalkable)
                        walkableCount++;

                    grid[x, y, z] = node;
                    cachedAllNodes.Add(node);

                    if (node.IsWalkable)
                        cachedWalkableNodes.Add(node);
                }
            }
        }

        Debug.Log($"[Grid] Created grid with {xSize * ySize * zSize} nodes, walkable: {walkableCount}");
    }

    private Node CreateNodeAt(Vector3 worldPos, int x, int y, int z)
    {
        Door door = FindDoorAtPosition(worldPos);

        if (door != null)
        {
            Node node = new Node(new Vector3Int(x, y, z), worldPos, !door.IsLocked, NodeType.Door, door);

            if (!doorNodeMap.TryGetValue(door, out List<Node> nodes))
            {
                nodes = new List<Node>();
                doorNodeMap.Add(door, nodes);
            }

            nodes.Add(node);
            return node;
        }

        bool walkable = !Physics.CheckSphere(worldPos, nodeSize * 0.45f, wallLayer, QueryTriggerInteraction.Ignore);
        NodeType type = walkable ? NodeType.Floor : NodeType.Wall;
        return new Node(new Vector3Int(x, y, z), worldPos, walkable, type);
    }

    private Door FindDoorAtPosition(Vector3 worldPos)
    {
        Collider[] overlaps = Physics.OverlapSphere(
            worldPos,
            nodeSize * 0.45f,
            ~0,
            QueryTriggerInteraction.Collide);

        if (overlaps == null || overlaps.Length == 0)
            return null;

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider col = overlaps[i];
            if (col == null)
                continue;

            Door door = col.GetComponentInParent<Door>();
            if (door != null)
                return door;

            if (!string.IsNullOrWhiteSpace(doorTag) && col.CompareTag(doorTag))
            {
                door = col.GetComponentInParent<Door>();
                if (door != null)
                    return door;
            }
        }

        return null;
    }

    private void RefreshDoorNodes(Door door)
    {
        if (door == null)
            return;

        if (!doorNodeMap.TryGetValue(door, out List<Node> nodes) || nodes == null)
            return;

        for (int i = 0; i < nodes.Count; i++)
        {
            Node node = nodes[i];
            if (node != null)
                node.RefreshFromDoor();
        }
    }

    private void RebuildWalkableCache()
    {
        cachedWalkableNodes.Clear();

        if (cachedAllNodes == null)
            return;

        for (int i = 0; i < cachedAllNodes.Count; i++)
        {
            Node node = cachedAllNodes[i];
            if (node != null && node.IsWalkable)
                cachedWalkableNodes.Add(node);
        }
    }

    public List<Node> GetAllNodes()
    {
        return new List<Node>(cachedAllNodes);
    }

    public Node GetRandomWalkableNode()
    {
        if (cachedWalkableNodes.Count == 0)
            return null;

        return cachedWalkableNodes[Random.Range(0, cachedWalkableNodes.Count)];
    }

    public Node GetNearestWalkableNode(Vector3 pos)
    {
        if (cachedWalkableNodes.Count == 0)
            return null;

        Node nearest = cachedWalkableNodes[0];
        float bestSqr = (nearest.WorldPosition - pos).sqrMagnitude;

        for (int i = 1; i < cachedWalkableNodes.Count; i++)
        {
            Node node = cachedWalkableNodes[i];
            float sqr = (node.WorldPosition - pos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearest = node;
            }
        }

        return nearest;
    }

    public Vector3 ClampToGridBounds(Vector3 pos)
    {
        Vector3 halfSize = gridSize * 0.5f;
        Vector3 min = transform.position - halfSize;
        Vector3 max = transform.position + halfSize;

        pos.x = Mathf.Clamp(pos.x, min.x, max.x);
        pos.y = Mathf.Clamp(pos.y, min.y, max.y);
        pos.z = Mathf.Clamp(pos.z, min.z, max.z);
        return pos;
    }

    public Node NodeFromWorld(Vector3 pos)
    {
        if (grid == null)
            return null;

        Vector3 localPos = pos - (transform.position - gridSize / 2f);
        int x = Mathf.Clamp(Mathf.FloorToInt(localPos.x / nodeSize), 0, xSize - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(localPos.y / nodeSize), 0, ySize - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(localPos.z / nodeSize), 0, zSize - 1);

        return grid[x, y, z];
    }

    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();
        if (node == null || grid == null)
            return neighbors;

        int cx = node.GridPosition.x;
        int cy = node.GridPosition.y;
        int cz = node.GridPosition.z;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0)
                        continue;

                    int nx = cx + dx;
                    int ny = cy + dy;
                    int nz = cz + dz;

                    if (nx < 0 || ny < 0 || nz < 0 || nx >= xSize || ny >= ySize || nz >= zSize)
                        continue;

                    Node n = grid[nx, ny, nz];
                    if (n != null)
                        neighbors.Add(n);
                }
            }
        }

        return neighbors;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
        Gizmos.DrawCube(transform.position, gridSize);
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position, gridSize);

        if (!showNodes || grid == null)
            return;

        foreach (var n in grid)
        {
            if (n == null)
                continue;

            Gizmos.color = GetNodeColor(n);
            Gizmos.DrawCube(n.WorldPosition, Vector3.one * 0.8f * nodeSize);
        }
    }

    private Color GetNodeColor(Node node)
    {
        if (node.Type == NodeType.Door)
        {
            return node.IsWalkable
                ? new Color(0.2f, 0.9f, 1f, 0.45f)
                : new Color(1f, 0.65f, 0.1f, 0.45f);
        }

        if (node.IsWalkable)
            return new Color(1f, 1f, 1f, 0.08f);

        return new Color(1f, 0f, 0f, 0.3f);
    }
}