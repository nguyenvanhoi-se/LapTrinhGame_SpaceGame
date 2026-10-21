using System;
using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>Hiển thị một lượt chơi với điểm, thời lượng, kết quả và thời điểm kết thúc.</summary>
public sealed class GameHistoryRowUI : MonoBehaviour
{
    [SerializeField] TMP_Text levelText;
    [SerializeField] TMP_Text scoreText;
    [SerializeField] TMP_Text timeText;
    [SerializeField] TMP_Text resultText;
    [SerializeField] TMP_Text dateText;

    /// <summary>Điền dữ liệu lượt chơi, định dạng thời lượng theo phút hoặc giờ và đổi thời điểm kết thúc sang giờ địa phương.</summary>
    /// <param name="record">Bản ghi lịch sử hợp lệ cần hiển thị.</param>
    public void Bind(GameHistoryRecord record)
    {
        levelText.text = "LEVEL " + record.level;
        scoreText.text = "SCORE: " + record.score;
        var duration = TimeSpan.FromSeconds(Math.Floor(record.playedSeconds));
        timeText.text = "TIME: " + (duration.TotalHours >= 1
            ? ((int)duration.TotalHours).ToString("00") + duration.ToString(@"\:mm\:ss")
            : ((int)duration.TotalMinutes).ToString("00") + duration.ToString(@"\:ss"));
        resultText.text = "RESULT: " + record.result;
        var endedAt = DateTimeOffset.Parse(record.endedAt, CultureInfo.InvariantCulture);
        dateText.text = "DATE: " + endedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
    }
}
