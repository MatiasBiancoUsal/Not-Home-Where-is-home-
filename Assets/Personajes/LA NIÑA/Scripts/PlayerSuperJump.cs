using UnityEngine;

// ============================================================
//  SUPER SALTO (con carga)
//  En el PISO, mantener W (arriba) arranca la PREPARACION:
//    1) animacion de INICIO (una vez)  ->  2) LOOP DE ESPERA (mientras sostenes)
//  Cuanto mas tiempo se sostiene, mas alto sera el salto (hasta un maximo).
//  - Si se presiona ESPACIO con carga suficiente            -> SUPER salto.
//  - Si se presiona ESPACIO MUY RAPIDO (carga < minimo)     -> salto NORMAL
//    (TryExecute devuelve false y el PlayerController hace el salto normal).
//  - Si se VENCE el tiempo (tiempoLimite)                   -> animacion de SALIDA (caduca) y vuelve a idle.
//  - Si se SUELTA W o se queda sin piso                     -> se cancela seco (sin animacion de salida).
//  Al ejecutar, sacude la camara por la potencia del salto (mas carga = mas fuerte).
//  Durante el super salto, el ESPACIO en el aire hace un DOBLE salto (lo maneja el PlayerController).
//
//  Despues de saltar o de que caduque hay que SOLTAR W para volver a cargar.
//
//  Va EN el player (mismo GameObject que PlayerController).
// ============================================================
public class PlayerSuperJump : MonoBehaviour
{
    private PlayerController playerController;

    [Header("Fuerza del salto")]
    [Tooltip("Fuerza (altura) del salto con la carga MINIMA: apenas empezas a cargar y saltas.")]
    public float fuerzaMinima = 12f;
    [Tooltip("Fuerza (altura) del salto a carga COMPLETA. Esto define 'que tan alto' puede saltar.")]
    public float fuerzaMaxima = 28f;

    [Header("Tiempos")]
    [Tooltip("Tiempo MINIMO de carga para que cuente como super salto. Si presionas espacio antes de esto (W + espacio muy rapido), se hace un salto NORMAL.")]
    public float tiempoMinimoParaSuperSalto = 0.15f;
    [Tooltip("Segundos sosteniendo W para llegar a la altura MAXIMA.")]
    public float tiempoCargaMaxima = 1f;
    [Tooltip("Segundos TOTALES que dura la preparacion. Si no saltas antes de esto, CADUCA. Debe ser >= tiempoCargaMaxima.")]
    public float tiempoLimite = 1.5f;
    [Tooltip("Duracion de la animacion de INICIO antes de pasar al loop de espera. Igualalo al largo del clip de inicio.")]
    public float duracionIntro = 0.25f;
    [Tooltip("Duracion de la animacion de SALIDA (cuando caduca). Igualalo al largo del clip de salida.")]
    public float duracionCaduca = 0.4f;

    [Header("Activacion")]
    [Tooltip("Cuanto hay que inclinar W (arriba) para considerar que se 'mantiene presionado'.")]
    public float umbralW = 0.5f;
    [Tooltip("Si esta activo, el player queda quieto (sin moverse en horizontal) mientras carga.")]
    public bool bloquearMovimientoAlCargar = true;

    // estado interno
    private bool isPreparing;     // en preparacion (inicio + loop)
    private bool isSuperJumping;  // en pleno super salto (en el aire, hasta aterrizar)
    private bool isExpiring;      // reproduciendo la animacion de salida (caduca)
    private bool waitingRelease;  // hay que soltar W antes de poder volver a cargar
    private float chargeTimer;    // cuanto venimos cargando
    private float expireTimer;    // cuanto queda de la animacion de salida

    // Getters (los usan las animaciones y el PlayerController)
    public bool IsPreparing => isPreparing;
    public bool IsWaitingLoop => isPreparing && chargeTimer >= duracionIntro; // ya paso el inicio: esta en el loop
    public bool IsExpiring => isExpiring;
    public bool IsSuperJumping => isSuperJumping;
    // De 0 a 1: cuanta carga lleva acumulada. Sirve, por ejemplo, para una barra de carga a futuro.
    public float ChargeRatio => Mathf.Clamp01(chargeTimer / tiempoCargaMaxima);

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    public void OnUpdate()
    {
        // Si veniamos de un super salto y ya aterrizamos (en el piso y sin subir), lo damos por terminado.
        if (isSuperJumping && playerController.jump.IsGrounded && playerController.rb.linearVelocity.y <= 0.1f)
        {
            isSuperJumping = false;
        }

        // Contador de la animacion de salida (caduca).
        if (isExpiring)
        {
            expireTimer -= Time.fixedDeltaTime;
            if (expireTimer <= 0f) isExpiring = false;
        }

        float holdUp = playerController.controles.Player.Move.ReadValue<Vector2>().y;

        // Una vez que soltamos W, se vuelve a habilitar la carga.
        if (waitingRelease && holdUp < umbralW) waitingRelease = false;

        if (isPreparing)
        {
            // Soltar W o quedarse sin piso: cancela seco, SIN animacion de salida.
            if (holdUp < umbralW || !playerController.jump.IsGrounded)
            {
                isPreparing = false;
                chargeTimer = 0f;
                return;
            }

            chargeTimer += Time.fixedDeltaTime;

            // Se vencio el tiempo: CADUCA -> dispara la animacion de salida.
            if (chargeTimer >= tiempoLimite)
            {
                isPreparing = false;
                chargeTimer = 0f;
                isExpiring = true;
                expireTimer = duracionCaduca;
                waitingRelease = true; // hay que soltar W para volver a cargar
                return;
            }

            // Mientras carga lo mantenemos quieto (corre DESPUES del movimiento, asi pisa la velocidad horizontal).
            if (bloquearMovimientoAlCargar)
            {
                playerController.rb.linearVelocity = new Vector2(0f, playerController.rb.linearVelocity.y);
            }
        }
        else if (!isSuperJumping && !isExpiring)
        {
            // Arrancamos la preparacion: en el piso, manteniendo W, y habiendo soltado desde la ultima vez.
            if (playerController.jump.IsGrounded && holdUp >= umbralW && !waitingRelease)
            {
                isPreparing = true;
                chargeTimer = 0f;
            }
        }
    }

    // Lo llama el PlayerController al presionar ESPACIO mientras cargamos.
    // Devuelve TRUE si ejecuto el super salto; FALSE si la carga fue insuficiente
    // (en ese caso cancela la preparacion y el PlayerController hace un salto normal).
    public bool TryExecute()
    {
        // Carga insuficiente (W + espacio muy rapido): NO es super salto, dejamos paso al salto normal.
        if (chargeTimer < tiempoMinimoParaSuperSalto)
        {
            isPreparing = false;
            chargeTimer = 0f;
            waitingRelease = true; // soltar W antes de volver a cargar
            return false;
        }

        float ratio = ChargeRatio;
        float fuerza = Mathf.Lerp(fuerzaMinima, fuerzaMaxima, ratio);

        playerController.rb.linearVelocity = new Vector2(playerController.rb.linearVelocity.x, fuerza);
        playerController.rb.gravityScale = playerController.normalGravity;

        isPreparing = false;
        isSuperJumping = true;
        chargeTimer = 0f;
        waitingRelease = true; // soltar W antes de volver a cargar (evita recargar solo al aterrizar)

        // Sacudon de camara por la potencia del salto (mas carga = mas fuerte).
        CameraShaker.Instance?.ShakeSuperJump(Mathf.Lerp(0.5f, 1f, ratio));

        return true;
    }

    // Termina el estado de super salto. Lo llama el climb al pegarse a una pared: asi, al saltar
    // de la pared, se usa la animacion de salto normal y NO queda la del super salto.
    public void CancelSuperJump()
    {
        isSuperJumping = false;
    }

    // Evita malos seteos en el Inspector.
    private void OnValidate()
    {
        if (tiempoLimite < tiempoCargaMaxima) tiempoLimite = tiempoCargaMaxima;
        if (duracionIntro > tiempoLimite) duracionIntro = tiempoLimite;
        tiempoMinimoParaSuperSalto = Mathf.Clamp(tiempoMinimoParaSuperSalto, 0f, tiempoCargaMaxima);
    }
}
