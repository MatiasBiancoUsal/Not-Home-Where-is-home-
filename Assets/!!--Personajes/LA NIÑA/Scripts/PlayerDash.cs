using UnityEngine;

public class PlayerDash : MonoBehaviour
{
    [Header("Dash")]
    public float dashForce; // variable que controla la fuerza del dash

    //timers
    public float dashDuration = 0.2f; // determina cuanto tiempo tarda un dash
    public float coolDownDash = 2f; // evitar spawneo
    public bool isDash = false;
    private float timerDashDuration = 0f; // ooldown o timer que verifica el dashDuration
    private float timerCoolDownDash = 0f; // timer que verifica el coolDownDash

    [Header("Flote post-dash en el aire")]
    [Tooltip("Segundos que el player queda flotando luego de un dash EN EL AIRE, antes de empezar a caer. 0 = sin flote.")]
    public float floteDuracion = 1f;
    private bool isFloating = false;
    private float timerFlote = 0f;

    private Vector2 dashDirection;
    private PlayerController playerController;
    private PlayerAudio playerAudio;
    private CollisionDetectionMode2D collisionDetectionAnterior;

    //Getters
    public bool IsDash // para saber si estamos dasheando
    {
        get { return isDash; }
    }
    public bool IsFloating // por si las animaciones quieren saber si esta flotando
    {
        get { return isFloating; }
    }

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerAudio = GetComponent<PlayerAudio>();
    }

    public void OnUpdate()
    {
        if (timerCoolDownDash >= 0f)
        {
            timerCoolDownDash -= Time.fixedDeltaTime;
        }

        if (isDash)
        {
           DashUpdate();
        }
        else if (isFloating)
        {
            FloteUpdate();
        }
    }

    //desde este metodo vamos a verificar los timers y la direccion del dash
    void DashStart()
    {
        isDash = true;
        timerDashDuration = dashDuration;
        timerCoolDownDash = coolDownDash;
        playerController.rb.gravityScale = 0f;
        playerAudio?.ReproducirDash();

        // El dash alcanza velocidades altas. Continuous evita atravesar colliders
        // finos antes de que Unity llegue a emitir el callback de colision.
        collisionDetectionAnterior = playerController.rb.collisionDetectionMode;
        playerController.rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        //----- verificar direccion del dash -------//

        // vector donde se almacena el valor del movimiento del player que se recibe desde el playerController
        Vector2 move = playerController.controles.Player.Move.ReadValue<Vector2>();

        if (move.x > 0.5f && move.y > 0.5f) // Revisa si la direccion del input es hacia arriba y en diagonal derecha
        {
            dashDirection = new Vector2(1, 1).normalized;
        }
        else if (move.x < -0.5f && move.y > 0.5f) // Revisa si la direccion del input es hacia arriba y en diagonal izquierda
        {
            dashDirection = new Vector2(-1, 1).normalized;
        }
        else if (move.x > 0.5f && move.y < -0.5f) // Revisa si la direccion del input es hacia abajo y en diagonal derecha
        {
            dashDirection = new Vector2(1, -1).normalized;
        }
        else if (move.x < -0.5f && move.y < -0.5f) // Revisa si la direccion del input es hacia abajo y en diagonal izquierda
        {
            dashDirection = new Vector2(-1, -1).normalized;
        }
        else if (move.y > 0.5) // Revisa si la direccion del input es hacia arriba
        {
            dashDirection = Vector2.up;
        }
        else if (move.y < -0.5) // Revisa si la direccion del input es hacia abajo
        {
            dashDirection = Vector2.down;
        }
        else if (move.x > 0.5) // Revisa si la direccion del input es hacia la derecha
        {
            dashDirection = Vector2.right;
        }
        else if (move.x < -0.5) // Revisa si la direccion del input es hacia la izquierda
        {
            dashDirection = Vector2.left;
        }
        else // verificar si hacer el dash cuando no hay direccion de movimiento
        {
            // si no hay direccion para el dash entonces por defecto se mueve hacia la direccion que mira el player
            if (playerController.movement.IsFacingRight)
            {
                dashDirection = Vector2.right;
            }
            else
            {
                dashDirection = Vector2.left;
            }

        }


    }

    void DashUpdate()
    {
        // Terminamos antes de volver a imponer la velocidad. Antes isDash pasaba a
        // false en este mismo FixedUpdate pero el Rigidbody todavia avanzaba un frame
        // a velocidad de dash; los bloques tocados en ese frame no se rompian.
        timerDashDuration -= Time.fixedDeltaTime;
        if (timerDashDuration <= 0f)
        {
            DashEnd();
            return;
        }

        // aplicamos la velocidad del dash al rb del player
        playerController.rb.linearVelocity = dashDirection * dashForce;
    }

    void DashEnd()
    {
        isDash = false;
        playerController.rb.collisionDetectionMode = collisionDetectionAnterior;

        // Si el dash termino EN EL AIRE, flotamos un momento antes de caer (dash mas estrategico).
        if (!playerController.jump.IsGrounded && floteDuracion > 0f)
        {
            isFloating = true;
            timerFlote = floteDuracion;
            playerController.rb.gravityScale = 0f;              // sin gravedad: no cae
            playerController.rb.linearVelocity = Vector2.zero;  // queda suspendido en el aire
        }
        else
        {
            playerController.rb.gravityScale = playerController.normalGravity; // en el piso: gravedad normal
        }
    }

    void FloteUpdate()
    {
        // si tocamos el piso durante el flote, lo cortamos.
        if (playerController.jump.IsGrounded)
        {
            EndFlote();
            return;
        }

        // anulamos solo la caida vertical; el movimiento horizontal sigue libre (para reposicionarse).
        Vector2 v = playerController.rb.linearVelocity;
        v.y = 0f;
        playerController.rb.linearVelocity = v;

        timerFlote -= Time.fixedDeltaTime;
        if (timerFlote <= 0f)
        {
            EndFlote();
        }
    }

    void EndFlote()
    {
        isFloating = false;
        playerController.rb.gravityScale = playerController.normalGravity; // ahora si, empieza a caer
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryBreakDashBlock(GetOtherCollider(collision));
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Si el player ya estaba tocando el bloque al iniciar el dash, no se genera
        // otro Enter. Stay permite que ese bloque tambien responda a la habilidad.
        TryBreakDashBlock(GetOtherCollider(collision));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryBreakDashBlock(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryBreakDashBlock(other);
    }

    private Collider2D GetOtherCollider(Collision2D collision)
    {
        if (PerteneceAlPlayer(collision.collider)) return collision.otherCollider;
        return collision.collider;
    }

    private bool PerteneceAlPlayer(Collider2D candidate)
    {
        return candidate != null && candidate.GetComponentInParent<PlayerController>() == playerController;
    }

    private void TryBreakDashBlock(Collider2D other)
    {
        if (!isDash || other == null) return;

        Transform objetoConTag = FindTaggedAncestor(other.transform, "Dash");
        if (objetoConTag == null) return;

        // ObjetoRompible sigue siendo opcional: si existe conserva su efecto visual;
        // si falta, el propio tag Dash alcanza para romper el objeto marcado.
        ObjetoRompible rompible = other.GetComponentInParent<ObjetoRompible>();
        if (rompible != null)
        {
            rompible.Romper();
        }
        else
        {
            Destroy(objetoConTag.gameObject);
        }
    }

    private static Transform FindTaggedAncestor(Transform current, string requiredTag)
    {
        while (current != null)
        {
            if (current.CompareTag(requiredTag)) return current;
            current = current.parent;
        }

        return null;
    }

    private void OnDisable()
    {
        if (playerController == null) return;

        if (isDash)
        {
            playerController.rb.collisionDetectionMode = collisionDetectionAnterior;
        }

        if (isDash || isFloating)
        {
            playerController.rb.gravityScale = playerController.normalGravity;
        }

        isDash = false;
        isFloating = false;
    }

    public void DashHold() // se llama cuando el juego detecta que se presiono la tecla de dash
    {
        if (!playerController.TieneHabilidad(PlayerController.Habilidad.Dash)) return;

        // OJO: el cooldown se chequea con timerCoolDownDash (NO con timerDashDuration).
        if (!isDash && !isFloating && timerCoolDownDash <= 0f) // no si estamos dasheando/flotando y el cooldown termino
        {
            DashStart();
        }
    }
}
