using UnityEngine;

/// <summary>Điều phối trạng thái màn chơi, tạm dừng, điều kiện kết thúc và ghi lịch sử.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>Các trạng thái chơi, hoàn thành màn, thua và tạm dừng.</summary>
    public enum GameState
    {
        Playing,
        LevelComplete,
        GameOver,
        Paused
    }

    private GameState currentState = GameState.Playing;

    private int currentLevel;
    [Header("Level Goal")]
    [SerializeField, Min(1)] private int requiredScore = 100;

    private float timeScaleBeforePause = 1f;
    private bool ownsPause;
    private string historyRunId;
    private double playedSeconds;
    private double historyClock;
    private bool historyFinished;

    public bool IsPaused => currentState == GameState.Paused;
    /// <summary>Cho phép cập nhật gameplay khi thời gian đang chạy và màn còn ở trạng thái chơi, hoặc chưa có GameManager.</summary>
    public static bool CanAdvanceGameplay => Time.timeScale > 0f &&
        (Instance == null || Instance.currentState == GameState.Playing);

    /// <summary>Thiết lập instance dùng chung cho màn chơi và hủy bộ quản lý bị trùng.</summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>Khởi tạo trạng thái, điểm và bộ đếm thời gian của màn hiện tại.</summary>
    private void Start()
    {
        SetupLevel();
    }

    /// <summary>Khôi phục thời gian, xác định màn, lưu màn gần nhất, xóa điểm màn và tạo ID lượt chơi.</summary>
    private void SetupLevel()
    {
        PrepareForSceneChange();
        currentLevel = GetCurrentLevelFromScene();

        PlayerPrefs.SetInt("LastLevel", currentLevel);
        PlayerPrefs.Save();

        currentState = GameState.Playing;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetLevelScore();
        }

        historyRunId = System.Guid.NewGuid().ToString("N");
        playedSeconds = 0;
        // Lưu mốc thời gian thực để đo riêng thời lượng của lượt chơi mới.
        historyClock = Time.realtimeSinceStartupAsDouble;
        historyFinished = false;

        Debug.Log(
            "LEVEL " + currentLevel +
            " | Required Score: " + requiredScore
        );
    }

    /// <summary>Đổi tên scene Level1 đến Level4 thành số màn, mặc định là 1.</summary>
    /// <returns>Số màn tương ứng với scene hiện tại.</returns>
    private int GetCurrentLevelFromScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager
            .GetActiveScene()
            .name;

        switch (sceneName)
        {
            case "Level1":
                return 1;

            case "Level2":
                return 2;

            case "Level3":
                return 3;

            case "Level4":
                return 4;

            default:
                return 1;
        }
    }

    /// <summary>Lưu tốc độ thời gian, chốt thời gian đã chơi và tạm dừng game cùng âm thanh nếu đang chơi.</summary>
    /// <returns>True nếu vừa chuyển sang tạm dừng; false nếu trạng thái hiện tại không cho phép.</returns>
    public bool PauseGame()
    {
        if (currentState != GameState.Playing || Time.timeScale <= 0f)
            return false;

        timeScaleBeforePause = Time.timeScale;
        AccumulatePlayTime();
        ownsPause = true;
        currentState = GameState.Paused;
        Time.timeScale = 0f;
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetGameplayPaused(true);
        return true;
    }

    /// <summary>Đặt lại mốc tính thời lượng rồi khôi phục trạng thái chơi và thời gian trước khi tạm dừng.</summary>
    public void ResumeGame()
    {
        if (!IsPaused) return;
        // Bắt đầu mốc mới để thời gian nằm trong menu tạm dừng không được cộng vào lượt chơi.
        historyClock = Time.realtimeSinceStartupAsDouble;
        currentState = GameState.Playing;
        PrepareForSceneChange();
    }

    /// <summary>Giải phóng trạng thái tạm dừng và khôi phục âm thanh để scene tiếp theo không bị đứng.</summary>
    public void PrepareForSceneChange()
    {
        // Trả lại tốc độ trước đó chỉ khi chính bộ quản lý này đã đặt lệnh tạm dừng.
        if (ownsPause)
        {
            Time.timeScale = timeScaleBeforePause;
            ownsPause = false;
        }
        if (Time.timeScale <= 0f) Time.timeScale = 1f;
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetGameplayPaused(false);
    }

    /// <summary>Khôi phục thời gian và xóa instance nếu bộ quản lý hiện tại bị hủy.</summary>
    private void OnDestroy()
    {
        if (Instance != this) return;
        PrepareForSceneChange();
        Instance = null;
    }

    /// <summary>Cộng thời gian chơi thực tế và kiểm tra đủ điểm khi trạng thái cho phép chơi.</summary>
    private void Update()
    {
        AccumulatePlayTime();
        if (!CanAdvanceGameplay)
            return;

        CheckLevelComplete();
    }

    /// <summary>So sánh điểm màn hiện tại với mục tiêu rồi xử lý hoàn thành nếu đã đạt.</summary>
    private void CheckLevelComplete()
    {
        if (ScoreManager.Instance == null)
            return;

        int currentScore =
            ScoreManager.Instance.GetLevelScore();

        if (currentScore >= requiredScore)
        {
            LevelComplete();
        }
    }

    /// <summary>Chốt thời gian, ghi lịch sử hoàn thành và yêu cầu LevelManager xử lý màn tiếp theo.</summary>
    private void LevelComplete()
    {
        if (currentState != GameState.Playing)
            return;

        AccumulatePlayTime();
        currentState = GameState.LevelComplete;
        RecordHistory(GameHistoryRecord.Completed);
        PrepareForSceneChange();

        Debug.Log(
            "LEVEL " + currentLevel + " COMPLETE!"
        );

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.CompleteLevel(currentLevel);
        }
    }

    /// <summary>Chốt kết quả thua một lần, ghi lịch sử và chuyển đến scene EndGame.</summary>
    public void GameOver()
    {
        if (currentState != GameState.Playing && !IsPaused)
            return;

        AccumulatePlayTime();
        currentState = GameState.GameOver;
        RecordHistory(GameHistoryRecord.GameOver);
        PrepareForSceneChange();

        Debug.Log("GAME OVER");

        UnityEngine.SceneManagement.SceneManager
            .LoadScene("EndGame");
    }

    /// <summary>Cộng khoảng thời gian thực kể từ lần gọi trước chỉ khi lượt chơi còn đang diễn ra.</summary>
    private void AccumulatePlayTime()
    {
        double now = Time.realtimeSinceStartupAsDouble;
        if (historyRunId != null && !historyFinished && currentState == GameState.Playing)
            playedSeconds += System.Math.Max(0, now - historyClock);
        historyClock = now;
    }

    /// <summary>Ghi một kết quả duy nhất cho ID lượt chơi cùng điểm, thời lượng và thời điểm kết thúc.</summary>
    /// <param name="result">Kết quả hoàn thành hoặc thua theo hằng của GameHistoryRecord.</param>
    private void RecordHistory(string result)
    {
        if (historyFinished || historyRunId == null) return;
        // Chặn trước khi lưu để các lời gọi kết thúc liên tiếp không tạo thêm bản ghi.
        historyFinished = true;
        var scores = ScoreManager.Instance;
        GameHistoryManager.Instance.AddHistory(new GameHistoryRecord
        {
            id = historyRunId, level = currentLevel,
            score = scores != null ? scores.GetLevelScore() : 0,
            hasTotalScore = scores != null,
            totalScore = scores != null ? scores.GetTotalScore() : 0,
            playedSeconds = playedSeconds, result = result,
            endedAt = System.DateTimeOffset.Now.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
        });
    }

    /// <summary>Cung cấp trạng thái điều phối hiện tại.</summary>
    /// <returns>Trạng thái đang chơi, hoàn thành, thua hoặc tạm dừng.</returns>
    public GameState GetGameState()
    {
        return currentState;
    }

    /// <summary>Cung cấp số màn được xác định khi khởi tạo.</summary>
    /// <returns>Số màn hiện tại.</returns>
    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    /// <summary>Cung cấp mục tiêu điểm của màn.</summary>
    /// <returns>Số điểm cần đạt để hoàn thành.</returns>
    public int GetRequiredScore()
    {
        return requiredScore;
    }
}
