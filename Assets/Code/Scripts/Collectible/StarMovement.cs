using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class StarMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // Star chỉ rơi thẳng xuống
        Vector2 movement = Vector2.down * moveSpeed;

        rb.MovePosition(
            rb.position + movement * Time.fixedDeltaTime
        );
    }
}