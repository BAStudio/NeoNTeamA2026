using UnityEngine;

public class Node
{
    public Vector3Int GridPosition;
    public Vector3 WorldPosition;
    public bool IsWalkable;

    public int GCost;
    public int HCost;
    public int FCost => GCost + HCost;

    public Node Parent;

    public Node(Vector3Int gridPos, Vector3 worldPos, bool walkable)
    {
        GridPosition = gridPos;
        WorldPosition = worldPos;
        IsWalkable = walkable;

        ResetCosts();
    }

    public void ResetCosts()
    {
        GCost = int.MaxValue;
        HCost = 0;
        Parent = null;
    }
}