using UnityEngine;

public class WeaponAim : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform[] firePoints;
    [SerializeField] private float fireRate = 0.5f;

    private PlayerConfig playerConfig;
    private float nextFireTime;

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

    private void Update()
    {
        AutoFire();
    }

    private void AutoFire()
    {
        if (Time.time < nextFireTime)
            return;

        Shoot();

        nextFireTime = Time.time + fireRate;
    }

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

        // Phát âm thanh 1 lần cho mỗi lần bắn
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayShootSound();
        }
    }
}