using UnityEngine;

public class ShipManager : MonoBehaviour
{
    public static ShipManager Instance { get; private set; }

    [Header("Available Player Prefabs")]
    [SerializeField] private GameObject[] playerPrefabs;

    private GameObject selectedPlayerPrefab;

    private const string SelectedPlayerKey = "SelectedPlayer";

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

    public GameObject GetSelectedPlayerPrefab()
    {
        return selectedPlayerPrefab;
    }

    public GameObject[] GetPlayerPrefabs()
    {
        return playerPrefabs;
    }
}