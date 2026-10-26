using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class EnemigoCategoria3 : MonoBehaviour
{
    [Header("Animacion")]
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(1f)] private float fotogramasPorSegundo = 10f;

    [Header("Ataque al tocar")]
    [SerializeField, Min(1)] private int danio = 1;
    [SerializeField, Min(0f)] private float fuerzaEmpuje = 8f;
    [SerializeField, Min(0.05f)] private float esperaEntreGolpes = 0.75f;

    private SpriteRenderer spriteRenderer;
    private Collider2D areaDeteccion;
    private Damageable jugadoraDetectada;
    private float relojAnimacion;
    private float proximoGolpe;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        areaDeteccion = GetComponent<Collider2D>();

        if (frames != null && frames.Length > 0)
        {
            spriteRenderer.sprite = frames[0];
        }
    }

    private void Update()
    {
        jugadoraDetectada = BuscarJugadorEnElArea();
        Animar(jugadoraDetectada != null);

        if (jugadoraDetectada != null)
        {
            IntentarDanar(jugadoraDetectada);
        }
    }

    private void Animar(bool activa)
    {
        if (frames == null || frames.Length == 0) return;

        if (!activa)
        {
            relojAnimacion = 0f;
            spriteRenderer.sprite = frames[0];
            return;
        }

        relojAnimacion += Time.deltaTime * fotogramasPorSegundo;
        int indice = Mathf.FloorToInt(relojAnimacion) % frames.Length;
        spriteRenderer.sprite = frames[indice];
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        Damageable jugador = ObtenerJugador(otro.transform);
        if (jugador == null) return;

        jugadoraDetectada = jugador;
        IntentarDanar(jugador);
    }

    private void OnTriggerStay2D(Collider2D otro)
    {
        Damageable jugador = ObtenerJugador(otro.transform);
        if (jugador == null) return;

        jugadoraDetectada = jugador;
        IntentarDanar(jugador);
    }

    private void OnTriggerExit2D(Collider2D otro)
    {
        Damageable jugador = ObtenerJugador(otro.transform);
        if (jugador != null && jugador == jugadoraDetectada)
        {
            jugadoraDetectada = null;
        }
    }

    private Damageable ObtenerJugador(Transform objetivo)
    {
        if (objetivo == null) return null;

        Damageable damageable = objetivo.GetComponentInParent<Damageable>();
        return damageable != null && damageable.CompareTag("Player") ? damageable : null;
    }

    private Damageable BuscarJugadorEnElArea()
    {
        if (areaDeteccion == null) return null;

        Bounds limites = areaDeteccion.bounds;
        Collider2D[] objetosDentro = Physics2D.OverlapBoxAll(
            limites.center,
            limites.size,
            0f,
            LayerMask.GetMask("Player"));

        foreach (Collider2D objeto in objetosDentro)
        {
            Damageable jugador = ObtenerJugador(objeto.transform);
            if (jugador != null) return jugador;
        }

        return null;
    }

    private void IntentarDanar(Damageable damageable)
    {
        if (damageable == null || Time.time < proximoGolpe) return;

        if (damageable.ApplyDamage(danio, transform.position, fuerzaEmpuje))
        {
            proximoGolpe = Time.time + esperaEntreGolpes;
        }
    }

}
