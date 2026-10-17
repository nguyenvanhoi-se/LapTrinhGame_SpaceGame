using UnityEngine;

public class LevelStartUI : MonoBehaviour
{
    [SerializeField] private float showDuration = 2f;

    private void Start()
    {
        Invoke(nameof(Hide), showDuration);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}