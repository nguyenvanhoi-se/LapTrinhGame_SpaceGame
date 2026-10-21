using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Thiết lập nút chọn màn theo tiến trình mở khóa của người chơi.</summary>
public class SelectLevelUI : MonoBehaviour
{
    [Header("Level Buttons")]
    [SerializeField] private Button level1Button;
    [SerializeField] private Button level2Button;
    [SerializeField] private Button level3Button;
    [SerializeField] private Button level4Button;

    [Header("Back")]
    [SerializeField] private Button backButton;

    /// <summary>Thiết lập các nút chọn màn và nút quay lại khi mở giao diện.</summary>
    private void Start()
    {
        SetupLevelButtons();
        SetupBackButton();
    }

    /// <summary>Bật nút theo tiến trình mở khóa và gắn từng nút với số màn tương ứng.</summary>
    private void SetupLevelButtons()
    {
        int unlockedLevel = 1;

        if (LevelManager.Instance != null)
        {
            unlockedLevel = LevelManager.Instance.GetUnlockedLevel();
        }

        level1Button.interactable = unlockedLevel >= 1;
        level2Button.interactable = unlockedLevel >= 2;
        level3Button.interactable = unlockedLevel >= 3;
        level4Button.interactable = unlockedLevel >= 4;

        level1Button.onClick.AddListener(() => LoadLevel(1));
        level2Button.onClick.AddListener(() => LoadLevel(2));
        level3Button.onClick.AddListener(() => LoadLevel(3));
        level4Button.onClick.AddListener(() => LoadLevel(4));
    }

    /// <summary>Gắn thao tác quay về menu chính cho nút Back.</summary>
    private void SetupBackButton()
    {
        backButton.onClick.AddListener(BackToMainMenu);
    }

    /// <summary>Kiểm tra bộ quản lý và quyền mở màn trước khi yêu cầu tải.</summary>
    /// <param name="level">Số màn mà người chơi vừa chọn.</param>
    private void LoadLevel(int level)
    {
        if (LevelManager.Instance == null)
        {
            Debug.LogWarning("Chưa có LevelManager!");
            return;
        }

        if (!LevelManager.Instance.IsLevelUnlocked(level))
        {
            Debug.Log("Level " + level + " đang bị khóa!");
            return;
        }

        LevelManager.Instance.LoadLevel(level);
    }

    /// <summary>Tải scene menu chính từ màn hình chọn màn.</summary>
    private void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}