using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

// ============================================================
//  MIRAR HACIA ABAJO
//
//  Manteniendo S (o la flecha abajo) con la niña QUIETA en el piso, la camara baja
//  de a poco para que el jugador pueda ver que hay mas abajo antes de tirarse: si hay
//  pinchos, una plataforma o el vacio.
//
//  Al soltar la tecla, o al moverse, saltar o recibir un golpe, la camara vuelve sola
//  a su lugar.
//
//  Va EN el mismo objeto de la camara que tiene el Cinemachine Position Composer
//  (la CM FollowPlayer), al lado del CameraFallCatchUp.
// ============================================================
[RequireComponent(typeof(CinemachinePositionComposer))]
public class CamaraMirarAbajo : MonoBehaviour
{
    [Header("Cuanto baja")]
    [Tooltip("Cuantas unidades del mundo baja la camara como maximo.")]
    [Min(0f)] public float distancia = 4.5f;
    [Tooltip("Que tan rapido baja y vuelve. Mas alto = mas seco.")]
    [Min(0.1f)] public float velocidad = 3f;

    [Header("Cuando se puede")]
    [Tooltip("Segundos que hay que mantener la tecla antes de que empiece a bajar, para que no " +
             "se mueva sola cada vez que el jugador toca S de paso.")]
    [Min(0f)] public float esperaAntesDeBajar = 0.35f;
    [Tooltip("Activo: solo mira hacia abajo si esta en el PISO. Es lo recomendado: en el aire la " +
             "camara ya se mueve sola con la caida.")]
    public bool soloEnElPiso = true;
    [Tooltip("Velocidad horizontal maxima para considerar que esta quieta. Caminando no baja.")]
    [Min(0f)] public float velocidadMaximaParaMirar = 0.6f;

    private CinemachinePositionComposer composer;
    private PlayerController jugador;

    private float desplazamientoActual;
    private float tiempoManteniendo;
    private float offsetOriginalY;
    private bool guardoElOriginal;

    private void Awake()
    {
        composer = GetComponent<CinemachinePositionComposer>();
    }

    private void Update()
    {
        if (jugador == null)
        {
            jugador = FindFirstObjectByType<PlayerController>();
            if (jugador == null) return;
        }

        if (!guardoElOriginal)
        {
            offsetOriginalY = composer.TargetOffset.y;
            guardoElOriginal = true;
        }

        if (QuiereMirarAbajo())
        {
            tiempoManteniendo += Time.deltaTime;
        }
        else
        {
            tiempoManteniendo = 0f;
        }

        float objetivo = tiempoManteniendo >= esperaAntesDeBajar ? -distancia : 0f;
        desplazamientoActual = Mathf.MoveTowards(desplazamientoActual, objetivo, velocidad * Time.deltaTime * Mathf.Max(1f, distancia));

        Vector3 offset = composer.TargetOffset;
        offset.y = offsetOriginalY + desplazamientoActual;
        composer.TargetOffset = offset;
    }

    private bool QuiereMirarAbajo()
    {
        if (Keyboard.current == null) return false;

        // El juego esta congelado (pausa, cinematica, cartel): la camara no se mueve.
        if (Time.timeScale <= 0.01f) return false;

        // Sin controles (muerte, transicion de zona, cinematica): tampoco.
        if (!jugador.enabled) return false;

        bool tecla = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;
        if (!tecla) return false;

        if (soloEnElPiso && (jugador.jump == null || !jugador.jump.IsGrounded)) return false;

        // Quieta: si esta caminando, la camara se queda donde esta.
        if (jugador.rb != null && Mathf.Abs(jugador.rb.linearVelocity.x) > velocidadMaximaParaMirar) return false;

        // Nada de mirar para abajo en medio de otra mecanica.
        if (jugador.climb != null && jugador.climb.IsClimbing) return false;
        if (jugador.dash != null && jugador.dash.IsDash) return false;
        if (jugador.attacks != null && jugador.attacks.IsAttack) return false;
        if (jugador.superJump != null && jugador.superJump.IsPreparing) return false;

        return true;
    }

    // Si se apaga el componente (cambio de escena, etc) dejamos la camara como estaba.
    private void OnDisable()
    {
        if (composer == null || !guardoElOriginal) return;

        Vector3 offset = composer.TargetOffset;
        offset.y = offsetOriginalY;
        composer.TargetOffset = offset;
        desplazamientoActual = 0f;
    }
}
