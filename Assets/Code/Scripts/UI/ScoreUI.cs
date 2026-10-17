using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

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

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScore;
        }
    }

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