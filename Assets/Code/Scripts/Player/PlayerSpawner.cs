using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;

    private void Start()
    {
        SpawnPlayer();
    }

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