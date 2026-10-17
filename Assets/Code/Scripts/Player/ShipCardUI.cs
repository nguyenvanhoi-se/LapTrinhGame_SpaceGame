using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShipCardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image shipImage;
    [SerializeField] private TMP_Text shipNameText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private Button selectButton;
    [SerializeField] private TMP_Text selectButtonText;

    private GameObject playerPrefab;
    private SelectShipUI selectShipUI;

    public void Setup(
        GameObject prefab,
        SelectShipUI manager)
    {
        playerPrefab = prefab;
        selectShipUI = manager;

        if (playerPrefab == null)
            return;

        PlayerConfig config =
            playerPrefab.GetComponent<PlayerConfig>();

        SpriteRenderer spriteRenderer =
            playerPrefab.GetComponent<SpriteRenderer>();

        if (config == null)
        {
            Debug.LogWarning(
                playerPrefab.name +
                " chưa có PlayerConfig!"
            );
            return;
        }

        // Hình tàu
        if (shipImage != null &&
            spriteRenderer != null)
        {
            shipImage.sprite =
                spriteRenderer.sprite;
        }

        // Thông tin tàu
        shipNameText.text =
            config.playerName;
        shipNameText.alignment = TextAlignmentOptions.Center;

        hpText.text =
            "HP: " + config.maxHP;
        shipNameText.alignment = TextAlignmentOptions.Center;

        speedText.text =
            "SPEED: " + config.moveSpeed;
        shipNameText.alignment = TextAlignmentOptions.Center;

        damageText.text =
            "DAMAGE: " + config.damage;
        shipNameText.alignment = TextAlignmentOptions.Center;

        // Button
        selectButton.onClick.RemoveAllListeners();

        selectButton.onClick.AddListener(
            OnSelectClicked
        );

        SetSelected(false);
    }

    private void OnSelectClicked()
    {
        if (selectShipUI == null)
            return;

        selectShipUI.SelectPlayer(
            playerPrefab
        );
    }

    public void SetSelected(bool selected)
    {
        if (selectButtonText == null)
            return;

        selectButtonText.text =
            selected
                ? "SELECTED"
                : "SELECT";
    }

    public GameObject GetPlayerPrefab()
    {
        return playerPrefab;
    }
}