using UnityEngine;

/// <summary>Di chuyển đạn, gây sát thương khi trúng mục tiêu và tự hủy khi hết thời gian tồn tại.</summary>
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private int damage = 10;

    /// <summary>Hẹn hủy viên đạn sau thời gian tồn tại được cấu hình.</summary>
    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    /// <summary>Cho đạn bay theo trục lên cục bộ với tốc độ theo giây khi game đang chạy.</summary>
    private void Update()
    {
        if (!GameManager.CanAdvanceGameplay) return;
        transform.Translate(Vector2.up * speed * Time.deltaTime);
    }

    /// <summary>Gán sát thương từ cấu hình tàu cho viên đạn vừa sinh.</summary>
    /// <param name="newDamage">Sát thương viên đạn sẽ gây ra.</param>
    public void Initialize(int newDamage)
    {
        damage = newDamage;
    }

    /// <summary>Gây sát thương nếu collider có Health rồi hủy viên đạn, chỉ xử lý khi game đang chạy.</summary>
    /// <param name="other">Collider mà đạn vừa đi vào.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!GameManager.CanAdvanceGameplay) return;

        Health health = other.GetComponent<Health>();

        if (health != null)
        {
            health.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
