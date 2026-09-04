using System.Collections;
using UnityEngine;

// Todo lo que muestra el cartel de UNA habilidad, junto en un solo lugar.
[System.Serializable]
public class CartelDeHabilidad
{
    [Tooltip("La imagen que hace de BASE del cartel para esta habilidad.")]
    public Sprite fondo;
    [Tooltip("Titulo grande. OJO: la tipografia del juego no tiene numeros, si escribis " +
             "un numero va a salir con otra letra.")]
    public string titulo;
    [TextArea(2, 4)]
    [Tooltip("La explicacion de la habilidad. Es el texto que aparece con la animacion de tipeo.")]
    public string descripcion;
}

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

    [Header("EL CARTEL DE CADA HABILIDAD (imagen + textos)")]
    [Tooltip("Aca se edita TODO lo que muestra el cartel de cada habilidad. La flor usa el bloque " +
             "que corresponde a la habilidad que entrega, asi que se completa una vez y sirve para " +
             "todas las flores del juego.")]
    [SerializeField] private CartelDeHabilidad dobleSalto = new CartelDeHabilidad
    {
        titulo = "DOBLE SALTO",
        descripcion = "Presiona SALTO nuevamente mientras estas en el aire."
    };
    [SerializeField] private CartelDeHabilidad dash = new CartelDeHabilidad
    {
        titulo = "DASH",
        descripcion = "Presiona SHIFT para impulsarte en la direccion elegida."
    };
    [SerializeField] private CartelDeHabilidad escalar = new CartelDeHabilidad
    {
        titulo = "ESCALAR",
        descripcion = "Acercate a una pared y usa W o S para trepar."
    };
    [SerializeField] private CartelDeHabilidad pisoton = new CartelDeHabilidad
    {
        titulo = "PISOTON",
        descripcion = "En el aire, presiona S para caer con fuerza."
    };
    [SerializeField] private CartelDeHabilidad superSalto = new CartelDeHabilidad
    {
        titulo = "SUPER SALTO",
        descripcion = "En el suelo, manten W para cargar y presiona SALTO."
    };
    [SerializeField] private CartelDeHabilidad escudo = new CartelDeHabilidad
    {
        titulo = "ESCUDO",
        descripcion = "Presiona E para invocar un escudo que aguanta varios golpes."
    };

    // ---- Campos viejos, de cuando la imagen y el texto estaban separados ----
    // Quedan ocultos SOLO para no perder los sprites que ya estaban asignados: la primera
    // vez que se abre la flor, MigrarSpritesViejos() los copia al bloque nuevo. Una vez
    // migrado todo, se pueden borrar de aca.
    [HideInInspector] [SerializeField] private Sprite cartelDobleSalto;
    [HideInInspector] [SerializeField] private Sprite cartelDash;
    [HideInInspector] [SerializeField] private Sprite cartelEscalar;
    [HideInInspector] [SerializeField] private Sprite cartelPisoton;
    [HideInInspector] [SerializeField] private Sprite cartelSuperSalto;
    [HideInInspector] [SerializeField] private Sprite cartelEscudo;

    [Header("Comun a todos los carteles")]
    [SerializeField] private string textoParaCerrar = "Presiona ESPACIO, ENTER o ESC para continuar";
    [SerializeField] private bool pausarMientrasSeMuestra = true;

    // El titulo y la descripcion que se van a mostrar. No se editan aca: los completa
    // AsignarCartelDeHabilidad() copiandolos del bloque de la habilidad que entrega la flor.
    private string tituloCartel;
    private string descripcionCartel;

    [Header("Diseño del cartel")]
    [Tooltip("Desplega esta seccion para modificar posiciones, tamaños, tipografias y colores.")]
    [SerializeField] private EstiloCartelHabilidad estiloCartel = new EstiloCartelHabilidad();

    // Para que otros carteles del juego (por ejemplo el de COMBATE que aparece despues de
    // la cinematica) puedan usar exactamente el mismo diseño que los de habilidad, en vez
    // de tener que copiar tipografias y posiciones a mano y que despues queden distintos.
    public EstiloCartelHabilidad EstiloDelCartel => estiloCartel;

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

    // Devuelve el bloque de cartel que corresponde a la habilidad que entrega esta flor.
    private CartelDeHabilidad CartelDeEstaHabilidad()
    {
        switch (habilidad)
        {
            case PlayerController.Habilidad.DobleSalto: return dobleSalto;
            case PlayerController.Habilidad.Dash: return dash;
            case PlayerController.Habilidad.Escalar: return escalar;
            case PlayerController.Habilidad.Pisoton: return pisoton;
            case PlayerController.Habilidad.SuperSalto: return superSalto;
            case PlayerController.Habilidad.Escudo: return escudo;
            default: return null;
        }
    }

    // Copia la imagen y los textos del bloque de esta habilidad a los campos que usa el cartel.
    private void AsignarCartelDeHabilidad()
    {
        MigrarSpritesViejos();

        CartelDeHabilidad cartel = CartelDeEstaHabilidad();
        if (cartel == null) return;

        if (cartel.fondo != null)
        {
            fondoCartel = cartel.fondo;
            imagenCartel = null;
        }

        tituloCartel = string.IsNullOrWhiteSpace(cartel.titulo) ? "NUEVA HABILIDAD" : cartel.titulo;
        descripcionCartel = cartel.descripcion ?? string.Empty;
    }

    // Antes la imagen de cada cartel estaba en un campo suelto, separada del texto. Al pasar
    // todo a un solo bloque por habilidad, los sprites que ya estaban asignados se copian
    // aca para no tener que volver a arrastrarlos uno por uno.
    private void MigrarSpritesViejos()
    {
        Copiar(cartelDobleSalto, dobleSalto);
        Copiar(cartelDash, dash);
        Copiar(cartelEscalar, escalar);
        Copiar(cartelPisoton, pisoton);
        Copiar(cartelSuperSalto, superSalto);
        Copiar(cartelEscudo, escudo);
    }

    private static void Copiar(Sprite viejo, CartelDeHabilidad destino)
    {
        // Solo si el nuevo esta vacio: lo que se haya cargado a mano siempre gana.
        if (viejo != null && destino != null && destino.fondo == null)
        {
            destino.fondo = viejo;
        }
    }

}
