using UnityEngine;

public class AsteroidCollision : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 10;

    private bool hasHitPlayer = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHitPlayer)
            return;

        PlayerHealth playerHealth =
            collision.gameObject.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            playerHealth =
                collision.gameObject.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth == null)
            return;

        hasHitPlayer = true;

        playerHealth.TakeDamage(damage);

        Destroy(gameObject);
    }

    public int GetDamage()
    {
        return damage;
    }
}