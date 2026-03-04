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
        Vector3 origin = transform.position - gridSize / 2f + Vector3.one * (nodeSize * 0.5f);

        int walkableCount = 0;

        for (int x = 0; x < xSize; x++)
            for (int y = 0; y < ySize; y++)
                for (int z = 0; z < zSize; z++)
                {
                    Vector3 worldPos = origin + new Vector3(x * nodeSize, y * nodeSize, z * nodeSize);
                    bool walkable = !Physics.CheckSphere(worldPos, nodeSize * 0.45f, wallLayer);
                    if (walkable) walkableCount++;
                    grid[x, y, z] = new Node(new Vector3Int(x, y, z), worldPos, walkable);
                }

        Debug.Log($"[Grid] Created grid with {xSize * ySize * zSize} nodes, walkable: {walkableCount}");
    }

    public List<Node> GetAllNodes()
    {
        List<Node> nodes = new List<Node>();
        foreach (var n in grid)
            if (n != null) nodes.Add(n);
        return nodes;
    }

    public Node GetRandomWalkableNode()
    {
        List<Node> walkable = new List<Node>();
        foreach (var n in grid)
            if (n != null && n.IsWalkable) walkable.Add(n);

        if (walkable.Count == 0) return null;
        return walkable[Random.Range(0, walkable.Count)];
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
            for (int dy = -1; dy <= 1; dy++)
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0) continue;
                    int nx = cx + dx, ny = cy + dy, nz = cz + dz;
                    if (nx < 0 || ny < 0 || nz < 0 || nx >= xSize || ny >= ySize || nz >= zSize) continue;
                    Node n = grid[nx, ny, nz];
                    if (n != null) neighbors.Add(n);
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