using UnityEngine;

/// <summary>Phát hiệu ứng nổ và gây sát thương một lần khi thiên thạch va vào tàu rồi hủy thiên thạch.</summary>
public class AsteroidCollision : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 10;

    private bool hasHitPlayer = false;

    /// <summary>Tìm máu người chơi, phát vụ nổ, gây sát thương một lần rồi hủy thiên thạch.</summary>
    /// <param name="collision">Thông tin va chạm vật lý 2D.</param>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHitPlayer || !GameManager.CanAdvanceGameplay)
            return;

        PlayerHealth playerHealth =
            collision.gameObject.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            playerHealth =
                collision.gameObject.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth == null)
            return;

        // Destroy được xử lý cuối frame, nên cần chặn trước để các va chạm khác không trừ máu lần nữa.
        hasHitPlayer = true;

        // Phát trước khi trừ máu vì đòn cuối có thể làm GameManager chuyển sang scene Game Over.
        if (TryGetComponent<AsteroidDeathVfx>(out var deathVfx))
            deathVfx.PlayExplosion();

        playerHealth.TakeDamage(damage);

        Destroy(gameObject);
    }

    /// <summary>Cung cấp mức sát thương va chạm đã cấu hình.</summary>
    /// <returns>Lượng máu bị trừ khi thiên thạch chạm tàu.</returns>
    public int GetDamage()
    {
        return damage;
    }
}
