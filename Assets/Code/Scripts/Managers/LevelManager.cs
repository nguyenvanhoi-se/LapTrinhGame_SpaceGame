using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Quản lý mở khóa, chuyển màn và lưu tiến trình bằng PlayerPrefs.</summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private const string UnlockedLevelKey = "UnlockedLevel";

    /// <summary>Giữ một bộ quản lý tiến trình duy nhất xuyên các scene.</summary>
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

    /// <summary>Đọc màn cao nhất đã mở từ PlayerPrefs, mặc định mở màn 1.</summary>
    /// <returns>Số màn cao nhất đã mở khóa.</returns>
    public int GetUnlockedLevel()
    {
        return PlayerPrefs.GetInt(UnlockedLevelKey, 1);
    }

    /// <summary>So sánh màn yêu cầu với màn cao nhất đã mở.</summary>
    /// <param name="level">Số màn cần kiểm tra.</param>
    /// <returns>True nếu số màn không vượt tiến trình đã lưu.</returns>
    public bool IsLevelUnlocked(int level)
    {
        return level <= GetUnlockedLevel();
    }

    /// <summary>Chỉ nâng và lưu tiến trình khi màn yêu cầu cao hơn màn đã mở.</summary>
    /// <param name="level">Số màn muốn mở khóa.</param>
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

    /// <summary>Kiểm tra quyền mở màn, khôi phục thời gian rồi tải scene theo số màn.</summary>
    /// <param name="level">Số màn muốn tải.</param>
    public void LoadLevel(int level)
    {
        if (!IsLevelUnlocked(level))
        {
            Debug.Log("Level " + level + " đang bị khóa.");
            return;
        }

        string sceneName = "Level" + level;

        if (GameManager.Instance != null)
            GameManager.Instance.PrepareForSceneChange();
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>Mở màn kế tiếp, xóa điểm màn và tải màn đó nếu còn trong bốn màn của game.</summary>
    /// <param name="completedLevel">Số màn vừa hoàn thành.</param>
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

    /// <summary>Xóa khóa tiến trình và đặt lại toàn bộ điểm nếu có ScoreManager.</summary>
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
