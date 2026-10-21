using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Xử lý các nút điều hướng và thoát ở menu chính.</summary>
public class MainMenuUI : MonoBehaviour
{
    /// <summary>Bắt đầu game bằng cách tải scene Level1.</summary>
    public void StartGame()
    {
        SceneManager.LoadScene("Level1");
    }

    /// <summary>Mở scene chọn tàu.</summary>
    public void SelectShip()
    {
        SceneManager.LoadScene("SelectShip");
    }

    /// <summary>Mở scene chọn màn.</summary>
    public void SelectLevel()
    {
        SceneManager.LoadScene("SelectLevel");
    }

    /// <summary>Mở scene hướng dẫn chơi.</summary>
    public void Instructions()
    {
        SceneManager.LoadScene("Instructions");
    }

    /// <summary>Ghi thông báo và yêu cầu thoát ứng dụng.</summary>
    public void QuitGame()
    {
        Debug.Log("QUIT GAME");

        Application.Quit();
    }
}