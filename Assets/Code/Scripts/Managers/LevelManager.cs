using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private const string UnlockedLevelKey = "UnlockedLevel";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public int GetUnlockedLevel()
    {
        return PlayerPrefs.GetInt(UnlockedLevelKey, 1);
    }

    public bool IsLevelUnlocked(int level)
    {
        return level <= GetUnlockedLevel();
    }

    public void UnlockLevel(int level)
    {
        int currentUnlockedLevel = GetUnlockedLevel();

        if (level > currentUnlockedLevel)
        {
            PlayerPrefs.SetInt(UnlockedLevelKey, level);
            PlayerPrefs.Save();

            Debug.Log("Đã mở Level " + level);
        }
    }

    public void LoadLevel(int level)
    {
        if (!IsLevelUnlocked(level))
        {
            Debug.Log("Level " + level + " đang bị khóa.");
            return;
        }

        string sceneName = "Level" + level;

        SceneManager.LoadScene(sceneName);
    }

    public void CompleteLevel(int completedLevel)
    {
        int nextLevel = completedLevel + 1;

        if (nextLevel <= 4)
        {
            UnlockLevel(nextLevel);

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetLevelScore();
            }

            LoadLevel(nextLevel);
        }
        else
        {
            Debug.Log("ĐÃ HOÀN THÀNH TẤT CẢ LEVEL!");
        }
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(UnlockedLevelKey);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetAllScore();
        }

        PlayerPrefs.Save();

        Debug.Log("Đã reset tiến trình Level.");
    }
}