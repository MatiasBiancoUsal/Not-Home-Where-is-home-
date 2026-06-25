using UnityEngine;

public class PlayerClimb : MonoBehaviour
{
    private PlayerController playerController;

    [Header("Trepar (Climb)")]
    public float climbSpeed = 3f;      // velocidad al subir/bajar por la pared
    public float jumpOffForce = 8f;    // impulso hacia arriba al saltar de la pared (con ESPACIO)

    [Header("Salto de pared direccional")]
    [Tooltip("Impulso HORIZONTAL al saltar hacia el lado contrario a la pared (presionando A/D en contra).")]
    public float wallJumpHorizontalForce = 9f;
    [Tooltip("Segundos en que el movimiento NO pisa el envion horizontal del salto (para que se sienta el impulso).")]
    public float wallJumpLockTime = 0.2f;
    [Tooltip("Segundos sin poder re-pegarse a la pared despues de saltar.")]
    public float reStickTime = 0.25f;

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
    private float wallJumpLockTimer = 0f; // mientras corre, el movimiento no pisa el envion del salto de pared

    //Getters
    public bool IsClimbing { get { return isClimbing; } }
    public bool IsClimbWalking { get { return isClimbing && Mathf.Abs(verticalInput) > 0.1f; } }
    public bool IsWallJumpLocked => wallJumpLockTimer > 0f; // lo consulta el PlayerMovement

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
        if (wallJumpLockTimer > 0f)
        {
            wallJumpLockTimer -= Time.fixedDeltaTime;
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
        playerController.superJump.CancelSuperJump(); // el super salto termina al pegarse a la pared (sino su animacion queda al saltar de la pared)
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

    // Se llama desde el PlayerController cuando se aprieta ESPACIO estando pegado a la pared.
    // La direccion depende del input horizontal (A/D):
    //  - empuja CONTRA la pared          -> se queda pegado (no salta).
    //  - empuja para el lado CONTRARIO   -> salto con impulso horizontal hacia ese lado + arriba.
    //  - sin A/D (neutro)                -> salto recto hacia arriba.
    public void JumpOffWall()
    {
        float wallDir = playerController.movement.IsFacingRight ? 1f : -1f; // hacia donde esta la pared
        float inputX = playerController.controles.Player.Move.ReadValue<Vector2>().x;

        // Empuja CONTRA la pared (ej: pared a la izquierda y aprieta A): se queda pegado.
        if (inputX * wallDir > 0.3f)
        {
            return;
        }

        ExitClimb();

        float horizontal = 0f;
        if (inputX * wallDir < -0.3f) // empuja para el lado contrario a la pared
        {
            float awayDir = -wallDir;
            horizontal = awayDir * wallJumpHorizontalForce;
            wallJumpLockTimer = wallJumpLockTime;         // que el movimiento no pise el envion
            playerController.movement.SetFacing(awayDir); // que mire hacia donde salta
        }

        rb.linearVelocity = new Vector2(horizontal, jumpOffForce);
        reStickTimer = reStickTime; // un ratito para no re-pegarnos al instante
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
