using UnityEngine;

public class DestroyBelowScreen : MonoBehaviour
{
    [SerializeField] private float destroyY = -7f;

    private void Update()
    {
        if (transform.position.y < destroyY)
        {
            Destroy(gameObject);
        }
    }
}