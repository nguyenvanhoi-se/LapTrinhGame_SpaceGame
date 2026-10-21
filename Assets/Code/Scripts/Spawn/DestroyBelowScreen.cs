using UnityEngine;

/// <summary>Thu hồi đối tượng khi đi xuống dưới độ cao giới hạn.</summary>
public class DestroyBelowScreen : MonoBehaviour
{
    [SerializeField] private float destroyY = -7f;

    /// <summary>Hủy đối tượng khi thấp hơn mốc destroyY và game đang chạy.</summary>
    private void Update()
    {
        if (!GameManager.CanAdvanceGameplay) return;
        if (transform.position.y < destroyY)
        {
            Destroy(gameObject);
        }
    }
}
