using UnityEngine;
using UnityEngine.InputSystem;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;

        playerConfig = GetComponent<PlayerConfig>();

        if (playerSprite == null)
            playerSprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        movementInput = Vector2.zero;

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

        movementInput = movementInput.normalized;
    }

    private void FixedUpdate()
    {
        if (playerConfig == null)
            return;

        Vector2 newPosition =
            rb.position +
            movementInput *
            playerConfig.moveSpeed *
            Time.fixedDeltaTime;

        newPosition = ClampToScreen(newPosition);

        rb.MovePosition(newPosition);
    }

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