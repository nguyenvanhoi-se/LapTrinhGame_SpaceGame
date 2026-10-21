using System;
using UnityEngine;

/// <summary>Quản lý điểm màn hiện tại, tổng điểm và phát sự kiện cập nhật giao diện.</summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private int totalScore = 0;
    [SerializeField] private int levelScore = 0;

    public event Action<int> OnScoreChanged;

    /// <summary>Giữ một bộ quản lý điểm duy nhất và không hủy khi chuyển scene.</summary>
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

    /// <summary>Cộng vào điểm màn và tổng điểm khi game đang chạy rồi báo giao diện cập nhật.</summary>
    /// <param name="amount">Số điểm cần cộng.</param>
    public void AddScore(int amount)
    {
        if (!GameManager.CanAdvanceGameplay) return;

        levelScore += amount;
        totalScore += amount;

        Debug.Log(
            "Level Score: " + levelScore +
            " | Total Score: " + totalScore
        );

        // Báo ngay cho UI
        OnScoreChanged?.Invoke(levelScore);
    }

    /// <summary>Lấy số điểm đang tích lũy trong màn.</summary>
    /// <returns>Điểm màn hiện tại.</returns>
    public int GetLevelScore()
    {
        return levelScore;
    }

    /// <summary>Lấy tổng điểm đang giữ qua các màn.</summary>
    /// <returns>Tổng điểm hiện tại.</returns>
    public int GetTotalScore()
    {
        return totalScore;
    }

    /// <summary>Đặt điểm màn về 0 và phát sự kiện thay đổi điểm.</summary>
    public void ResetLevelScore()
    {
        levelScore = 0;

        OnScoreChanged?.Invoke(levelScore);
    }

    /// <summary>Đặt cả điểm màn lẫn tổng điểm về 0 rồi báo giao diện.</summary>
    public void ResetAllScore()
    {
        levelScore = 0;
        totalScore = 0;

        OnScoreChanged?.Invoke(levelScore);
    }
}
