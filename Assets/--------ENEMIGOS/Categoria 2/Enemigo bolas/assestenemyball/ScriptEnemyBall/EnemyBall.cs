using UnityEngine;

public class EnemyBall : MonoBehaviour
{
    public float speed = 5f;
    public float lifeTime = 5f;

    [Header("Paredes")]
    public string wallTag = "Wall"; // al tocar un objeto con este tag, la bala se frena y desaparece

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetDirection(Vector2 direction)
    {
        rb.linearVelocity = direction.normalized * speed;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // si la bala toca una pared, se frena y desaparece ahi (no la traspasa).
        // asi el player puede resguardarse detras de las paredes.
        if (other.CompareTag(wallTag))
        {
            rb.linearVelocity = Vector2.zero;
            Destroy(gameObject);
        }
    }
}
