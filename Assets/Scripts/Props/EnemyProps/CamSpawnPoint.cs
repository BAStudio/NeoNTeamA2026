using UnityEngine;

public class CamSpawnPoint : MonoBehaviour
{
    [Header("Spawn Chance Weight")]
    [Min(0f)]
    public float weight = 1f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
}