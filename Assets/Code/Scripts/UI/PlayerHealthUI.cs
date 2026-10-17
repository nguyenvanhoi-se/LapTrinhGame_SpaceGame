using TMPro;
using UnityEngine;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private TMP_Text hpText;

    private PlayerHealth playerHealth;

    private void Start()
    {
        FindPlayerHealth();

        UpdateHP();
    }

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