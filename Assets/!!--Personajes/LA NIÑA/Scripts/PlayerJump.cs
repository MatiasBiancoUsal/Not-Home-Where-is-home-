using UnityEngine;

public class PlayerJump : MonoBehaviour
{
    private PlayerController playerController;

    [Header("Variables Salto")]
    public float jumpForce;
    public float groundRadius;
    public float groundCheckDistance;
    public LayerMask groundMask;
    private bool isGrounded;

    [Header("Coyote Time")]
    public float coyoteTime = 0.5f;
    public float coyoteCounter = 0f;
    private bool hasJumped = false; // variable para verificar si el jugador ha saltado o no

    [Header("Buffer Jump")]
    public float bufferJumpTime = 0.5f;
    public float bufferJumpCounter = 0f;

    //Getters
    public bool IsGrounded 
    { 
        get { return isGrounded; }
    }

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    public void OnUpdate()
    {
        CheckGround();
        JumpUpdates();
    }

    // este metodo verifica si el player esta en contacto con un objeto o layer de tipo suelo
    public void CheckGround()
    {
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, groundRadius, Vector2.down, groundCheckDistance, groundMask);

        // verificar si el circle cast esta colisionando con la layer ground
        if (hit.collider != null)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    //se encarga de actualizar las mejoras del salto; gravedad dinamica, coyote time y buffer jump
    public void JumpUpdates()
    {
        // este IF ELSE verifica el coyote time
        if (isGrounded)
        {
            coyoteCounter = coyoteTime; // reiniciar el contador de coyote time
            hasJumped = false; // reiniciar la variable de salto
        }
        else
        {
            coyoteCounter -= Time.fixedDeltaTime; // reducir el contador de coyote time
        }

        // este IF ELSE verifica el buffer jump
        if (bufferJumpCounter > 0)
        {
            bufferJumpCounter -= Time.fixedDeltaTime;

            //si tocamos el suelo y hay un buffer activo, entonces salto autom�tico
            if (isGrounded)
            {
                playerController.rb.linearVelocity = new Vector2(playerController.rb.linearVelocity.x, jumpForce);
                //ajustar la gravedad normal para el personaje
                playerController.rb.gravityScale = playerController.normalGravity;
                coyoteCounter = 0;
                bufferJumpCounter = 0;
            }
        }

        // este IF ELSE actualiza la gravedad del personaje
        if (isGrounded)
        {
            playerController.rb.gravityScale = playerController.normalGravity;
        }

        else if (playerController.rb.linearVelocity.y < -0.1) // en caso de que estemos en caida
        {
            playerController.rb.gravityScale = playerController.fallGravity;
        }
    }

    public void JumpHold()
    {
        if (CanGroundJump()) //realizamos un salto
        {
            playerController.rb.linearVelocity = new Vector2(playerController.rb.linearVelocity.x, jumpForce);
            //ajustar la gravedad normal para el personaje
            playerController.rb.gravityScale = playerController.normalGravity;
            coyoteCounter = 0;
            hasJumped = true; // marcar que el jugador ha saltado
        }
        else //no se ha realizado un salto
        {
            BufferJump();
        }
    }

    // ¿hay un salto desde el piso o coyote disponible? El PlayerController lo usa para
    // decidir si hace un salto normal o uno en el aire (doble salto). Es la MISMA condicion de arriba.
    public bool CanGroundJump()
    {
        return isGrounded || coyoteCounter > 0 && !hasJumped;
    }

    // activa el buffer del salto (lo guarda para ejecutarlo apenas toquemos el piso)
    public void BufferJump()
    {
        bufferJumpCounter = bufferJumpTime;
    }

    public void JumpRelease()
    {
        // si esta subiendo y el jugador suelta el boton de salto, se reduce la velocidad de salto
        if (playerController.rb.linearVelocity.y > 0)
        {
            playerController.rb.linearVelocity = new Vector2(playerController.rb.linearVelocity.x, playerController.rb.linearVelocity.y * 0.5f);
        }
        playerController.rb.gravityScale = playerController.fallGravity;
        hasJumped = true; // marcar que el jugador ha saltado
    }

    private void OnDrawGizmos()
    {
        if (isGrounded)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.red;
        }

        Vector3 checkPosition = transform.position + Vector3.down * groundCheckDistance;
        Gizmos.DrawWireSphere(checkPosition, groundRadius);
    }
}
