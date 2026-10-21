using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Điều khiển bảng lịch sử và hộp thoại xác nhận xóa trong menu chính.</summary>
public sealed class GameHistoryUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] GameObject confirmation;
    [SerializeField] GameObject[] menuElements;
    [SerializeField] RectTransform content;
    [SerializeField] GameHistoryRowUI rowPrefab;
    [SerializeField] TMP_Text emptyText;
    [SerializeField] TMP_Text statusText;
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] Button historyButton;
    [SerializeField] Button backButton;
    [SerializeField] Button clearButton;
    [SerializeField] Button cancelButton;
    readonly List<GameHistoryRowUI> rows = new List<GameHistoryRowUI>();
    readonly List<GameObject> hiddenMenuElements = new List<GameObject>();
    GameHistoryManager manager;
    bool showing;

    /// <summary>Ẩn các phần menu đang hiện, đăng ký nhận thay đổi và mở bảng lịch sử với nút quay lại được chọn.</summary>
    public void Open()
    {
        if (showing) return;
        showing = true;
        hiddenMenuElements.Clear();
        // Chỉ nhớ các phần đang hiện để khi quay lại không bật nhầm phần vốn đã bị ẩn.
        foreach (var element in menuElements)
            if (element != null && element.activeSelf) { hiddenMenuElements.Add(element); element.SetActive(false); }
        manager = GameHistoryManager.Instance;
        manager.HistoryChanged += Refresh;
        panel.SetActive(true);
        confirmation.SetActive(false);
        Refresh();
        Select(backButton);
    }

    /// <summary>Đóng bảng, hủy đăng ký sự kiện và khôi phục những phần menu đã bị ẩn khi mở lịch sử.</summary>
    public void Back()
    {
        if (!showing) return;
        if (manager != null) manager.HistoryChanged -= Refresh;
        showing = false;
        backButton.interactable = true;
        scrollRect.enabled = true;
        confirmation.SetActive(false);
        panel.SetActive(false);
        foreach (var element in hiddenMenuElements) if (element != null) element.SetActive(true);
        hiddenMenuElements.Clear();
        Select(historyButton);
    }

    /// <summary>Mở hộp xác nhận khi có lịch sử và tạm khóa thao tác cuộn, quay lại, xóa bên dưới.</summary>
    public void AskClearHistory()
    {
        if (!showing || manager.GetHistory().Count == 0) return;
        confirmation.SetActive(true);
        backButton.interactable = clearButton.interactable = false;
        scrollRect.enabled = false;
        Select(cancelButton);
    }

    /// <summary>Đóng xác nhận, bật lại thao tác và chọn nút phù hợp với dữ liệu còn lại.</summary>
    public void CancelClearHistory()
    {
        confirmation.SetActive(false);
        backButton.interactable = true;
        scrollRect.enabled = true;
        Refresh();
        Select(clearButton.interactable ? clearButton : backButton);
    }

    /// <summary>Chỉ yêu cầu xóa khi bảng và hộp xác nhận đang mở, sau đó đóng hộp xác nhận.</summary>
    public void DeleteHistory()
    {
        // Nút DELETE trong hộp xác nhận gọi hàm này; kiểm tra hộp đang mở trước khi xóa.
        if (!showing || !confirmation.activeSelf) return;
        manager.ClearHistory();
        CancelClearHistory();
    }

    /// <summary>Tạo lại các dòng lịch sử, cập nhật trạng thái rỗng hoặc lỗi lưu và đưa danh sách về đầu.</summary>
    void Refresh()
    {
        if (!showing) return;
        foreach (var row in rows) if (row != null) { row.gameObject.SetActive(false); Destroy(row.gameObject); }
        rows.Clear();
        var history = manager.GetHistory();
        foreach (var record in history)
        {
            var row = Instantiate(rowPrefab, content, false);
            row.Bind(record);
            rows.Add(row);
        }
        emptyText.gameObject.SetActive(history.Count == 0);
        statusText.text = manager.LastError == null ? "" : "HISTORY STORAGE ERROR\nYour existing file is preserved. Please try again later.";
        statusText.gameObject.SetActive(manager.LastError != null);
        clearButton.interactable = !confirmation.activeSelf && history.Count > 0;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        scrollRect.verticalNormalizedPosition = 1;
    }

    /// <summary>Đặt nút làm đối tượng được chọn để hỗ trợ điều hướng bằng bàn phím.</summary>
    /// <param name="button">Nút cần chọn; null để bỏ lựa chọn hiện tại.</param>
    static void Select(Button button)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button != null ? button.gameObject : null);
    }

    /// <summary>Đóng bảng và khôi phục menu nếu thành phần bị vô hiệu hóa khi lịch sử đang mở.</summary>
    void OnDisable() { if (showing) Back(); }
}
