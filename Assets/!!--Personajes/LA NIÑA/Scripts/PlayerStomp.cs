using UnityEngine;

public class PlayerStomp : MonoBehaviour
{
    private PlayerController playerController;
    private Collider2D playerCollider;
    private CollisionDetectionMode2D collisionDetectionAnterior;

    [Header("Stomp")]
    public float stompForce = 20f; // fuerza con la que cae hacia abajo
    [Tooltip("Fuerza del REBOTE (pogo) hacia arriba al impactar. Chico, para un envion corto que se encadene con el doble salto.")]
    public float pogoForce = 7f;
    public float impulseUpForce = 5f; // fuerza del mini impulso hacia arriba antes de hacer stomp
    public float timeImpulseUp = 0.5f; // tiempo que dura el impulso hacia arriba
    private float counterImpulseUp = 0f; // timer del impulso hacia arriba

    [Tooltip("Ventana (seg) tras impactar en la que un ObjetoRompible con tag 'Stomp' todavia cuenta como golpeado. Cubre el desfasaje entre el chequeo de piso y la colision fisica.")]
    public float graciaRompible = 0.2f;
    private float impactoTimer = 0f;

    private bool isImpulse;
    private bool isStomp = false; // variable que indica si estamos haciendo el stomp
    private bool enPogo = false;  // rebote post-impacto: no se puede re-stompear y el espacio hace doble salto

    //Getter para que la animacion sepa si estamos en pleno stomp (impulso arriba + caida)
    public bool IsStomping => isStomp;
    // Igual que IsStomping pero queda true un ratito DESPUES de impactar. Lo usa ObjetoRompible:
    // el chequeo de piso corta el stomp un instante antes de que se dispare la colision fisica.
    public bool IsStompImpacting => isStomp || impactoTimer > 0f;
    // True durante el rebote (pogo) post-stomp. Lo usa el PlayerController para que el espacio sea doble salto.
    public bool EnPogo => enPogo;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerCollider = GetComponent<Collider2D>();
    }

    public void OnUpdate()
    {
        if (impactoTimer > 0f) impactoTimer -= Time.fixedDeltaTime;

        // Si ya rebotamos y volvimos a tocar el piso (sin subir), termina el estado de pogo:
        // recien ahi se puede volver a stompear.
        if (enPogo && !isStomp && playerController.jump.IsGrounded && playerController.rb.linearVelocity.y <= 0.1f)
        {
            enPogo = false;
        }

        UpdateStomp();
    }

    void StartStomp()
    {
        isStomp = true;
        counterImpulseUp = 0f;
        isImpulse = true;

        // El descenso llega a ser muy rapido (40 u/s en el prefab). Con deteccion
        // discreta, Unity puede atravesar bloques finos sin emitir una colision.
        collisionDetectionAnterior = playerController.rb.collisionDetectionMode;
        playerController.rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        playerController.rb.linearVelocity = Vector2.zero; // resetear controles
        playerController.controles.Player.Move.Disable(); // desactivar controles movimiento
    }

    void UpdateStomp()
    {
        if (!isStomp) // que el metodo stomp solo se complete cuando sea cierto que isStomp = true
        {
            return;
        }

        counterImpulseUp += Time.fixedDeltaTime; //

        if (isImpulse)
        {
            // mientras dure el impulso forzamos la fuerza o velocidad hacia arriba
            playerController.rb.linearVelocity = Vector2.up * impulseUpForce;

            // si se acab� el tiempo del impulso
            if (counterImpulseUp >= timeImpulseUp)
            {
                isImpulse = false;
                counterImpulseUp = 0f;
            }

        }
        else // ya termin� el impulso y empezamos el stomp
        {
            // aplicar la fuerza o velocidad hacia abajo
            playerController.rb.linearVelocity = Vector2.down * stompForce;

            // si toca un objeto (por ahora solo el suelo) entonces termina el stomp
            if (playerController.jump.IsGrounded)
            {
                EndStomp();
            }
        }
    }

    void EndStomp()
    {
        if (!isStomp) return;

        CameraShaker.Instance?.ShakeStompImpact(); // sacudon por el impacto del stomp contra el piso

        impactoTimer = graciaRompible; // ventana para que el ObjetoRompible alcance a detectar la colision

        // REBOTE (pogo): pequeño envion hacia arriba al impactar, para poder encadenar un doble salto.
        playerController.rb.linearVelocity = Vector2.up * pogoForce;
        enPogo = true; // mientras dure el rebote NO se puede re-stompear (no spamear)

        playerController.controles.Player.Move.Enable(); // activar controles movimiento
        playerController.rb.collisionDetectionMode = collisionDetectionAnterior;
        isStomp = false; // termina el stomp
        isImpulse = false;
        counterImpulseUp = 0f; // resetea el timer del impulso hacia arriba antes de hacer stomp
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryBreakStompBlock(GetOtherCollider(collision), ImpactoDesdeArriba(collision));
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Cubre el caso en que el player ya estaba rozando el bloque cuando empezo
        // el stomp: en ese caso Unity no vuelve a mandar OnCollisionEnter2D.
        TryBreakStompBlock(GetOtherCollider(collision), ImpactoDesdeArriba(collision));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryBreakStompBlock(other, PlayerEstaEncima(other));
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryBreakStompBlock(other, PlayerEstaEncima(other));
    }

    private Collider2D GetOtherCollider(Collision2D collision)
    {
        // Normalmente collider es el otro y otherCollider es el propio, pero esta
        // comprobacion evita depender de ese orden y funciona con colliders hijos.
        if (PerteneceAlPlayer(collision.collider)) return collision.otherCollider;
        return collision.collider;
    }

    private bool PerteneceAlPlayer(Collider2D candidate)
    {
        return candidate != null && candidate.GetComponentInParent<PlayerController>() == playerController;
    }

    private bool ImpactoDesdeArriba(Collision2D collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contacto = collision.GetContact(i);
            Vector2 normalHaciaPlayer = PerteneceAlPlayer(collision.collider)
                ? -contacto.normal
                : contacto.normal;

            if (normalHaciaPlayer.y > 0.35f) return true;
        }

        Collider2D other = GetOtherCollider(collision);
        return PlayerEstaEncima(other);
    }

    private bool PlayerEstaEncima(Collider2D other)
    {
        return other != null && playerCollider != null &&
               playerCollider.bounds.center.y > other.bounds.center.y;
    }

    private void TryBreakStompBlock(Collider2D other, bool impactoDesdeArriba)
    {
        // En OnCollisionEnter2D la fisica ya puede haber llevado la velocidad vertical
        // a cero. El estado !isImpulse es la fuente confiable de que estamos cayendo.
        if (!isStomp || isImpulse || !impactoDesdeArriba || other == null)
        {
            return;
        }

        Transform objetoConTag = FindTaggedAncestor(other.transform, "Stomp");
        if (objetoConTag == null) return;

        // Si tiene ObjetoRompible respetamos su efecto visual. Para los bloques que
        // solo tienen el tag, el tag Stomp alcanza y se elimina el objeto marcado.
        ObjetoRompible rompible = other.GetComponentInParent<ObjetoRompible>();
        if (rompible != null)
        {
            rompible.Romper();
        }
        else
        {
            Destroy(objetoConTag.gameObject);
        }

        EndStomp();
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
        // Si el objeto se desactiva a mitad del stomp, no dejamos el Rigidbody en
        // modo continuo. PlayerController administra el estado general de los inputs.
        if (!isStomp || playerController == null) return;

        playerController.rb.collisionDetectionMode = collisionDetectionAnterior;
        isStomp = false;
        isImpulse = false;
    }

    public void StompHold()
    {
        if (!playerController.TieneHabilidad(PlayerController.Habilidad.Pisoton)) return;

        //condiciones para iniciar el stomp (no durante el rebote, asi no se spamea)
        if (!isStomp && !enPogo && !playerController.jump.IsGrounded && !playerController.climb.IsClimbing)
        {
            StartStomp();
        }
    }
}
