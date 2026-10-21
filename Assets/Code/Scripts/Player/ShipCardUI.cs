using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Hiển thị thông số một tàu và chuyển thao tác chọn đến màn hình chọn tàu.</summary>
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

    /// <summary>Đọc hình và chỉ số từ prefab, gắn thao tác nút chọn rồi đặt trạng thái ban đầu.</summary>
    /// <param name="prefab">Prefab tàu mà thẻ đại diện.</param>
    /// <param name="manager">Giao diện quản lý các thẻ và lựa chọn tàu.</param>
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

        // Gỡ callback cũ trước khi gắn lại để một lần nhấn chỉ xử lý chọn tàu một lần.
        selectButton.onClick.RemoveAllListeners();

        selectButton.onClick.AddListener(
            OnSelectClicked
        );

        SetSelected(false);
    }

    /// <summary>Chuyển yêu cầu chọn prefab của thẻ đến giao diện quản lý.</summary>
    private void OnSelectClicked()
    {
        if (selectShipUI == null)
            return;

        selectShipUI.SelectPlayer(
            playerPrefab
        );
    }

    /// <summary>Đổi nhãn và màu nút theo việc tàu trên thẻ có đang được chọn hay không.</summary>
    /// <param name="selected">True nếu thẻ đại diện cho tàu đang chọn.</param>
    public void SetSelected(bool selected)
    {
        if (selectButton != null)
            selectButton.GetComponent<ButtonTextState>()?.SetSelected(selected);

        if (selectButtonText == null)
            return;

        selectButtonText.text =
            selected
                ? "SELECTED"
                : "SELECT";
    }

    /// <summary>Cung cấp prefab mà thẻ đang hiển thị.</summary>
    /// <returns>Prefab tàu đã gán cho thẻ.</returns>
    public GameObject GetPlayerPrefab()
    {
        return playerPrefab;
    }
}
