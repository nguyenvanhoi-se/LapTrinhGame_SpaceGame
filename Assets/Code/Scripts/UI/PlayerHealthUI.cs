using TMPro;
using UnityEngine;

/// <summary>Hiển thị máu hiện tại và máu tối đa của tàu.</summary>
public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private TMP_Text hpText;

    private PlayerHealth playerHealth;

    /// <summary>Tìm thành phần máu người chơi và hiển thị giá trị ban đầu.</summary>
    private void Start()
    {
        FindPlayerHealth();

        UpdateHP();
    }

    /// <summary>Tìm PlayerHealth trong scene và cảnh báo nếu tàu chưa xuất hiện.</summary>
    private void FindPlayerHealth()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning(
                "PlayerHealthUI: Chưa tìm thấy PlayerHealth!"
            );
        }
    }

    /// <summary>Tìm lại tàu khi cần rồi cập nhật nhãn máu hiện tại trên máu tối đa.</summary>
    public void UpdateHP()
    {
        if (playerHealth == null)
        {
            FindPlayerHealth();
        }

        if (playerHealth == null)
            return;

        if (hpText == null)
        {
            Debug.LogWarning(
                "PlayerHealthUI: Chưa gán HP Text!"
            );
            return;
        }

        hpText.text =
            "HP: " +
            playerHealth.GetCurrentHealth() +
            "/" +
            playerHealth.GetMaxHealth();
    }
}