using System.Collections;
using UnityEngine;

public class ActivarFlor : MonoBehaviour
{
    [Header("Habilidad que entrega")]
    [SerializeField] private PlayerController.Habilidad habilidad = PlayerController.Habilidad.DobleSalto;
    [Tooltip("Si esta activo, nombres como FlorDobleSalto, FlorDash o FlorEscalar eligen la habilidad automaticamente.")]
    [SerializeField] private bool detectarPorNombre = true;
    [Tooltip("Si ya fue recogida, se oculta al volver a entrar a la zona.")]
    [SerializeField] private bool ocultarSiYaFueRecogida = true;

    [Header("Cartel despues de la animacion")]
    [SerializeField] private bool mostrarCartel = true;
    [Tooltip("La animacion actual dura 2 segundos.")]
    [SerializeField] private float esperaAntesDelCartel = 2f;
    [SerializeField] private Sprite imagenCartel;
    [Tooltip("Ilustracion que reemplaza el rectangulo negro en todos los carteles de habilidad.")]
    [SerializeField] private Sprite fondoCartel;
    [SerializeField] private string tituloCartel = "NUEVA HABILIDAD";
    [TextArea(2, 5)]
    [SerializeField] private string descripcionCartel = "Proba tu nueva habilidad.";
    [SerializeField] private string textoParaCerrar = "Presiona ESPACIO, ENTER o ESC para continuar";
    [SerializeField] private bool pausarMientrasSeMuestra = true;

    [Header("Diseño del cartel")]
    [Tooltip("Desplega esta seccion para modificar posiciones, tamaños, tipografias y colores.")]
    [SerializeField] private EstiloCartelHabilidad estiloCartel = new EstiloCartelHabilidad();

    private Animator animator;
    private bool recogida;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (detectarPorNombre) DetectarHabilidadPorNombre();
        CompletarTextoPredeterminado();
    }

    private void Start()
    {
        if (ocultarSiYaFueRecogida && ProgresoJuego.YaMostrado(ClaveProgreso()))
        {
            gameObject.SetActive(false);
        }
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
            animator.SetTrigger("ActivarFlor");
        }

        Collider2D flowerCollider = GetComponent<Collider2D>();
        if (flowerCollider != null) flowerCollider.enabled = false;

        if (mostrarCartel)
        {
            StartCoroutine(MostrarCartelDespuesDeAnimacion());
        }
    }

    private IEnumerator MostrarCartelDespuesDeAnimacion()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, esperaAntesDelCartel));
        CartelHabilidadUI.Mostrar(
            imagenCartel,
            fondoCartel,
            tituloCartel,
            descripcionCartel,
            textoParaCerrar,
            pausarMientrasSeMuestra,
            estiloCartel);
    }

    private void DetectarHabilidadPorNombre()
    {
        string normalizedName = gameObject.name.Replace(" ", "").Replace("_", "").ToLowerInvariant();

        if (normalizedName.Contains("doblesalto")) habilidad = PlayerController.Habilidad.DobleSalto;
        else if (normalizedName.Contains("dash")) habilidad = PlayerController.Habilidad.Dash;
        else if (normalizedName.Contains("escalar") || normalizedName.Contains("trepar")) habilidad = PlayerController.Habilidad.Escalar;
        else if (normalizedName.Contains("pisoton") || normalizedName.Contains("stomp")) habilidad = PlayerController.Habilidad.Pisoton;
        else if (normalizedName.Contains("supersalto")) habilidad = PlayerController.Habilidad.SuperSalto;
    }

    private string ClaveProgreso()
    {
        return "Habilidad_" + habilidad;
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
        }
    }
}
