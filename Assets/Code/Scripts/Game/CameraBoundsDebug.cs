using UnityEngine;

/// <summary>In giới hạn vùng nhìn của camera trực giao để hỗ trợ kiểm tra màn chơi.</summary>
public class CameraBoundsDebug : MonoBehaviour
{
    /// <summary>Tính chiều rộng, chiều cao và bốn mép camera trực giao rồi in ra Console.</summary>
    private void Start()
    {
        Camera cam = GetComponent<Camera>();

        if (cam == null || !cam.orthographic)
        {
            Debug.LogWarning("Camera phải là Orthographic!");
            return;
        }

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        float left = cam.transform.position.x - width / 2f;
        float right = cam.transform.position.x + width / 2f;
        float bottom = cam.transform.position.y - height / 2f;
        float top = cam.transform.position.y + height / 2f;

        Debug.Log(
            "=== CAMERA BOUNDS ===\n" +
            "Aspect: " + cam.aspect + "\n" +
            "Width: " + width + "\n" +
            "Height: " + height + "\n" +
            "Left: " + left + "\n" +
            "Right: " + right + "\n" +
            "Bottom: " + bottom + "\n" +
            "Top: " + top
        );
    }
}