using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Đọc phím di chuyển và giữ tàu trong vùng nhìn của camera.</summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer playerSprite;

    [Header("Screen Boundary")]
    [SerializeField] private float screenPadding = 0.1f;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private Camera mainCamera;
    private Vector2 movementInput;

    private PlayerConfig playerConfig;

    /// <summary>Lấy vật lý, collider, camera và cấu hình tàu để chuẩn bị di chuyển.</summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;

        playerConfig = GetComponent<PlayerConfig>();

        if (playerSprite == null)
            playerSprite = GetComponent<SpriteRenderer>();
    }

    /// <summary>Đọc WASD hoặc phím mũi tên và chuẩn hóa hướng để di chuyển chéo không nhanh hơn.</summary>
    private void Update()
    {
        movementInput = Vector2.zero;
        if (!GameManager.CanAdvanceGameplay) return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.wKey.isPressed ||
            keyboard.upArrowKey.isPressed)
        {
            movementInput.y += 1f;
        }

        if (keyboard.sKey.isPressed ||
            keyboard.downArrowKey.isPressed)
        {
            movementInput.y -= 1f;
        }

        if (keyboard.aKey.isPressed ||
            keyboard.leftArrowKey.isPressed)
        {
            movementInput.x -= 1f;
        }

        if (keyboard.dKey.isPressed ||
            keyboard.rightArrowKey.isPressed)
        {
            movementInput.x += 1f;
        }

        // Hướng chéo có độ dài lớn hơn 1; chuẩn hóa giúp giữ cùng tốc độ với đi ngang hoặc dọc.
        movementInput = movementInput.normalized;
    }

    /// <summary>Tính vị trí từ hướng nhập và tốc độ tàu, giới hạn trong màn hình rồi di chuyển bằng Rigidbody2D.</summary>
    private void FixedUpdate()
    {
        if (!GameManager.CanAdvanceGameplay || playerConfig == null)
            return;

        Vector2 newPosition =
            rb.position +
            movementInput *
            playerConfig.moveSpeed *
            Time.fixedDeltaTime;

        newPosition = ClampToScreen(newPosition);

        rb.MovePosition(newPosition);
    }

    /// <summary>Giới hạn vị trí tâm tàu theo mép camera, có tính nửa kích thước collider và khoảng đệm.</summary>
    /// <param name="position">Vị trí tàu dự định di chuyển đến.</param>
    /// <returns>Vị trí nằm trong giới hạn; giữ nguyên nếu thiếu camera hoặc collider.</returns>
    private Vector2 ClampToScreen(Vector2 position)
    {
        if (mainCamera == null ||
            playerCollider == null)
        {
            return position;
        }

        float halfWidth =
            playerCollider.bounds.extents.x;

        float halfHeight =
            playerCollider.bounds.extents.y;

        float cameraHeight =
            mainCamera.orthographicSize;

        float cameraWidth =
            cameraHeight * mainCamera.aspect;

        float cameraLeft =
            mainCamera.transform.position.x -
            cameraWidth;

        float cameraRight =
            mainCamera.transform.position.x +
            cameraWidth;

        float cameraBottom =
            mainCamera.transform.position.y -
            cameraHeight;

        float cameraTop =
            mainCamera.transform.position.y +
            cameraHeight;

        // Thu hẹp vùng đặt tâm tàu theo collider để cả thân tàu và khoảng đệm đều nằm trong màn hình.
        float minX =
            cameraLeft +
            halfWidth +
            screenPadding;

        float maxX =
            cameraRight -
            halfWidth -
            screenPadding;

        float minY =
            cameraBottom +
            halfHeight +
            screenPadding;

        float maxY =
            cameraTop -
            halfHeight -
            screenPadding;

        position.x =
            Mathf.Clamp(position.x, minX, maxX);

        position.y =
            Mathf.Clamp(position.y, minY, maxY);

        return position;
    }
}
