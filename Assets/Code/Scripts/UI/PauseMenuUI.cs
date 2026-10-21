using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Điều khiển giao diện tạm dừng và gửi yêu cầu đổi trạng thái đến GameManager.</summary>
[DisallowMultipleComponent]
public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;

    /// <summary>Đồng bộ bảng tạm dừng và trạng thái nút khi bắt đầu.</summary>
    private void Start() => Refresh();

    /// <summary>Đọc phím Escape hoặc P để đổi trạng thái tạm dừng, sau đó đồng bộ giao diện.</summary>
    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame))
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) Resume();
            else Pause();
        }
        Refresh();
    }

    /// <summary>Yêu cầu GameManager tạm dừng và cập nhật bảng điều khiển.</summary>
    public void Pause()
    {
        if (GameManager.Instance != null) GameManager.Instance.PauseGame();
        Refresh();
    }

    /// <summary>Yêu cầu GameManager tiếp tục chơi và cập nhật bảng điều khiển.</summary>
    public void Resume()
    {
        if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
        Refresh();
    }

    /// <summary>Khôi phục thời gian rồi tải lại scene chứa giao diện này.</summary>
    public void Restart()
    {
        RestoreTime();
        SceneManager.LoadScene(gameObject.scene.name);
    }

    /// <summary>Khôi phục thời gian rồi tải menu chính.</summary>
    public void MainMenu()
    {
        RestoreTime();
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>Giải phóng trạng thái tạm dừng qua GameManager, hoặc trực tiếp khôi phục thời gian và âm thanh nếu thiếu bộ quản lý.</summary>
    private void RestoreTime()
    {
        if (GameManager.Instance != null) GameManager.Instance.PrepareForSceneChange();
        else
        {
            if (Time.timeScale <= 0f) Time.timeScale = 1f;
            if (AudioManager.Instance != null) AudioManager.Instance.SetGameplayPaused(false);
        }
    }

    /// <summary>Đồng bộ nút tạm dừng, bảng và lựa chọn điều hướng với trạng thái của GameManager.</summary>
    private void Refresh()
    {
        var game = GameManager.Instance;
        bool paused = game != null && game.IsPaused;
        if (pauseButton != null)
            pauseButton.interactable = game != null && game.GetGameState() == GameManager.GameState.Playing;
        if (pausePanel == null || pausePanel.activeSelf == paused) return;

        pausePanel.SetActive(paused);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(paused ? resumeButton?.gameObject : pauseButton?.gameObject);
    }

    /// <summary>Tiếp tục game nếu giao diện bị tắt trong lúc đang tạm dừng để tránh giữ game bị đứng.</summary>
    private void OnDisable()
    {
        // Khi bảng bị tắt hoặc gỡ, tiếp tục game để không giữ timeScale ở 0.
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            GameManager.Instance.ResumeGame();
    }
}
