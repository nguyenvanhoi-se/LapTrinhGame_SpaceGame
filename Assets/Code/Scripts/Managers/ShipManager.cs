using UnityEngine;

/// <summary>Quản lý danh sách prefab tàu và lưu lựa chọn của người chơi giữa các scene.</summary>
public class ShipManager : MonoBehaviour
{
    public static ShipManager Instance { get; private set; }

    [Header("Available Player Prefabs")]
    [SerializeField] private GameObject[] playerPrefabs;

    private GameObject selectedPlayerPrefab;

    private const string SelectedPlayerKey = "SelectedPlayer";

    /// <summary>Giữ một instance xuyên scene và khôi phục lựa chọn tàu đã lưu.</summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSelectedPlayer();
    }

    /// <summary>Tìm prefab theo tên cấu hình đã lưu; dùng tàu đầu tiên khi chưa chọn hoặc không tìm thấy.</summary>
    private void LoadSelectedPlayer()
    {
        string selectedPlayerName =
            PlayerPrefs.GetString(SelectedPlayerKey, "");

        // Chưa từng chọn tàu
        if (string.IsNullOrEmpty(selectedPlayerName))
        {
            if (playerPrefabs != null &&
                playerPrefabs.Length > 0)
            {
                selectedPlayerPrefab = playerPrefabs[0];

                SaveSelectedPlayer();

                Debug.Log(
                    "Mặc định sử dụng tàu: " +
                    selectedPlayerPrefab.name
                );
            }

            return;
        }

        // Tìm tàu đã chọn
        foreach (GameObject playerPrefab in playerPrefabs)
        {
            if (playerPrefab == null)
                continue;

            PlayerConfig config =
                playerPrefab.GetComponent<PlayerConfig>();

            if (config == null)
                continue;

            if (config.playerName == selectedPlayerName)
            {
                selectedPlayerPrefab = playerPrefab;
                break;
            }
        }

        // Nếu không tìm thấy tàu đã lưu
        // thì dùng tàu đầu tiên
        if (selectedPlayerPrefab == null &&
            playerPrefabs != null &&
            playerPrefabs.Length > 0)
        {
            selectedPlayerPrefab = playerPrefabs[0];

            SaveSelectedPlayer();
        }
    }

    /// <summary>Nhận prefab hợp lệ làm tàu đang chọn và lưu lựa chọn.</summary>
    /// <param name="playerPrefab">Prefab tàu người chơi chọn.</param>
    public void SelectPlayer(GameObject playerPrefab)
    {
        if (playerPrefab == null)
            return;

        selectedPlayerPrefab = playerPrefab;

        SaveSelectedPlayer();

        Debug.Log(
            "Đã chọn tàu: " +
            playerPrefab.name
        );
    }

    /// <summary>Lưu tên trong PlayerConfig của tàu hiện tại vào PlayerPrefs.</summary>
    private void SaveSelectedPlayer()
    {
        if (selectedPlayerPrefab == null)
            return;

        PlayerConfig config =
            selectedPlayerPrefab.GetComponent<PlayerConfig>();

        if (config == null)
        {
            Debug.LogWarning(
                "Player Prefab chưa có PlayerConfig!"
            );
            return;
        }

        PlayerPrefs.SetString(
            SelectedPlayerKey,
            config.playerName
        );

        PlayerPrefs.Save();
    }

    /// <summary>Lấy prefab tàu đang chọn để tạo người chơi.</summary>
    /// <returns>Prefab đã chọn, có thể null nếu chưa có tàu hợp lệ.</returns>
    public GameObject GetSelectedPlayerPrefab()
    {
        return selectedPlayerPrefab;
    }

    /// <summary>Cung cấp mảng prefab tàu đã cấu hình.</summary>
    /// <returns>Mảng prefab hiện có của ShipManager.</returns>
    public GameObject[] GetPlayerPrefabs()
    {
        return playerPrefabs;
    }
}