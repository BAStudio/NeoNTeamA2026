using UnityEngine;

public class CamSpawnPoint : MonoBehaviour
{
    [Header("Spawn Chance Weight")]
    [Min(0f)]
    public float weight = 1f;

    public bool occupied = false;

    private void OnDrawGizmos()
    {
        Gizmos.color = occupied ? Color.red : Color.green;
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
}