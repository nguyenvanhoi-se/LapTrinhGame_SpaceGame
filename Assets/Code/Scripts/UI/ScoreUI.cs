using TMPro;
using UnityEngine;

/// <summary>Hiển thị điểm màn chơi và cập nhật khi nhận sự kiện thay đổi điểm.</summary>
public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    /// <summary>Hiển thị điểm ban đầu và đăng ký nhận sự kiện thay đổi điểm từ ScoreManager.</summary>
    private void Start()
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("ScoreUI: Chưa có ScoreManager!");
            return;
        }

        // Hiển thị điểm hiện tại ngay khi mở scene
        UpdateScore(ScoreManager.Instance.GetLevelScore());

        // Đăng ký nhận thông báo khi điểm thay đổi
        ScoreManager.Instance.OnScoreChanged += UpdateScore;
    }

    /// <summary>Hủy đăng ký sự kiện để bộ quản lý điểm không gọi vào giao diện đã bị hủy.</summary>
    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScore;
        }
    }

    /// <summary>Ghi điểm mới lên nhãn giao diện nếu nhãn đã được gán.</summary>
    /// <param name="score">Điểm màn cần hiển thị.</param>
    private void UpdateScore(int score)
    {
        if (scoreText == null)
        {
            Debug.LogWarning("ScoreUI: Chưa gán Score Text!");
            return;
        }

        scoreText.text = "SCORE: " + score;
    }
}