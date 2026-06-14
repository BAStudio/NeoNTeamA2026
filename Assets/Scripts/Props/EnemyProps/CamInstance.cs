using UnityEngine;

public class CamInstance : MonoBehaviour
{
    [HideInInspector]
    public CamSpawnPoint spawnPoint;

    private void OnDestroy()
    {
        if (spawnPoint != null)
        {
            spawnPoint.occupied = false;
        }
    }
}