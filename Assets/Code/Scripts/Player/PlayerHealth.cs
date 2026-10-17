using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private PlayerConfig playerConfig;

    private int currentHealth;
    private PlayerHealthUI healthUI;

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

    public void TakeDamage(int damage)
    {
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

    private void Die()
    {
        Debug.Log("PLAYER GAME OVER");

        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();

        gameObject.SetActive(false);
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return playerConfig != null
            ? playerConfig.maxHP
            : 0;
    }
}