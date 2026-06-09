using System.Collections.Generic;
using UnityEngine;

public class CamSpawnManager : MonoBehaviour
{
    [Header("Spawn Point Tag")]
    public string spawnPointTag = "cam spawn point";

    [Header("Debug")]
    [SerializeField]
    private List<CamSpawnPoint> spawnPoints =
        new List<CamSpawnPoint>();

    private void Awake()
    {
        RefreshSpawnPoints();
    }

    public void RefreshSpawnPoints()
    {
        spawnPoints.Clear();

        GameObject[] foundPoints =
            GameObject.FindGameObjectsWithTag(spawnPointTag);

        foreach (GameObject obj in foundPoints)
        {
            CamSpawnPoint point = obj.GetComponent<CamSpawnPoint>();

            if (point != null)
            {
                spawnPoints.Add(point);
            }
        }

        Debug.Log($"CamSpawnManager found {spawnPoints.Count} spawn points.");
    }

    public Transform GetRandomSpawnPoint()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points found.");
            return null;
        }

        float totalWeight = 0f;

        foreach (CamSpawnPoint point in spawnPoints)
        {
            if (point != null && point.weight > 0f)
            {
                totalWeight += point.weight;
            }
        }

        if (totalWeight <= 0f)
        {
            Debug.LogWarning("All spawn point weights are zero.");
            return null;
        }

        float randomValue = Random.Range(0f, totalWeight);

        float currentWeight = 0f;

        foreach (CamSpawnPoint point in spawnPoints)
        {
            if (point == null || point.weight <= 0f)
                continue;

            currentWeight += point.weight;

            if (randomValue <= currentWeight)
            {
                return point.transform;
            }
        }

        return spawnPoints[0].transform;
    }
}