using UnityEngine;

/// <summary>Cho ngôi sao rơi thẳng xuống bằng hệ thống vật lý 2D.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class StarMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;

    private Rigidbody2D rb;

    /// <summary>Lấy Rigidbody2D để dùng khi cập nhật vị trí ngôi sao.</summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>Di chuyển ngôi sao xuống dưới theo nhịp vật lý khi game cho phép cập nhật.</summary>
    private void FixedUpdate()
    {
        if (!GameManager.CanAdvanceGameplay) return;
        // Star chỉ rơi thẳng xuống
        Vector2 movement = Vector2.down * moveSpeed;

        rb.MovePosition(
            rb.position + movement * Time.fixedDeltaTime
        );
    }
}
