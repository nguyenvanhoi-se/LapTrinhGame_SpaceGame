using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EndGameUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    private int lastLevel = 1;

    private void Start()
    {
        if (ScoreManager.Instance != null)
        {
            int finalScore = ScoreManager.Instance.GetLevelScore();
            scoreText.text = finalScore.ToString();
        }

        lastLevel = PlayerPrefs.GetInt("LastLevel", 1);
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene("Level" + lastLevel);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Debug.Log("QUIT GAME");

        Application.Quit();
    }
}