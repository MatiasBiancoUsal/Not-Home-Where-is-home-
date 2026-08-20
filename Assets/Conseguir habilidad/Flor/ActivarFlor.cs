using System.Collections;
using UnityEngine;

public class ActivarFlor : MonoBehaviour
{
    [Header("Habilidad que entrega")]
    [SerializeField] private PlayerController.Habilidad habilidad = PlayerController.Habilidad.DobleSalto;
    [Tooltip("Si esta activo, nombres como FlorDobleSalto, FlorDash o FlorEscalar eligen la habilidad automaticamente.")]
    [SerializeField] private bool detectarPorNombre = true;

    [Header("Como queda la flor una vez recogida")]
    [Tooltip("Sprite de la flor ABIERTA. Si lo dejas vacio, se congela sola en el ultimo frame de la animacion (que es lo que suele quedar bien). Llenalo solo si querés otro dibujo.")]
    [SerializeField] private Sprite spriteFlorAbierta;
    [Tooltip("Activalo SOLO si querés que la flor desaparezca al volver a la zona. Normalmente va apagado: la flor queda abierta como recuerdo de que ya la agarraste.")]
    [SerializeField] private bool ocultarAlVolverALaZona = false;

    [Header("Cartel despues de la animacion")]
    [SerializeField] private bool mostrarCartel = true;
    [Tooltip("La animacion actual dura 2 segundos.")]
    [SerializeField] private float esperaAntesDelCartel = 2f;
    [SerializeField] private Sprite imagenCartel;
    [Tooltip("Ilustracion que reemplaza el rectangulo negro en todos los carteles de habilidad.")]
    [SerializeField] private Sprite fondoCartel;

    [Header("Cartel correspondiente a cada habilidad")]
    [Tooltip("Estos carteles se eligen automaticamente segun la habilidad de la flor.")]
    [SerializeField] private Sprite cartelDobleSalto;
    [SerializeField] private Sprite cartelDash;
    [SerializeField] private Sprite cartelEscalar;
    [SerializeField] private Sprite cartelPisoton;
    [SerializeField] private Sprite cartelSuperSalto;
    [SerializeField] private Sprite cartelEscudo;
    [SerializeField] private string tituloCartel = "NUEVA HABILIDAD";
    [TextArea(2, 5)]
    [SerializeField] private string descripcionCartel = "Proba tu nueva habilidad.";
    [SerializeField] private string textoParaCerrar = "Presiona ESPACIO, ENTER o ESC para continuar";
    [SerializeField] private bool pausarMientrasSeMuestra = true;

    [Header("Diseño del cartel")]
    [Tooltip("Desplega esta seccion para modificar posiciones, tamaños, tipografias y colores.")]
    [SerializeField] private EstiloCartelHabilidad estiloCartel = new EstiloCartelHabilidad();

    private const string TRIGGER_ABRIR = "ActivarFlor";

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool recogida;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (detectarPorNombre) DetectarHabilidadPorNombre();
        AsignarCartelDeHabilidad();
        CompletarTextoPredeterminado();
    }

    private void Start()
    {
        if (!ProgresoJuego.YaMostrado(ClaveProgreso())) return;

        // Ya la agarramos en otra vuelta. Por defecto la dejamos ABIERTA en el escenario;
        // solo desaparece si nos lo piden expresamente.
        recogida = true;
        DesactivarCollider();

        if (ocultarAlVolverALaZona)
        {
            gameObject.SetActive(false);
            return;
        }

        AbrirSinAnimacion();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (recogida || !other.CompareTag("Player")) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        recogida = true;
        player.DesbloquearHabilidad(habilidad);

        if (animator != null)
        {
            animator.SetTrigger(TRIGGER_ABRIR);
        }

        DesactivarCollider();

        // Corre SIEMPRE (aunque no haya cartel): al terminar la animacion hay que
        // dejar la flor abierta para que no vuelva sola al estado cerrado.
        StartCoroutine(SecuenciaRecoleccion());
    }

    private IEnumerator SecuenciaRecoleccion()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, esperaAntesDelCartel));

        QuedarAbierta();

        if (mostrarCartel)
        {
            CartelHabilidadUI.Mostrar(
                imagenCartel,
                fondoCartel,
                tituloCartel,
                descripcionCartel,
                textoParaCerrar,
                pausarMientrasSeMuestra,
                estiloCartel);
        }
    }

    // Fija la flor en su estado abierto. Se llama cuando la animacion ya termino.
    private void QuedarAbierta()
    {
        // Apagar el Animator congela el sprite en el frame donde quedo, que es el
        // ultimo de la animacion de apertura.
        if (animator != null) animator.enabled = false;

        if (spriteFlorAbierta != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = spriteFlorAbierta;
        }
    }

    // Igual que arriba, pero para cuando entramos a la zona y la flor YA estaba recogida:
    // tiene que verse abierta de entrada, sin reproducir la animacion delante del jugador.
    private void AbrirSinAnimacion()
    {
        if (spriteFlorAbierta != null && spriteRenderer != null)
        {
            if (animator != null) animator.enabled = false;
            spriteRenderer.sprite = spriteFlorAbierta;
            return;
        }

        if (animator == null) return;

        // Sin sprite asignado: adelantamos la animacion hasta el final de una y la
        // congelamos ahi, asi el jugador la ve abierta desde el primer frame.
        animator.SetTrigger(TRIGGER_ABRIR);
        animator.Update(0f);
        animator.Update(Mathf.Max(0.1f, esperaAntesDelCartel));
        animator.enabled = false;
    }

    private void DesactivarCollider()
    {
        Collider2D flowerCollider = GetComponent<Collider2D>();
        if (flowerCollider != null) flowerCollider.enabled = false;
    }

    private void DetectarHabilidadPorNombre()
    {
        string normalizedName = gameObject.name.Replace(" ", "").Replace("_", "").ToLowerInvariant();

        if (normalizedName.Contains("doblesalto")) habilidad = PlayerController.Habilidad.DobleSalto;
        else if (normalizedName.Contains("dash")) habilidad = PlayerController.Habilidad.Dash;
        else if (normalizedName.Contains("escalar") || normalizedName.Contains("trepar")) habilidad = PlayerController.Habilidad.Escalar;
        else if (normalizedName.Contains("pisoton") || normalizedName.Contains("stomp")) habilidad = PlayerController.Habilidad.Pisoton;
        else if (normalizedName.Contains("supersalto")) habilidad = PlayerController.Habilidad.SuperSalto;
        else if (normalizedName.Contains("escudo")) habilidad = PlayerController.Habilidad.Escudo;
    }

    private string ClaveProgreso()
    {
        return "Habilidad_" + habilidad;
    }

    private void AsignarCartelDeHabilidad()
    {
        Sprite cartelCorrespondiente = null;

        switch (habilidad)
        {
            case PlayerController.Habilidad.DobleSalto: cartelCorrespondiente = cartelDobleSalto; break;
            case PlayerController.Habilidad.Dash: cartelCorrespondiente = cartelDash; break;
            case PlayerController.Habilidad.Escalar: cartelCorrespondiente = cartelEscalar; break;
            case PlayerController.Habilidad.Pisoton: cartelCorrespondiente = cartelPisoton; break;
            case PlayerController.Habilidad.SuperSalto: cartelCorrespondiente = cartelSuperSalto; break;
            case PlayerController.Habilidad.Escudo: cartelCorrespondiente = cartelEscudo; break;
        }

        if (cartelCorrespondiente != null)
        {
            fondoCartel = cartelCorrespondiente;
            imagenCartel = null;
        }
    }

    private void CompletarTextoPredeterminado()
    {
        if (tituloCartel != "NUEVA HABILIDAD" || descripcionCartel != "Proba tu nueva habilidad.") return;

        switch (habilidad)
        {
            case PlayerController.Habilidad.DobleSalto:
                tituloCartel = "DOBLE SALTO";
                descripcionCartel = "Presiona SALTO nuevamente mientras estas en el aire.";
                break;
            case PlayerController.Habilidad.Dash:
                tituloCartel = "DASH";
                descripcionCartel = "Presiona SHIFT para impulsarte en la direccion elegida.";
                break;
            case PlayerController.Habilidad.Escalar:
                tituloCartel = "ESCALAR";
                descripcionCartel = "Acercate a una pared y usa W o S para trepar.";
                break;
            case PlayerController.Habilidad.Pisoton:
                tituloCartel = "PISOTON";
                descripcionCartel = "En el aire, presiona S para caer con fuerza.";
                break;
            case PlayerController.Habilidad.SuperSalto:
                tituloCartel = "SUPER SALTO";
                descripcionCartel = "En el suelo, manten W para cargar y presiona SALTO.";
                break;
            case PlayerController.Habilidad.Escudo:
                tituloCartel = "ESCUDO";
                descripcionCartel = "Presiona E para invocar un escudo que aguanta 6 golpes.";
                break;
        }
    }
}
