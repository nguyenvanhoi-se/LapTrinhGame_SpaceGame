using UnityEngine;

/// <summary>Sinh ngôi sao theo chu kỳ tại vị trí ngang ngẫu nhiên.</summary>
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

    /// <summary>Hẹn thời điểm sinh ngôi sao đầu tiên sau một chu kỳ.</summary>
    private void Start()
    {
        nextSpawnTime = Time.time + spawnInterval;
    }

    /// <summary>Sinh sao khi đến hạn rồi hẹn lần tiếp theo theo thời gian game.</summary>
    private void Update()
    {
        if (!GameManager.CanAdvanceGameplay) return;
        if (Time.time < nextSpawnTime)
            return;

        SpawnStar();

        nextSpawnTime =
            Time.time + spawnInterval;
    }

    /// <summary>Kiểm tra prefab rồi tạo sao tại độ cao cố định với hoành độ ngẫu nhiên trong vùng cấu hình.</summary>
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
