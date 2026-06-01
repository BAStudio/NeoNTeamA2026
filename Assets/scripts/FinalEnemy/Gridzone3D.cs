using System.Collections.Generic;
using UnityEngine;

public class Grid3DMaster : MonoBehaviour
{
    [SerializeField] private Vector3 gridSize = new Vector3(60, 1, 60);
    [SerializeField] private float nodeSize = 1f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private bool showNodes = false;
    [SerializeField] private Color gizmoColor = Color.green;

    private Node[,,] grid;
    private int xSize, ySize, zSize;

    private readonly List<Node> cachedAllNodes = new List<Node>();
    private readonly List<Node> cachedWalkableNodes = new List<Node>();

    private void Awake()
    {
        CreateGrid();
    }

    public void CreateGrid()
    {
        xSize = Mathf.Max(1, Mathf.RoundToInt(gridSize.x / nodeSize));
        ySize = Mathf.Max(1, Mathf.RoundToInt(gridSize.y / nodeSize));
        zSize = Mathf.Max(1, Mathf.RoundToInt(gridSize.z / nodeSize));

        grid = new Node[xSize, ySize, zSize];
        cachedAllNodes.Clear();
        cachedWalkableNodes.Clear();

        Vector3 origin = transform.position - gridSize / 2f + Vector3.one * (nodeSize * 0.5f);
        int walkableCount = 0;

        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                for (int z = 0; z < zSize; z++)
                {
                    Vector3 worldPos = origin + new Vector3(x * nodeSize, y * nodeSize, z * nodeSize);
                    bool walkable = !Physics.CheckSphere(worldPos, nodeSize * 0.45f, wallLayer);

                    if (walkable)
                        walkableCount++;

                    Node node = new Node(new Vector3Int(x, y, z), worldPos, walkable);
                    grid[x, y, z] = node;
                    cachedAllNodes.Add(node);

                    if (walkable)
                        cachedWalkableNodes.Add(node);
                }
            }
        }

        Debug.Log($"[Grid] Created grid with {xSize * ySize * zSize} nodes, walkable: {walkableCount}");
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
        if (grid == null) return null;

        Vector3 localPos = pos - (transform.position - gridSize / 2f);
        int x = Mathf.Clamp(Mathf.FloorToInt(localPos.x / nodeSize), 0, xSize - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(localPos.y / nodeSize), 0, ySize - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(localPos.z / nodeSize), 0, zSize - 1);
        return grid[x, y, z];
    }

    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();
        if (node == null || grid == null) return neighbors;

        int cx = node.GridPosition.x, cy = node.GridPosition.y, cz = node.GridPosition.z;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0) continue;

                    int nx = cx + dx, ny = cy + dy, nz = cz + dz;
                    if (nx < 0 || ny < 0 || nz < 0 || nx >= xSize || ny >= ySize || nz >= zSize) continue;

                    Node n = grid[nx, ny, nz];
                    if (n != null) neighbors.Add(n);
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

        if (!showNodes || grid == null) return;

        foreach (var n in grid)
        {
            if (n == null) continue;

            Gizmos.color = n.IsWalkable ? new Color(1, 1, 1, 0.08f) : new Color(1, 0, 0, 0.3f);
            Gizmos.DrawCube(n.WorldPosition, Vector3.one * 0.8f * nodeSize);
        }
    }
}