using System.Collections.Generic;
using UnityEngine;

public class CamSpawnManager : MonoBehaviour
{
    [Header("Spawn Point Tag")]
    public string spawnPointTag = "cam spawn point";

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

        Debug.Log($"Found {spawnPoints.Count} spawn points.");
    }

    public CamSpawnPoint GetRandomFreeSpawnPoint()
    {
        List<CamSpawnPoint> freePoints =
            new List<CamSpawnPoint>();

        float totalWeight = 0f;

        foreach (CamSpawnPoint point in spawnPoints)
        {
            if (point == null)
                continue;

            if (point.occupied)
                continue;

            if (point.weight <= 0f)
                continue;

            freePoints.Add(point);
            totalWeight += point.weight;
        }

        if (freePoints.Count == 0)
        {
            Debug.LogWarning("No free spawn points available.");
            return null;
        }

        float randomValue = Random.Range(0f, totalWeight);

        float currentWeight = 0f;

        foreach (CamSpawnPoint point in freePoints)
        {
            currentWeight += point.weight;

            if (randomValue <= currentWeight)
            {
                return point;
            }
        }

        return freePoints[0];
    }
}