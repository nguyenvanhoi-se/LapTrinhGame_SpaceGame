using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Playing,
        LevelComplete,
        GameOver
    }

    private GameState currentState = GameState.Playing;

    private int currentLevel;
    private int requiredScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        SetupLevel();
    }

    private void SetupLevel()
    {
        currentLevel = GetCurrentLevelFromScene();

        PlayerPrefs.SetInt("LastLevel", currentLevel);
        PlayerPrefs.Save();

        requiredScore = GetRequiredScore(currentLevel);
        currentState = GameState.Playing;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetLevelScore();
        }

        Debug.Log(
            "LEVEL " + currentLevel +
            " | Required Score: " + requiredScore
        );
    }

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

    private int GetRequiredScore(int level)
    {
        switch (level)
        {
            case 1:
                return 100;

            case 2:
                return 200;

            case 3:
                return 400;

            case 4:
                return 1000;

            default:
                return 100;
        }
    }

    private void Update()
    {
        if (currentState != GameState.Playing)
            return;

        CheckLevelComplete();
    }

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

    private void LevelComplete()
    {
        if (currentState != GameState.Playing)
            return;

        currentState = GameState.LevelComplete;

        Debug.Log(
            "LEVEL " + currentLevel + " COMPLETE!"
        );

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.CompleteLevel(currentLevel);
        }
    }

    public void GameOver()
    {
        if (currentState != GameState.Playing)
            return;

        currentState = GameState.GameOver;

        Debug.Log("GAME OVER");

        UnityEngine.SceneManagement.SceneManager
            .LoadScene("EndGame");
    }

    public GameState GetGameState()
    {
        return currentState;
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    public int GetRequiredScore()
    {
        return requiredScore;
    }
}