using UnityEngine;

/// <summary>Quản lý máu tàu, cập nhật giao diện và báo thua khi hết máu.</summary>
public class PlayerHealth : MonoBehaviour
{
    private PlayerConfig playerConfig;

    private int currentHealth;
    private PlayerHealthUI healthUI;

    /// <summary>Đọc cấu hình máu tối đa của tàu và tìm giao diện hiển thị máu.</summary>
    private void Awake()
    {
        playerConfig = GetComponent<PlayerConfig>();

        if (playerConfig == null)
        {
            Debug.LogError(
                "PlayerHealth: Chưa có PlayerConfig!"
            );
            return;
        }

        currentHealth = playerConfig.maxHP;

        healthUI =
            FindFirstObjectByType<PlayerHealthUI>();
    }

    /// <summary>Trừ máu khi game đang chạy, cập nhật giao diện và xử lý thua nếu hết máu.</summary>
    /// <param name="damage">Lượng sát thương nhận vào.</param>
    public void TakeDamage(int damage)
    {
        if (!GameManager.CanAdvanceGameplay) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log(
            "Player nhận " +
            damage +
            " damage → HP: " +
            currentHealth +
            "/" +
            playerConfig.maxHP
        );

        if (healthUI == null)
        {
            healthUI =
                FindFirstObjectByType<PlayerHealthUI>();
        }

        if (healthUI != null)
        {
            healthUI.UpdateHP();
        }

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>Báo GameManager xử lý thua rồi vô hiệu hóa đối tượng tàu.</summary>
    private void Die()
    {
        Debug.Log("PLAYER GAME OVER");

        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();

        gameObject.SetActive(false);
    }

    /// <summary>Cung cấp lượng máu còn lại của tàu.</summary>
    /// <returns>Máu hiện tại.</returns>
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>Đọc giới hạn máu từ cấu hình tàu.</summary>
    /// <returns>Máu tối đa, hoặc 0 nếu thiếu PlayerConfig.</returns>
    public int GetMaxHealth()
    {
        return playerConfig != null
            ? playerConfig.maxHP
            : 0;
    }
}
