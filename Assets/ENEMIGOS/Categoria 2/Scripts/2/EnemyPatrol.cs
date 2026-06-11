using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 2f;
    public bool startMovingRight = false; // por defecto arranca hacia la IZQUIERDA

    [Header("Piso (para no caerse del borde)")]
    public Transform groundCheck;          // hijo, adelante y ABAJO del monstruo
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;          // que considera "piso" (para no caerse de los bordes)

    [Header("Pared / limite del enemigo (por TAG)")]
    public string wallTag = "Wall";        // se da vuelta al tocar algo con este tag

    private Rigidbody2D rb;
    private bool movingRight = true;       // el prefab arranca mirando a la derecha (escala por defecto)

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Si queremos que arranque hacia la izquierda, lo damos vuelta una vez al inicio.
        if (!startMovingRight)
        {
            Flip();
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // Movimiento horizontal (mantiene su velocidad vertical)
        rb.linearVelocity = new Vector2(movingRight ? speed : -speed, rb.linearVelocity.y);

        // Si NO hay piso adelante -> dar vuelta (para no caerse del borde)
        if (groundCheck != null)
        {
            bool hayPiso = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            if (!hayPiso)
            {
                Flip();
            }
        }
    }

    // PARED SOLIDA: el enemigo choca de costado contra algo tagueado "Wall".
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag(wallTag)) return;

        // Solo si el choque es de COSTADO (pared), no si cae sobre un piso tagueado "Wall".
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (Mathf.Abs(contact.normal.x) > 0.5f)
            {
                Flip();
                return;
            }
        }
    }

    // LIMITE INVISIBLE (trigger): el player lo atraviesa, el enemigo se da vuelta.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(wallTag))
        {
            Flip();
        }
    }

    // Da vuelta al monstruo: cambia la direccion, espeja el sprite y mueve el groundCheck al frente.
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
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
