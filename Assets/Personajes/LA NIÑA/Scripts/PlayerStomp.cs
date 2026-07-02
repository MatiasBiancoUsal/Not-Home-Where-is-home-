using UnityEngine;

public class PlayerStomp : MonoBehaviour
{
    private PlayerController playerController;

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

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
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
        CameraShaker.Instance?.ShakeStompImpact(); // sacudon por el impacto del stomp contra el piso

        impactoTimer = graciaRompible; // ventana para que el ObjetoRompible alcance a detectar la colision

        // REBOTE (pogo): pequeño envion hacia arriba al impactar, para poder encadenar un doble salto.
        playerController.rb.linearVelocity = Vector2.up * pogoForce;
        enPogo = true; // mientras dure el rebote NO se puede re-stompear (no spamear)

        playerController.controles.Player.Move.Enable(); // activar controles movimiento
        isStomp = false; // termina el stomp
        isImpulse = false;
        counterImpulseUp = 0f; // resetea el timer del impulso hacia arriba antes de hacer stomp
    }

    public void StompHold()
    {
        //condiciones para iniciar el stomp (no durante el rebote, asi no se spamea)
        if (!isStomp && !enPogo && !playerController.jump.IsGrounded && !playerController.climb.IsClimbing)
        {
            StartStomp();
        }
    }
}
