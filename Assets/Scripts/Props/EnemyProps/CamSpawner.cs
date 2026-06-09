using UnityEngine;

public class CamSpawner : MonoBehaviour
{
    [Header("References")]
    public CamSpawnManager spawnManager;

    [Header("Cam Prefabs")]
    public GameObject[] camPrefabs;

    [Header("Spawn Settings")]
    public bool spawnOnStart = true;
    public bool repeatSpawn = true;

    public float spawnInterval = 5f;

    private float timer;

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnCam();
        }
    }

    private void Update()
    {
        if (!repeatSpawn)
            return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnCam();
        }
    }

    public void SpawnCam()
    {
        if (spawnManager == null)
        {
            Debug.LogWarning("Spawn Manager missing.");
            return;
        }

        if (camPrefabs == null || camPrefabs.Length == 0)
        {
            Debug.LogWarning("No cam prefabs assigned.");
            return;
        }

        CamSpawnPoint spawnPoint =
            spawnManager.GetRandomFreeSpawnPoint();

        if (spawnPoint == null)
            return;

        GameObject selectedCam =
            camPrefabs[Random.Range(0, camPrefabs.Length)];

        GameObject spawnedCam = Instantiate(
            selectedCam,
            spawnPoint.transform.position,
            spawnPoint.transform.rotation
        );

        spawnPoint.occupied = true;

        CamInstance camInstance =
            spawnedCam.GetComponent<CamInstance>();

        if (camInstance != null)
        {
            camInstance.spawnPoint = spawnPoint;
        }
    }
}