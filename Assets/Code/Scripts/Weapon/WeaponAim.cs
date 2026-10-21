using UnityEngine;

/// <summary>Tự động bắn theo chu kỳ tại các nòng súng dựa trên cấu hình tàu.</summary>
public class WeaponAim : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform[] firePoints;
    [SerializeField] private float fireRate = 0.5f;

    private PlayerConfig playerConfig;
    private float nextFireTime;

    /// <summary>Lấy cấu hình tàu từ đối tượng cha và báo lỗi nếu thiếu.</summary>
    private void Awake()
    {
        playerConfig = GetComponentInParent<PlayerConfig>();

        if (playerConfig == null)
        {
            Debug.LogError(
                "WeaponAim: Player chưa có PlayerConfig!"
            );
        }
    }

    /// <summary>Thực hiện chu kỳ bắn tự động khi game cho phép cập nhật.</summary>
    private void Update()
    {
        if (!GameManager.CanAdvanceGameplay) return;
        AutoFire();
    }

    /// <summary>Bắn khi hết thời gian chờ rồi hẹn đợt kế tiếp theo fireRate.</summary>
    private void AutoFire()
    {
        if (Time.time < nextFireTime)
            return;

        Shoot();

        nextFireTime = Time.time + fireRate;
    }

    /// <summary>Tạo đạn tại số nòng phù hợp cấu hình, gán sát thương và phát âm thanh một lần cho cả loạt.</summary>
    private void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning(
                "WeaponAim: Chưa gán Bullet Prefab!"
            );
            return;
        }

        if (playerConfig == null)
            return;

        if (firePoints == null ||
            firePoints.Length == 0)
        {
            Debug.LogWarning(
                "WeaponAim: Chưa có FirePoint!"
            );
            return;
        }

        // Không tạo nhiều đạn hơn số nòng đã gán, dù cấu hình tàu yêu cầu nhiều hơn.
        int count = Mathf.Min(
            playerConfig.bulletCount,
            firePoints.Length
        );

        for (int i = 0; i < count; i++)
        {
            Transform firePoint = firePoints[i];

            GameObject bulletObject = Instantiate(
                bulletPrefab,
                firePoint.position,
                firePoint.rotation
            );

            Bullet bullet =
                bulletObject.GetComponent<Bullet>();

            if (bullet != null)
            {
                bullet.Initialize(
                    playerConfig.damage
                );
            }
        }

        // Cả loạt nhiều nòng chỉ phát một âm thanh bắn.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayShootSound();
        }
    }
}
