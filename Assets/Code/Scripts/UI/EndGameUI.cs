using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>Hiển thị điểm khi thua và xử lý chơi lại, về menu hoặc thoát game.</summary>
public class EndGameUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    private int lastLevel = 1;

    /// <summary>Hiển thị điểm màn vừa chơi và đọc số màn gần nhất để dùng cho nút chơi lại.</summary>
    private void Start()
    {
        if (ScoreManager.Instance != null)
        {
            int finalScore = ScoreManager.Instance.GetLevelScore();
            scoreText.text = finalScore.ToString();
        }

        lastLevel = PlayerPrefs.GetInt("LastLevel", 1);
    }

    /// <summary>Tải lại màn chơi gần nhất được lưu trong PlayerPrefs.</summary>
    public void PlayAgain()
    {
        SceneManager.LoadScene("Level" + lastLevel);
    }

    /// <summary>Tải scene menu chính.</summary>
    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>Ghi thông báo và yêu cầu thoát ứng dụng.</summary>
    public void QuitGame()
    {
        Debug.Log("QUIT GAME");

        Application.Quit();
    }
}