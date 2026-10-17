using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    [Header("Asteroid Prefabs")]
    [SerializeField] private GameObject smallAsteroidPrefab;
    [SerializeField] private GameObject mediumAsteroidPrefab;
    [SerializeField] private GameObject largeAsteroidPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 1.5f;

    [SerializeField] private float spawnY = 6f;

    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 8f;

    private float nextSpawnTime;

    private void Update()
    {
        if (Time.time < nextSpawnTime)
            return;

        SpawnRandomAsteroid();

        nextSpawnTime =
            Time.time + spawnInterval;
    }

    private void SpawnRandomAsteroid()
    {
        GameObject prefab =
            GetRandomAsteroidPrefab();

        if (prefab == null)
        {
            Debug.LogWarning(
                "Chưa gán Asteroid Prefab!"
            );

            return;
        }

        float randomX =
            Random.Range(minX, maxX);

        Vector2 spawnPosition =
            new Vector2(randomX, spawnY);

        Instantiate(
            prefab,
            spawnPosition,
            Quaternion.identity
        );
    }

    private GameObject GetRandomAsteroidPrefab()
    {
        int randomIndex =
            Random.Range(0, 3);

        switch (randomIndex)
        {
            case 0:
                return smallAsteroidPrefab;

            case 1:
                return mediumAsteroidPrefab;

            case 2:
                return largeAsteroidPrefab;

            default:
                return null;
        }
    }
}