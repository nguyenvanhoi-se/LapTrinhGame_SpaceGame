using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Cung cấp dịch vụ lịch sử dùng chung giữa các scene và thông báo khi dữ liệu thay đổi.</summary>
public sealed class GameHistoryManager : MonoBehaviour
{
    static GameHistoryManager instance;
    GameHistoryStore store;
    /// <summary>Lấy dịch vụ dùng chung; tự tạo GameObject chứa dịch vụ ở lần truy cập đầu tiên nếu cần.</summary>
    public static GameHistoryManager Instance
    {
        get
        {
            if (instance == null) new GameObject("GameHistoryManager").AddComponent<GameHistoryManager>();
            return instance;
        }
    }
    public event Action HistoryChanged;
    public string FilePath => store.FilePath;
    public string LastError => store.LastError;

    /// <summary>Xóa tham chiếu tĩnh khi Unity khởi tạo phiên chạy để tránh giữ instance cũ.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetInstance() => instance = null;

    /// <summary>Giữ một instance xuyên scene và mở kho lịch sử trong thư mục dữ liệu lâu dài của game.</summary>
    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        store = new GameHistoryStore(Path.Combine(Application.persistentDataPath, "game_history.json"));
    }

    /// <summary>Thêm lượt chơi qua kho dữ liệu, báo thay đổi nếu thêm được và ghi cảnh báo khi có lỗi lưu.</summary>
    /// <param name="record">Kết quả lượt chơi cần thêm.</param>
    /// <returns>True nếu bản ghi được thêm vào bộ nhớ, kể cả khi chưa ghi được xuống đĩa.</returns>
    public bool AddHistory(GameHistoryRecord record)
    {
        bool added = store.AddHistory(record);
        if (added) HistoryChanged?.Invoke();
        if (store.LastError != null) Debug.LogWarning(store.LastError);
        return added;
    }
    /// <summary>Lấy các bản sao lịch sử từ kho để bên gọi không sửa trực tiếp dữ liệu gốc.</summary>
    /// <returns>Danh sách lượt chơi theo thứ tự mới nhất trước.</returns>
    public IReadOnlyList<GameHistoryRecord> GetHistory() => store.GetHistory();
    /// <summary>Yêu cầu kho ghi các thay đổi đang chờ xuống đĩa.</summary>
    /// <returns>True nếu lưu thành công hoặc không có thay đổi cần lưu.</returns>
    public bool SaveHistory() => store.SaveHistory();
    /// <summary>Nạp lại lịch sử và thông báo để giao diện cập nhật.</summary>
    /// <returns>True nếu file chính được nạp thành công hoặc chưa tồn tại.</returns>
    public bool LoadHistory()
    {
        bool loaded = store.LoadHistory();
        HistoryChanged?.Invoke();
        return loaded;
    }
    /// <summary>Yêu cầu kho xóa lịch sử và thông báo để giao diện cập nhật.</summary>
    /// <returns>True nếu xóa dữ liệu lưu trữ thành công.</returns>
    public bool ClearHistory()
    {
        bool cleared = store.ClearHistory();
        HistoryChanged?.Invoke();
        return cleared;
    }
    /// <summary>Thử lưu các thay đổi đang chờ khi ứng dụng bị tạm dừng.</summary>
    /// <param name="paused">True khi ứng dụng chuyển sang trạng thái tạm dừng.</param>
    void OnApplicationPause(bool paused) { if (paused) store?.SaveHistory(); }
    /// <summary>Thử lưu lịch sử còn chờ trước khi ứng dụng thoát.</summary>
    void OnApplicationQuit() => store?.SaveHistory();
    /// <summary>Xóa tham chiếu dùng chung nếu chính instance hiện tại bị hủy.</summary>
    void OnDestroy() { if (instance == this) instance = null; }
}
