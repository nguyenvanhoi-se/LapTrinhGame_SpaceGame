using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Xử lý điều hướng từ màn hình hướng dẫn về menu chính.</summary>
public class InstructionsUI : MonoBehaviour
{
    /// <summary>Tải menu chính từ màn hình hướng dẫn.</summary>
    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}