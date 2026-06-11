using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 2f;
    public bool startMovingRight = false;

    [Header("Piso (para no caerse del borde)")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Pared / limite del enemigo (por TAG)")]
    public string wallTag = "Wall";

    private Rigidbody2D rb;

    // CAMBIADO: ahora el sprite base se considera mirando a la izquierda
    private bool movingRight = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (startMovingRight)
        {
            Flip();
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        rb.linearVelocity = new Vector2(
            movingRight ? speed : -speed,
            rb.linearVelocity.y
        );

        if (groundCheck != null)
        {
            bool hayPiso = Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            );

            if (!hayPiso)
            {
                Flip();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag(wallTag)) return;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (Mathf.Abs(contact.normal.x) > 0.5f)
            {
                Flip();
                return;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(wallTag))
        {
            Flip();
        }
    }

    void Flip()
    {
        movingRight = !movingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

        if (groundCheck != null)
        {
            Vector3 checkPos = groundCheck.localPosition;
            checkPos.x *= -1;
            groundCheck.localPosition = checkPos;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }
}