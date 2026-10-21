using UnityEngine;

/// <summary>Cộng điểm và thu hồi ngôi sao khi tàu của người chơi chạm vào.</summary>
public class Star : MonoBehaviour
{
    [Header("Score")]
    [SerializeField] private int scoreValue = 10;

    /// <summary>Nếu đối tượng chạm vào có máu người chơi, cộng điểm rồi hủy ngôi sao khi game đang chạy.</summary>
    /// <param name="other">Collider đi vào vùng thu thập ngôi sao.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!GameManager.CanAdvanceGameplay) return;

        PlayerHealth playerHealth =
            other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreValue);
        }

        Destroy(gameObject);
    }
}
