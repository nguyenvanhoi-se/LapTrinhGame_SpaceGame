using UnityEngine;

public class StarSpawner : MonoBehaviour
{
    [Header("Star")]
    [SerializeField] private GameObject starPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 2f;

    [SerializeField] private float spawnY = 6f;

    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 8f;

    private float nextSpawnTime;

    private void Update()
    {
        if (Time.time < nextSpawnTime)
            return;

        SpawnStar();

        nextSpawnTime =
            Time.time + spawnInterval;
    }

    private void SpawnStar()
    {
        if (starPrefab == null)
        {
            Debug.LogWarning(
                "Chưa gán Star Prefab!"
            );

            return;
        }

        float randomX =
            Random.Range(minX, maxX);

        Vector2 spawnPosition =
            new Vector2(randomX, spawnY);

        Instantiate(
            starPrefab,
            spawnPosition,
            Quaternion.identity
        );
    }
}