using UnityEngine;

[CreateAssetMenu(menuName = "AI/Grid Zone")]
public class GridZoneData : ScriptableObject
{
    public Vector3 gridSize = new Vector3(30, 5, 30);
    public float nodeSize = 1f;
}