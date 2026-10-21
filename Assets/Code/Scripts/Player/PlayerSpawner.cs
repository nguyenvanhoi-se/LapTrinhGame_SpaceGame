using UnityEngine;

/// <summary>Tạo tàu đã chọn tại điểm xuất hiện của màn chơi.</summary>
public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;

    /// <summary>Yêu cầu tạo tàu đã chọn khi bắt đầu scene.</summary>
    private void Start()
    {
        SpawnPlayer();
    }

    /// <summary>Kiểm tra bộ quản lý, prefab và điểm xuất hiện rồi tạo tàu tại vị trí, góc quay đã đặt.</summary>
    private void SpawnPlayer()
    {
        if (ShipManager.Instance == null)
        {
            Debug.LogError(
                "PlayerSpawner: Không tìm thấy ShipManager!"
            );
            return;
        }

        GameObject playerPrefab =
            ShipManager.Instance.GetSelectedPlayerPrefab();

        if (playerPrefab == null)
        {
            Debug.LogError(
                "PlayerSpawner: Chưa có Player được chọn!"
            );
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError(
                "PlayerSpawner: Chưa gán Spawn Point!"
            );
            return;
        }

        Instantiate(
            playerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        Debug.Log(
            "Đã spawn: " +
            playerPrefab.name
        );
    }
}