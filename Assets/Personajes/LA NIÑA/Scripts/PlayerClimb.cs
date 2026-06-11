using UnityEngine;

public class PlayerClimb : MonoBehaviour
{
    private PlayerController playerController;

    [Header("Trepar (Climb)")]
    public float climbSpeed = 3f;      // velocidad al subir/bajar por la pared
    public float jumpOffForce = 8f;    // impulso hacia arriba al saltar de la pared (con ESPACIO)

    [Header("Deteccion de pared")]
    public string wallTag = "Wall";        // pared trepable (solida, con este tag)
    public float wallCheckDistance = 0.5f; // que tan adelante mira la pared
    public float wallCheckRadius = 0.2f;
    public float bottomCheckOffset = 0.5f; // que tan abajo mira para frenar al llegar al fondo de la pared

    [Header("Collider al trepar")]
    public Vector2 climbColliderOffset = new Vector2(1.8f, -0.6f);
    public Vector2 climbColliderSize = new Vector2(2.5f, 8.8f);

    private Rigidbody2D rb;
    private CapsuleCollider2D capsule;
    private Vector2 normalColliderOffset; // valores originales del collider (se guardan al inicio)
    private Vector2 normalColliderSize;

    private bool isClimbing = false;
    private float verticalInput = 0f;     // W/S (lo usa la animacion)
    private float reStickTimer = 0f;      // evita re-pegarse justo despues de saltar de la pared

    //Getters
    public bool IsClimbing { get { return isClimbing; } }
    public bool IsClimbWalking { get { return isClimbing && Mathf.Abs(verticalInput) > 0.1f; } }

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        capsule = GetComponent<CapsuleCollider2D>();

        if (capsule != null)
        {
            normalColliderOffset = capsule.offset;
            normalColliderSize = capsule.size;
        }
    }

    public void OnUpdate()
    {
        if (reStickTimer > 0f)
        {
            reStickTimer -= Time.fixedDeltaTime;
        }

        if (isClimbing)
        {
            Climbing();
        }
        else
        {
            TryEnterClimb();
        }
    }

    // Si estamos en el AIRE y tocamos una pared adelante -> nos pegamos a ella
    private void TryEnterClimb()
    {
        if (reStickTimer > 0f) return;                 // recien saltamos de una pared
        if (playerController.jump.IsGrounded) return;  // solo en el aire
        if (!HayPared()) return;                       // tiene que haber pared adelante

        EnterClimb();
    }

    private void EnterClimb()
    {
        isClimbing = true;
        rb.bodyType = RigidbodyType2D.Kinematic; // controlamos la posicion a mano, sin que la fisica la trabe
        rb.gravityScale = 0f;             // se queda pegado, no se cae
        rb.linearVelocity = Vector2.zero;

        SetClimbCollider(true);           // collider de la pose de trepar

        playerController.doubleJump.ResetAirJumps(); // recarga el doble salto para cuando salte de la pared
    }

    private void Climbing()
    {
        // W/S -> subir / bajar
        verticalInput = playerController.controles.Player.Move.ReadValue<Vector2>().y;

        float vy = verticalInput * climbSpeed;

        // Tope ARRIBA: si quiere SUBIR pero ya no hay pared, lo frenamos.
        if (vy > 0f && !HayPared())
        {
            vy = 0f;
        }
        // Tope ABAJO: si quiere BAJAR pero ya no hay pared mas abajo, lo frenamos (no seguir bajando en el aire).
        if (vy < 0f && !HayPared(-bottomCheckOffset))
        {
            vy = 0f;
        }

        rb.linearVelocity = new Vector2(0f, vy); // pegado en X, solo se mueve en Y

        // si baja y toca el piso, sale del modo trepar
        if (playerController.jump.IsGrounded)
        {
            ExitClimb();
        }
    }

    // Se llama desde el PlayerController cuando se aprieta ESPACIO estando pegado a la pared
    public void JumpOffWall()
    {
        ExitClimb();
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpOffForce); // salto hacia arriba
        reStickTimer = 0.25f; // un ratito para no re-pegarnos al instante
    }

    private void ExitClimb()
    {
        isClimbing = false;
        verticalInput = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;            // volvemos a la fisica normal
        rb.gravityScale = playerController.normalGravity; // devolvemos la gravedad normal
        SetClimbCollider(false);                          // collider normal de vuelta
    }

    // Cambia el collider entre el normal y el de trepar
    private void SetClimbCollider(bool climbing)
    {
        if (capsule == null) return;

        if (climbing)
        {
            capsule.offset = climbColliderOffset;
            capsule.size = climbColliderSize;
        }
        else
        {
            capsule.offset = normalColliderOffset;
            capsule.size = normalColliderSize;
        }
    }

    // Busca una pared SOLIDA (no trigger) con el tag, adelante del player (segun hacia donde mira)
    private bool HayPared(float yOffset = 0f)
    {
        float dir = playerController.movement.IsFacingRight ? 1f : -1f;
        Vector2 checkPos = (Vector2)transform.position + new Vector2(dir * wallCheckDistance, yOffset);

        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, wallCheckRadius);
        foreach (Collider2D hit in hits)
        {
            if (!hit.isTrigger && hit.CompareTag(wallTag))
            {
                return true;
            }
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Vector2 checkPos = (Vector2)transform.position + new Vector2(wallCheckDistance, 0f);
        Gizmos.DrawWireSphere(checkPos, wallCheckRadius);
    }
}
