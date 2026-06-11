using UnityEngine;

public class Node
{
    public Vector3Int GridPosition;
    public Vector3 WorldPosition;

    public bool IsWalkable;
    public NodeType Type;

    public Door LinkedDoor;

    public int GCost;
    public int HCost;
    public int FCost => GCost + HCost;

    public Node Parent;

    public Node(
        Vector3Int gridPos,
        Vector3 worldPos,
        bool walkable,
        NodeType type = NodeType.Floor,
        Door linkedDoor = null)
    {
        GridPosition = gridPos;
        WorldPosition = worldPos;
        IsWalkable = walkable;
        Type = type;
        LinkedDoor = linkedDoor;
        ResetCosts();
    }

    public void ResetCosts()
    {
        GCost = int.MaxValue;
        HCost = 0;
        Parent = null;
    }

    public void RefreshFromDoor()
    {
        if (LinkedDoor == null)
            return;

        IsWalkable = !LinkedDoor.IsLocked;
        Type = NodeType.Door;
    }
}