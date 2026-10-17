using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectShipUI : MonoBehaviour
{
    [Header("Ship List")]
    [SerializeField] private ShipCardUI shipCardPrefab;
    [SerializeField] private Transform content;

    private ShipCardUI selectedCard;

    private void Start()
    {
        CreateShipCards();
    }

    private void CreateShipCards()
    {
        if (shipCardPrefab == null)
        {
            Debug.LogWarning(
                "Chưa gán Ship Card Prefab!"
            );
            return;
        }

        if (content == null)
        {
            Debug.LogWarning(
                "Chưa gán Content!"
            );
            return;
        }

        if (ShipManager.Instance == null)
        {
            Debug.LogWarning(
                "Chưa có ShipManager!"
            );
            return;
        }

        GameObject[] playerPrefabs =
            ShipManager.Instance.GetPlayerPrefabs();

        if (playerPrefabs == null ||
            playerPrefabs.Length == 0)
        {
            Debug.LogWarning(
                "ShipManager chưa có Player Prefab!"
            );
            return;
        }

        foreach (GameObject playerPrefab in playerPrefabs)
        {
            if (playerPrefab == null)
                continue;

            ShipCardUI card =
                Instantiate(
                    shipCardPrefab,
                    content
                );

            card.Setup(
                playerPrefab,
                this
            );

            if (
                ShipManager.Instance
                .GetSelectedPlayerPrefab()
                == playerPrefab
            )
            {
                selectedCard = card;
                card.SetSelected(true);
            }
        }
    }

    public void SelectPlayer(
        GameObject playerPrefab)
    {
        if (playerPrefab == null)
            return;

        if (selectedCard != null)
            selectedCard.SetSelected(false);

        ShipManager.Instance.SelectPlayer(
            playerPrefab
        );

        ShipCardUI[] cards =
            content.GetComponentsInChildren<
                ShipCardUI>();

        foreach (ShipCardUI card in cards)
        {
            if (card.GetPlayerPrefab()
                == playerPrefab)
            {
                selectedCard = card;
                selectedCard.SetSelected(true);
                break;
            }
        }
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}