using UnityEngine;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
    }

    private void FixedUpdate()
    {
        if (hasReachedBottom)
            return;

        // Cho asteroid rơi thẳng xuống
        Vector2 newPosition =
            rb.position +
            Vector2.down * moveSpeed * Time.fixedDeltaTime;

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