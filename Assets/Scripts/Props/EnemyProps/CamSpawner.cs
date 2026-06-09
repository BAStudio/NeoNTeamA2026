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

    [Header("Limits")]
    public int maxSpawnedCams = 10;

    private int currentSpawned;
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

        if (currentSpawned >= maxSpawnedCams)
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

        Transform spawnPoint = spawnManager.GetRandomSpawnPoint();

        if (spawnPoint == null)
            return;

        GameObject selectedCam =
            camPrefabs[Random.Range(0, camPrefabs.Length)];

        Instantiate(
            selectedCam,
            spawnPoint.position,
            spawnPoint.rotation
        );

        currentSpawned++;
    }
}