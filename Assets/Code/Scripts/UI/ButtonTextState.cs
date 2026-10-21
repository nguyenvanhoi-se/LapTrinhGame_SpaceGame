using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Đổi màu chữ của nút theo trạng thái rê chuột, điều hướng và lựa chọn tàu.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class ButtonTextState : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private TMP_Text label;
    private static readonly Color NormalColor = Color.white;
    private static readonly Color ActiveColor = new Color32(0x66, 0xE6, 0xFF, 0xFF);
    private Button button;
    private bool hovered;
    private bool selected;
    private bool chosen;

    /// <summary>Xóa trạng thái rê chuột, đọc nút đang được điều hướng chọn và cập nhật màu chữ.</summary>
    private void OnEnable()
    {
        hovered = false;
        selected = EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == gameObject;
        RefreshColor();
    }

    /// <summary>Xóa trạng thái tương tác và trả chữ về màu bình thường.</summary>
    private void OnDisable()
    {
        hovered = false;
        selected = false;
        if (label != null) label.color = NormalColor;
    }

    /// <summary>Ghi nhận con trỏ đang ở trên nút rồi cập nhật màu chữ.</summary>
    /// <param name="eventData">Dữ liệu sự kiện từ EventSystem; hàm không cần đọc nội dung.</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        RefreshColor();
    }

    /// <summary>Xóa trạng thái con trỏ trên nút rồi cập nhật màu chữ.</summary>
    /// <param name="eventData">Dữ liệu sự kiện từ EventSystem; hàm không cần đọc nội dung.</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        RefreshColor();
    }

    /// <summary>Ghi nhận nút đang được điều hướng chọn rồi cập nhật màu chữ.</summary>
    /// <param name="eventData">Dữ liệu sự kiện từ EventSystem; hàm không cần đọc nội dung.</param>
    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
        RefreshColor();
    }

    /// <summary>Xóa trạng thái điều hướng chọn rồi cập nhật màu chữ.</summary>
    /// <param name="eventData">Dữ liệu sự kiện từ EventSystem; hàm không cần đọc nội dung.</param>
    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        RefreshColor();
    }
    /// <summary>Lưu trạng thái tàu được chọn để giữ màu nhấn của nút tương ứng.</summary>
    /// <param name="value">True nếu nút đại diện cho tàu đang chọn.</param>
    public void SetSelected(bool value)
    {
        chosen = value;
        RefreshColor();
    }

    /// <summary>Tìm thành phần còn thiếu và dùng màu nhấn chỉ khi nút tương tác được, đồng thời đang rê chuột hoặc được chọn.</summary>
    private void RefreshColor()
    {
        if (button == null) button = GetComponent<Button>();
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.color = button.IsInteractable() && (hovered || selected || chosen)
                ? ActiveColor : NormalColor;
    }
}
