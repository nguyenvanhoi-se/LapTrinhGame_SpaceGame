using UnityEngine;

/// <summary>Quản lý một vụ nổ gồm các hệ hạt và tự hủy khi tất cả hạt đã kết thúc.</summary>
[DisallowMultipleComponent]
public sealed class AsteroidExplosion : MonoBehaviour
{
    [SerializeField] private ParticleSystem flash;
    [SerializeField] private ParticleSystem fragments;
    [SerializeField] private ParticleSystem sparks;
    [SerializeField] private ParticleSystem dust;

    private ParticleSystem[] systems;
    private bool started;

    /// <summary>Lưu danh sách các hệ hạt con để phát và kiểm tra thời điểm kết thúc.</summary>
    private void Awake()
    {
        systems = GetComponentsInChildren<ParticleSystem>();
    }

    /// <summary>Đặt kích thước, số hạt, màu mảnh vỡ và thứ tự hiển thị trước khi vụ nổ phát.</summary>
    /// <param name="size">Hệ số kích thước toàn hiệu ứng, tối thiểu 0,1.</param>
    /// <param name="fragmentCount">Số mảnh vỡ trong đợt phát.</param>
    /// <param name="sparkCount">Số tia lửa trong đợt phát.</param>
    /// <param name="tint">Màu ban đầu của mảnh vỡ.</param>
    /// <param name="saturation">Độ bão hòa màu truyền cho shader mảnh vỡ.</param>
    /// <param name="sortingLayer">Mã lớp hiển thị của thiên thạch gốc.</param>
    /// <param name="sortingOrder">Thứ tự vẽ cơ sở; chớp sáng và tia lửa được vẽ cao hơn một bậc.</param>
    public void Configure(float size, int fragmentCount, int sparkCount, Color tint, float saturation,
        int sortingLayer, int sortingOrder)
    {
        transform.localScale = Vector3.one * Mathf.Max(0.1f, size);
        SetBurst(fragments, fragmentCount);
        SetBurst(sparks, sparkCount);
        var main = fragments.main;
        main.startColor = tint;
        // Đổi độ bão hòa riêng cho vụ nổ này mà không sửa vật liệu dùng chung.
        var properties = new MaterialPropertyBlock();
        properties.SetFloat("_Saturation", saturation);
        fragments.GetComponent<ParticleSystemRenderer>().SetPropertyBlock(properties);
        foreach (var system in systems)
        {
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = sortingOrder + (system == flash || system == sparks ? 1 : 0);
        }
    }

    /// <summary>Cấu hình một đợt phát ngay lúc bắt đầu, với số hạt giới hạn từ 1 đến 128.</summary>
    /// <param name="system">Hệ hạt cần cấu hình.</param>
    /// <param name="count">Số hạt mong muốn.</param>
    private static void SetBurst(ParticleSystem system, int count)
    {
        var emission = system.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 128)) });
    }

    /// <summary>Phát các hệ hạt sau khi bên tạo hiệu ứng đã có cơ hội gọi Configure.</summary>
    private void Start()
    {
        // Chờ đến Start để bên tạo vụ nổ kịp đặt số hạt trước khi phát.
        foreach (var system in systems) system.Play(false);
        started = true;
    }

    /// <summary>Chờ tất cả hệ hạt hết hoạt động rồi hủy toàn bộ đối tượng vụ nổ.</summary>
    private void Update()
    {
        if (!started) return;
        foreach (var system in systems)
            if (system.IsAlive(false)) return;
        Destroy(gameObject);
    }
}
