using UnityEngine;

/// <summary>Hiện thông báo đầu màn rồi tự ẩn sau khoảng thời gian cấu hình.</summary>
public class LevelStartUI : MonoBehaviour
{
    [SerializeField] private float showDuration = 2f;

    /// <summary>Hẹn gọi Hide sau thời gian hiển thị được cấu hình.</summary>
    private void Start()
    {
        Invoke(nameof(Hide), showDuration);
    }

    /// <summary>Ẩn đối tượng chứa thông báo đầu màn.</summary>
    private void Hide()
    {
        gameObject.SetActive(false);
    }
}