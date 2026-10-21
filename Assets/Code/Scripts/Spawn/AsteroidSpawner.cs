using System.Collections.Generic;
using UnityEngine;

/// <summary>Sinh thiên thạch theo trọng số, giới hạn số lượng và tránh các đợt rơi quá sát nhau.</summary>
public class AsteroidSpawner : MonoBehaviour
{
    [Header("Asteroid Prefabs")]
    [SerializeField] private GameObject smallAsteroidPrefab;
    [SerializeField] private GameObject mediumAsteroidPrefab;
    [SerializeField] private GameObject largeAsteroidPrefab;

    [Header("Level Difficulty")]
    [SerializeField, Min(0.1f)] private float spawnInterval = 2.5f;
    [SerializeField, Min(0.1f)] private float speedMultiplier = 1f;
    [SerializeField, Min(1)] private int maxActiveAsteroids = 5;

    [Header("Type Weights (prefab stats are unchanged)")]
    [SerializeField, Min(0f)] private float smallWeight = 9f;
    [SerializeField, Min(0f)] private float mediumWeight = 1f;
    [SerializeField, Min(0f)] private float largeWeight = .25f;

    [Header("Spawn Area / Safety")]
    [SerializeField] private float spawnY = 6f;

    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 8f;
    [SerializeField, Min(0f)] private float edgePadding = 0.5f;
    [SerializeField, Min(0f)] private float minimumSpawnSeparation = 1.2f;
    [SerializeField, Min(0f)] private float minimumBottomArrivalGap = 0.75f;

    private readonly List<AsteroidController> activeAsteroids = new List<AsteroidController>();
    private Camera mainCamera;
    private float nextSpawnTime;

    public float SpawnInterval => spawnInterval;
    public float SpeedMultiplier => speedMultiplier;
    public int MaxActiveAsteroids => maxActiveAsteroids;
    /// <summary>Loại thiên thạch đã bị hủy hoặc ẩn khỏi danh sách rồi trả về số còn hoạt động.</summary>
    public int ActiveCount
    {
        get
        {
            activeAsteroids.RemoveAll(a => a == null || !a.gameObject.activeInHierarchy);
            return activeAsteroids.Count;
        }
    }

    /// <summary>Lấy camera và hẹn đợt sinh đầu tiên sau một chu kỳ.</summary>
    private void Start()
    {
        mainCamera = Camera.main;
        nextSpawnTime = Time.time + spawnInterval;
    }

    /// <summary>Đến kỳ thì thử tối đa 12 lần chọn loại và vị trí an toàn; bỏ kỳ nếu đã đủ số lượng hoặc không tìm được chỗ.</summary>
    private void Update()
    {
        if (!GameManager.CanAdvanceGameplay || Time.time < nextSpawnTime) return;

        // Hẹn từ thời điểm hiện tại để không sinh bù dồn dập sau khi tạm dừng hoặc đầy thiên thạch.
        nextSpawnTime = Time.time + spawnInterval;
        if (ActiveCount >= maxActiveAsteroids) return;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            GameObject prefab = GetRandomAsteroidPrefab();
            if (prefab == null) return;
            var controller = prefab.GetComponent<AsteroidController>();
            if (controller == null) return;
            if (!TryGetSpawnPosition(prefab, controller, out Vector2 position)) continue;

            var asteroid = Instantiate(prefab, position, Quaternion.identity).GetComponent<AsteroidController>();
            asteroid.ConfigureSpeed(speedMultiplier);
            activeAsteroids.Add(asteroid);
            return;
        }
        // Chưa tìm được chỗ phù hợp thì bỏ đợt này để tránh dồn nhiều thiên thạch.
    }

    /// <summary>Tính vị trí sinh phía trên vùng nhìn, chừa mép theo kích thước và kiểm tra khoảng cách, thời điểm chạm đáy với các thiên thạch khác.</summary>
    /// <param name="prefab">Prefab dùng để đo kích thước hiển thị.</param>
    /// <param name="controller">Cấu hình tốc độ gốc của loại thiên thạch.</param>
    /// <param name="position">Nhận vị trí ứng viên đã tính.</param>
    /// <returns>True nếu vị trí và khoảng thời gian rơi đều đạt điều kiện an toàn.</returns>
    private bool TryGetSpawnPosition(GameObject prefab, AsteroidController controller, out Vector2 position)
    {
        var sprite = prefab.GetComponent<SpriteRenderer>();
        // Tính nửa kích thước sprite sau khi nhân tỉ lệ, kể cả khi prefab bị lật bằng scale âm.
        Vector3 extent = sprite != null && sprite.sprite != null
            ? Vector3.Scale(sprite.sprite.bounds.extents, new Vector3(Mathf.Abs(prefab.transform.localScale.x), Mathf.Abs(prefab.transform.localScale.y), 1))
            : Vector3.one * .5f;
        float left = minX, right = maxX, y = spawnY;
        float bottom = -spawnY;
        if (mainCamera != null && mainCamera.orthographic)
        {
            float halfHeight = mainCamera.orthographicSize;
            float halfWidth = halfHeight * mainCamera.aspect;
            left = Mathf.Max(left, mainCamera.transform.position.x - halfWidth + extent.x + edgePadding);
            right = Mathf.Min(right, mainCamera.transform.position.x + halfWidth - extent.x - edgePadding);
            y = Mathf.Max(y, mainCamera.transform.position.y + halfHeight + extent.y + .25f);
            bottom = mainCamera.transform.position.y - halfHeight;
        }
        position = new Vector2(Random.Range(left, Mathf.Max(left, right)), y);
        if (left > right) return false;
        // Ước lượng số giây còn lại đến đáy bằng quãng đường chia tốc độ rơi.
        float arrival = (y - bottom) / Mathf.Max(.1f, controller.BaseMoveSpeed * speedMultiplier);
        foreach (var other in activeAsteroids)
        {
            if (Mathf.Abs(other.transform.position.x - position.x) < minimumSpawnSeparation &&
                Mathf.Abs(other.transform.position.y - y) < 2f) return false;
            float otherArrival = (other.transform.position.y - bottom) / Mathf.Max(.1f, other.CurrentMoveSpeed);
            // Giãn thời điểm chạm đáy trên toàn màn hình để người chơi có thời gian xử lý từng đợt.
            if (Mathf.Abs(arrival - otherArrival) < minimumBottomArrivalGap) return false;
        }
        return true;
    }

    /// <summary>Chọn một prefab theo trọng số không âm, bỏ qua các loại chưa được gán.</summary>
    /// <returns>Prefab được chọn, hoặc null nếu tổng trọng số hợp lệ bằng 0.</returns>
    private GameObject GetRandomAsteroidPrefab()
    {
        float small = smallAsteroidPrefab != null ? Mathf.Max(0, smallWeight) : 0;
        float medium = mediumAsteroidPrefab != null ? Mathf.Max(0, mediumWeight) : 0;
        float large = largeAsteroidPrefab != null ? Mathf.Max(0, largeWeight) : 0;
        float total = small + medium + large;
        if (total <= 0) return null;
        // Chia khoảng ngẫu nhiên thành ba đoạn có độ dài theo trọng số của từng loại.
        float roll = Random.value * total;
        if (roll < small) return smallAsteroidPrefab;
        if (roll < small + medium) return mediumAsteroidPrefab;
        return largeAsteroidPrefab;
    }
}
