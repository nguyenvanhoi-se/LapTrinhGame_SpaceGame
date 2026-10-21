using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>Lưu kết quả, điểm số và thời gian của một lượt chơi.</summary>
[Serializable]
public sealed class GameHistoryRecord
{
    public const string Completed = "COMPLETED";
    public const string GameOver = "GAME OVER";
    public string id;
    public int level;
    public int score;
    public bool hasTotalScore;
    public int totalScore;
    public double playedSeconds;
    // Dùng ISO 8601 và lưu cả độ lệch múi giờ của máy lúc kết thúc lượt chơi.
    public string endedAt;
    public string result;

    /// <summary>Sao chép bản ghi để bên nhận có thể sử dụng mà không sửa bản gốc.</summary>
    /// <returns>Bản ghi mới chứa cùng các giá trị.</returns>
    public GameHistoryRecord Copy() => (GameHistoryRecord)MemberwiseClone();
}

/// <summary>Đọc, ghi lịch sử bằng JSON có phiên bản và bảo vệ dữ liệu cũ khi xảy ra lỗi.</summary>
public sealed class GameHistoryStore
{
    public const int MaximumRecords = 100;
    /// <summary>Đóng gói phiên bản định dạng và danh sách lượt chơi để chuyển đổi sang JSON.</summary>
    [Serializable] sealed class Document
    {
        public int schemaVersion;
        public List<GameHistoryRecord> records;
    }

    readonly string path;
    List<GameHistoryRecord> records = new List<GameHistoryRecord>();
    string loadedFingerprint;
    bool preserveUnreadableFile;
    bool unsupportedSchema;
    public string FilePath => path;
    public string LastError { get; private set; }
    public bool HasPendingChanges { get; private set; }

    /// <summary>Chuẩn hóa đường dẫn tuyệt đối và nạp lịch sử hiện có.</summary>
    /// <param name="filePath">Đường dẫn file JSON dùng để lưu lịch sử.</param>
    public GameHistoryStore(string filePath)
    {
        path = Path.GetFullPath(filePath);
        LoadHistory();
    }

    /// <summary>Sao chép từng lượt chơi ra một danh sách chỉ đọc.</summary>
    /// <returns>Các bản sao của lịch sử hiện đang giữ trong bộ nhớ.</returns>
    public IReadOnlyList<GameHistoryRecord> GetHistory() => records.Select(r => r.Copy()).ToArray();

    /// <summary>Kiểm tra bản ghi, bỏ ID trùng, chuẩn hóa danh sách và thử lưu ngay; dữ liệu vẫn chờ trong bộ nhớ nếu lưu lỗi.</summary>
    /// <param name="record">Bản ghi hợp lệ cần thêm; dữ liệu sai sẽ gây ArgumentException.</param>
    /// <returns>True nếu đã thêm; false nếu ID đã tồn tại.</returns>
    public bool AddHistory(GameHistoryRecord record)
    {
        if (!Valid(record)) throw new ArgumentException("Invalid history record.", nameof(record));
        if (records.Any(r => r.id == record.id)) return false;
        records.Add(record.Copy());
        records = Normalize(records);
        HasPendingChanges = true;
        SaveHistory(); // Nếu ghi đĩa thất bại, giữ thay đổi trong bộ nhớ để thử lưu lại.
        return true;
    }

    /// <summary>Nạp file chính hoặc phục hồi từ bản sao khi có thể, đồng thời giữ nguyên file không đọc được.</summary>
    /// <returns>True nếu đọc file chính thành công hoặc chưa có file; false nếu có lỗi hay thay đổi chưa lưu.</returns>
    public bool LoadHistory()
    {
        // Không nạp đè lên lượt chơi chưa lưu được vì lỗi ghi file.
        if (HasPendingChanges)
        {
            LastError = "History has unsaved changes. Retry saving before reloading.";
            return false;
        }
        LastError = null;
        preserveUnreadableFile = false;
        unsupportedSchema = false;
        try
        {
            loadedFingerprint = Fingerprint(path);
            if (loadedFingerprint == null) { records.Clear(); return true; }
            if (TryRead(path, out var loaded, out string error, out bool future))
            {
                records = loaded;
                return true;
            }
            unsupportedSchema = future;
            preserveUnreadableFile = true;
            // Không ghi đè file dùng định dạng mới hơn vì phiên bản này chưa hiểu cấu trúc đó.
            if (!future && TryRead(path + ".bak", out loaded, out _, out _))
            {
                records = loaded;
                LastError = "Recovered history from backup. The original file is preserved.";
            }
            else
            {
                records.Clear();
                LastError = "Cannot read history: " + error + " The original file is preserved.";
            }
            return false;
        }
        catch (Exception e) when (IsStorageError(e))
        {
            // Chặn ghi tiếp để không thay thế file chưa kiểm tra được nội dung.
            unsupportedSchema = true;
            LastError = "Cannot read history: " + e.Message;
            return false;
        }
    }

    /// <summary>Ghi các thay đổi đang chờ bằng file tạm và cập nhật dấu kiểm tra của file đã lưu.</summary>
    /// <returns>True nếu lưu thành công hoặc không có thay đổi; false khi gặp lỗi lưu trữ.</returns>
    public bool SaveHistory()
    {
        if (!HasPendingChanges) return true;
        try
        {
            PrepareWrite();
            string json = Serialize(records);
            AtomicWrite(path, json, !preserveUnreadableFile);
            loadedFingerprint = Fingerprint(path);
            preserveUnreadableFile = false;
            HasPendingChanges = false;
            LastError = null;
            return true;
        }
        catch (Exception e) when (IsStorageError(e))
        {
            LastError = "History could not be saved. It remains available in this session. " + e.Message;
            return false;
        }
    }

    /// <summary>Ghi lịch sử rỗng vào file dự phòng và file chính rồi xóa dữ liệu trong bộ nhớ; bên gọi cần xác nhận với người chơi trước.</summary>
    /// <returns>True nếu xóa thành công; false khi gặp lỗi lưu trữ.</returns>
    public bool ClearHistory()
    {
        try
        {
            PrepareWrite();
            string empty = Serialize(new List<GameHistoryRecord>());
            // Xóa cả bản dự phòng trước để lần phục hồi sau không làm xuất hiện lại lịch sử đã xóa.
            AtomicWrite(path + ".bak", empty, false);
            AtomicWrite(path, empty, false);
            records.Clear();
            loadedFingerprint = Fingerprint(path);
            preserveUnreadableFile = false;
            HasPendingChanges = false;
            LastError = null;
            return true;
        }
        catch (Exception e) when (IsStorageError(e))
        {
            LastError = "History could not be cleared. " + e.Message;
            return false;
        }
    }

    /// <summary>Kiểm tra phiên bản, phát hiện file bị sửa bên ngoài và lưu bản sao file lỗi trước khi ghi.</summary>
    void PrepareWrite()
    {
        if (unsupportedSchema) throw new IOException("This file cannot safely be overwritten. Reload after resolving the read error.");
        // So mã băm để tránh ghi đè thay đổi do chương trình khác thực hiện sau lần nạp.
        if (Fingerprint(path) != loadedFingerprint) throw new IOException("The file changed outside this game. Reload it before saving.");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        if (preserveUnreadableFile && File.Exists(path))
        {
            string archive = path + ".unreadable-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N");
            File.Copy(path, archive, false);
        }
    }

    /// <summary>Ghi JSON vào file tạm cùng thư mục rồi thay thế file đích để hạn chế làm hỏng dữ liệu khi ghi thất bại.</summary>
    /// <param name="destination">Đường dẫn file cần ghi.</param>
    /// <param name="json">Nội dung JSON cần lưu.</param>
    /// <param name="retainBackup">Có giữ file đích cũ thành bản sao .bak hay không.</param>
    static void AtomicWrite(string destination, string json, bool retainBackup)
    {
        string temporary = destination + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            // Chỉ thay file gốc sau khi ghi xong file tạm; nếu ghi lỗi thì vẫn còn dữ liệu cũ.
            if (File.Exists(destination)) File.Replace(temporary, destination, retainBackup ? destination + ".bak" : null);
            else File.Move(temporary, destination);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                try { File.Delete(temporary); }
                catch (Exception e) when (IsStorageError(e)) { /* Lỗi dọn file tạm không làm mất file gốc. */ }
            }
        }
    }

    /// <summary>Đóng gói danh sách vào định dạng phiên bản 1 và chuyển thành JSON dễ đọc.</summary>
    /// <param name="entries">Các lượt chơi cần chuyển đổi.</param>
    /// <returns>Chuỗi JSON của lịch sử.</returns>
    static string Serialize(List<GameHistoryRecord> entries) => JsonUtility.ToJson(new Document { schemaVersion = 1, records = entries }, true);

    /// <summary>Kiểm tra kích thước, định dạng và từng bản ghi trước khi nhận dữ liệu từ file.</summary>
    /// <param name="file">Đường dẫn file cần đọc.</param>
    /// <param name="entries">Nhận danh sách đã chuẩn hóa khi đọc thành công.</param>
    /// <param name="error">Nhận mô tả lỗi khi đọc thất bại.</param>
    /// <param name="future">Nhận true nếu file có phiên bản mới hơn phiên bản hỗ trợ.</param>
    /// <returns>True nếu đọc và kiểm tra dữ liệu thành công.</returns>
    static bool TryRead(string file, out List<GameHistoryRecord> entries, out string error, out bool future)
    {
        entries = null; error = null; future = false;
        try
        {
            if (!File.Exists(file)) { error = "File not found."; return false; }
            if (new FileInfo(file).Length > 10 * 1024 * 1024) { error = "File exceeds the supported size."; return false; }
            string json = File.ReadAllText(file, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{")) { error = "Empty or invalid JSON."; return false; }
            var document = JsonUtility.FromJson<Document>(json);
            if (document == null || document.schemaVersion != 1)
            {
                future = document != null && document.schemaVersion > 1;
                error = "Unsupported history schema."; return false;
            }
            if (document.records == null || document.records.Any(r => !Valid(r))) { error = "Invalid history records."; return false; }
            entries = Normalize(document.records);
            return true;
        }
        catch (Exception e) when (IsStorageError(e)) { error = e.Message; return false; }
    }

    /// <summary>Gộp ID trùng, ưu tiên kết quả hoàn thành, sắp xếp mới nhất trước và giữ tối đa 100 bản ghi.</summary>
    /// <param name="entries">Các bản ghi cần chuẩn hóa.</param>
    /// <returns>Danh sách bản sao đã loại trùng, sắp xếp và giới hạn số lượng.</returns>
    static List<GameHistoryRecord> Normalize(IEnumerable<GameHistoryRecord> entries) => entries
        // Mỗi ID giữ một kết quả: ưu tiên hoàn thành, rồi đến thời điểm kết thúc mới hơn.
        .GroupBy(r => r.id)
        .Select(g => g.OrderByDescending(r => r.result == GameHistoryRecord.Completed).ThenByDescending(EndedAt).First())
        .OrderByDescending(EndedAt).ThenBy(r => r.id, StringComparer.Ordinal)
        .Take(MaximumRecords).Select(r => r.Copy()).ToList();

    /// <summary>Đọc thời điểm kết thúc có kèm độ lệch múi giờ từ bản ghi.</summary>
    /// <param name="record">Bản ghi có thời điểm kết thúc hợp lệ.</param>
    /// <returns>Thời điểm kết thúc dưới dạng DateTimeOffset.</returns>
    static DateTimeOffset EndedAt(GameHistoryRecord record) => DateTimeOffset.Parse(record.endedAt, CultureInfo.InvariantCulture, DateTimeStyles.None);
    /// <summary>Kiểm tra ID, màn chơi, điểm, thời lượng, kết quả và thời điểm có nằm trong phạm vi hỗ trợ.</summary>
    /// <param name="record">Bản ghi cần kiểm tra.</param>
    /// <returns>True nếu mọi trường bắt buộc đều hợp lệ.</returns>
    static bool Valid(GameHistoryRecord record) => record != null && Guid.TryParseExact(record.id, "N", out _) &&
        record.level >= 1 && record.level <= 4 && record.score >= 0 && record.totalScore >= 0 &&
        !double.IsNaN(record.playedSeconds) && !double.IsInfinity(record.playedSeconds) && record.playedSeconds >= 0 &&
        record.playedSeconds < TimeSpan.MaxValue.TotalSeconds - 1 &&
        (record.result == GameHistoryRecord.Completed || record.result == GameHistoryRecord.GameOver) &&
        DateTimeOffset.TryParse(record.endedAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    /// <summary>Tính mã SHA-256 của file để phát hiện thay đổi từ bên ngoài.</summary>
    /// <param name="file">Đường dẫn file cần kiểm tra.</param>
    /// <returns>Mã băm dạng Base64, hoặc null nếu file không tồn tại.</returns>
    static string Fingerprint(string file)
    {
        if (!File.Exists(file)) return null;
        using (var sha = SHA256.Create())
        using (var stream = File.OpenRead(file)) return Convert.ToBase64String(sha.ComputeHash(stream));
    }

    /// <summary>Nhận diện các ngoại lệ được xử lý như lỗi đọc, ghi hoặc đường dẫn.</summary>
    /// <param name="e">Ngoại lệ cần phân loại.</param>
    /// <returns>True nếu thuộc nhóm lỗi lưu trữ được hỗ trợ.</returns>
    static bool IsStorageError(Exception e) => e is IOException || e is UnauthorizedAccessException ||
        e is ArgumentException || e is NotSupportedException || e is System.Security.SecurityException;
}
