using UnityEngine;

public class EnemyBall : MonoBehaviour
{
    public float speed = 5f;
    public float lifeTime = 5f;

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
}