using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectLevelUI : MonoBehaviour
{
    [Header("Level Buttons")]
    [SerializeField] private Button level1Button;
    [SerializeField] private Button level2Button;
    [SerializeField] private Button level3Button;
    [SerializeField] private Button level4Button;

    [Header("Back")]
    [SerializeField] private Button backButton;

    private void Start()
    {
        SetupLevelButtons();
        SetupBackButton();
    }

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

    private void SetupBackButton()
    {
        backButton.onClick.AddListener(BackToMainMenu);
    }

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

    private void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}