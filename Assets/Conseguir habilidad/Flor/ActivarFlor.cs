using UnityEngine;

public class ActivarFlor : MonoBehaviour
{
    [Header("Habilidad que entrega")]
    [SerializeField] private PlayerController.Habilidad habilidad = PlayerController.Habilidad.DobleSalto;
    [Tooltip("Si esta activo, nombres como FlorDobleSalto, FlorDash o FlorEscalar eligen la habilidad automaticamente.")]
    [SerializeField] private bool detectarPorNombre = true;
    [Tooltip("Si ya fue recogida, se oculta al volver a entrar a la zona.")]
    [SerializeField] private bool ocultarSiYaFueRecogida = true;

    private Animator animator;
    private bool recogida;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (detectarPorNombre) DetectarHabilidadPorNombre();
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
}
