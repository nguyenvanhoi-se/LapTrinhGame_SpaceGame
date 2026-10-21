using UnityEngine;

/// <summary>Tạo hiệu ứng nổ khi thiên thạch hết máu hoặc va vào tàu người chơi.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Health), typeof(SpriteRenderer))]
public sealed class AsteroidDeathVfx : MonoBehaviour
{
    [SerializeField] private AsteroidExplosion explosionPrefab;
    [SerializeField, Min(1)] private int fragmentCount = 7;
    [SerializeField, Min(1)] private int sparkCount = 12;
    [SerializeField] private Color fragmentTint = Color.white;
    [SerializeField, Range(0, 1)] private float fragmentSaturation = 1;

    private bool hasPlayed;

    /// <summary>Tạo vụ nổ một lần tại vị trí thiên thạch, điều chỉnh kích thước và thứ tự vẽ theo sprite gốc.</summary>
    public void PlayExplosion()
    {
        if (hasPlayed || explosionPrefab == null) return;
        hasPlayed = true;

        var source = GetComponent<SpriteRenderer>();
        float diameter = Mathf.Max(source.bounds.size.x, source.bounds.size.y);
        var explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        // 0,486 là đường kính tham chiếu của hiệu ứng để đổi kích thước theo thiên thạch gốc.
        explosion.Configure(diameter / 0.486f, fragmentCount, sparkCount,
            fragmentTint, fragmentSaturation, source.sortingLayerID, source.sortingOrder + 1);
    }
}
