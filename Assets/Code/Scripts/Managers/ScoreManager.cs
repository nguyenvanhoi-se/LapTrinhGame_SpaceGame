using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private int totalScore = 0;
    [SerializeField] private int levelScore = 0;

    public event Action<int> OnScoreChanged;

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

    public void AddScore(int amount)
    {
        levelScore += amount;
        totalScore += amount;

        Debug.Log(
            "Level Score: " + levelScore +
            " | Total Score: " + totalScore
        );

        // Báo ngay cho UI
        OnScoreChanged?.Invoke(levelScore);
    }

    public int GetLevelScore()
    {
        return levelScore;
    }

    public int GetTotalScore()
    {
        return totalScore;
    }

    public void ResetLevelScore()
    {
        levelScore = 0;

        OnScoreChanged?.Invoke(levelScore);
    }

    public void ResetAllScore()
    {
        levelScore = 0;
        totalScore = 0;

        OnScoreChanged?.Invoke(levelScore);
    }
}