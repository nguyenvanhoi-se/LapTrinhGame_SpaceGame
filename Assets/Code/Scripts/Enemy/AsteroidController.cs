using UnityEngine;

/// <summary>Điều khiển thiên thạch rơi và gây sát thương khi lọt qua mép dưới màn hình.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class AsteroidController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Damage to Player")]
    [SerializeField] private int damage = 10;

    private Rigidbody2D rb;
    private Camera mainCamera;
    private bool hasReachedBottom = false;
    private float speedMultiplier = 1f;

    public float BaseMoveSpeed => moveSpeed;
    public float CurrentMoveSpeed => moveSpeed * speedMultiplier;

    /// <summary>Áp dụng hệ số tốc độ riêng cho màn chơi và giới hạn tối thiểu ở 0,1.</summary>
    /// <param name="multiplier">Hệ số nhân với tốc độ rơi gốc.</param>
    public void ConfigureSpeed(float multiplier)
    {
        speedMultiplier = Mathf.Max(0.1f, multiplier);
    }

    /// <summary>Lấy Rigidbody2D và camera chính để di chuyển và kiểm tra mép màn hình.</summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
    }

    /// <summary>Cho thiên thạch rơi theo nhịp vật lý và xử lý khi tâm của nó chạm mép dưới vùng nhìn.</summary>
    private void FixedUpdate()
    {
        if (hasReachedBottom || !GameManager.CanAdvanceGameplay)
            return;

        // Cho asteroid rơi thẳng xuống
        Vector2 newPosition =
            rb.position +
            Vector2.down * CurrentMoveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(newPosition);

        // Kiểm tra theo mép màn hình thật
        if (mainCamera != null)
        {
            Vector3 screenPosition =
                mainCamera.WorldToViewportPoint(transform.position);

            // Khi tâm asteroid đã chạm/vượt mép dưới màn hình
            if (screenPosition.y <= 0f)
            {
                DamagePlayerAndDestroy();
            }
        }
    }

    /// <summary>Đánh dấu đã lọt qua màn hình, trừ máu người chơi nếu tìm thấy rồi hủy thiên thạch.</summary>
    private void DamagePlayerAndDestroy()
    {
        if (hasReachedBottom)
            return;

        hasReachedBottom = true;

        PlayerHealth playerHealth =
            FindFirstObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
