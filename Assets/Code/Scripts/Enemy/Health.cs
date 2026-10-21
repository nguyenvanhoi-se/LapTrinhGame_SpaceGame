using UnityEngine;

/// <summary>Quản lý máu của đối tượng và xử lý hiệu ứng, hủy đối tượng khi hết máu.</summary>
public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 10;

    private int currentHealth;

    /// <summary>Khởi tạo máu hiện tại bằng máu tối đa.</summary>
    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>Trừ máu khi game đang chạy, giữ máu không âm và xử lý chết nếu máu bằng 0.</summary>
    /// <param name="damage">Lượng sát thương nhận vào.</param>
    public void TakeDamage(int damage)
    {
        if (!GameManager.CanAdvanceGameplay) return;

        currentHealth -= damage;

        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log(
            gameObject.name +
            " nhận " + damage +
            " damage → HP: " +
            currentHealth + "/" +
            maxHealth
        );

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>Phát hiệu ứng nổ nếu có rồi hủy đối tượng.</summary>
    private void Die()
    {
        Debug.Log(gameObject.name + " đã bị tiêu diệt!");

        // Dùng chung hiệu ứng với va chạm vào tàu; AsteroidDeathVfx chặn phát trùng trên cùng thiên thạch.
        if (TryGetComponent<AsteroidDeathVfx>(out var deathVfx))
            deathVfx.PlayExplosion();

        Destroy(gameObject);
    }

    /// <summary>Cung cấp lượng máu còn lại.</summary>
    /// <returns>Máu hiện tại của đối tượng.</returns>
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>Cung cấp giới hạn máu đã cấu hình.</summary>
    /// <returns>Máu tối đa của đối tượng.</returns>
    public int GetMaxHealth()
    {
        return maxHealth;
    }
}
