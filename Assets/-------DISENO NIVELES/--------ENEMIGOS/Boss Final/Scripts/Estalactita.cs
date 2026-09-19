using UnityEngine;

// ============================================================
//  ESTALACTITA (ataque del boss final)
//  Aparece en el techo, TIEMBLA un momento mientras en el piso crece su sombra
//  (el aviso), y despues cae. Si toca a la niña le pega; al llegar al piso se rompe.
//
//  No hace falta ponerla en ninguna escena: la crea el boss. Si queres usar tu dibujo,
//  arma un prefab con un SpriteRenderer + este script y arrastralo al campo
//  "Prefab Estalactita" del boss. Si no tiene collider, se lo agrega solo.
// ============================================================
public class Estalactita : MonoBehaviour
{
    [Header("Daño a la niña")]
    public int danio = 1;
    public float empuje = 6f;

    [Header("Caida")]
    [Tooltip("Cuanto acelera al caer.")]
    public float aceleracion = 45f;
    public float velocidadMaxima = 22f;

    [Header("Aviso")]
    [Tooltip("Cuanto tiembla en el techo antes de caer.")]
    public float temblor = 0.06f;
    [Tooltip("Muestra una sombra en el piso donde va a caer.")]
    public bool mostrarSombra = true;
    public Color colorDeLaSombra = new Color(0f, 0f, 0f, 0.55f);
    public float anchoDeLaSombra = 0.9f;

    [Header("Al romperse")]
    public Color colorDelPuf = new Color(0.6f, 0.55f, 0.5f);
    public AudioClip sonidoAlRomperse;
    [Range(0f, 1f)] public float volumen = 0.7f;

    private float tiempoDeAviso = 0.8f;
    private float pisoDelRing = -1000f;
    private float alturaDelSuelo;
    private Vector3 posicionInicial;
    private Collider2D col;
    private Transform sombra;
    private SpriteRenderer dibujoSombra;
    private bool cayendo;
    private bool roto;
    private float t;
    private float velocidad;

    // La llama el boss apenas la crea.
    public void Iniciar(float segundosDeAviso, float yDelPisoDelRing)
    {
        tiempoDeAviso = Mathf.Max(0.05f, segundosDeAviso);
        pisoDelRing = yDelPisoDelRing;
    }

    private void Start()
    {
        col = GetComponent<Collider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        posicionInicial = transform.position;
        alturaDelSuelo = BuscarSuelo();

        if (mostrarSombra) CrearSombra();
    }

    private void Update()
    {
        if (roto) return;

        if (!cayendo)
        {
            // AVISO: tiembla cada vez mas y la sombra crece.
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / tiempoDeAviso);

            transform.position = posicionInicial + Vector3.right * Mathf.Sin(t * 60f) * temblor * (0.5f + k);
            ActualizarSombra(k);

            if (t >= tiempoDeAviso)
            {
                cayendo = true;
                transform.position = posicionInicial;
            }
            return;
        }

        // CAIDA
        velocidad = Mathf.Min(velocidad + aceleracion * Time.deltaTime, velocidadMaxima);
        transform.position += Vector3.down * velocidad * Time.deltaTime;

        if (UtilBoss.TocaAlJugador(col))
        {
            UtilBoss.PegarAlJugador(danio, transform.position, empuje);
            Romper();
            return;
        }

        if (col.bounds.min.y <= alturaDelSuelo) Romper();
    }

    // Busca la primera superficie debajo (piso o plataforma). El rayo arranca adentro del
    // techo, por eso ignoramos lo que este pegado arriba y nos quedamos con el primer piso.
    private float BuscarSuelo()
    {
        RaycastHit2D[] golpes = Physics2D.RaycastAll(posicionInicial, Vector2.down, 60f, UtilBoss.MascaraDePiso());

        foreach (RaycastHit2D g in golpes)
        {
            if (g.collider == null || g.collider.isTrigger) continue;
            if (g.normal.y < 0.5f) continue;                        // no es un piso
            if (g.point.y > posicionInicial.y - 0.5f) continue;     // es el techo del que cuelga

            return Mathf.Max(g.point.y, pisoDelRing);
        }

        return pisoDelRing;
    }

    private void CrearSombra()
    {
        GameObject go = new GameObject("SombraEstalactita");
        go.transform.position = new Vector3(posicionInicial.x, alturaDelSuelo + 0.06f, 0f);

        dibujoSombra = go.AddComponent<SpriteRenderer>();
        dibujoSombra.sprite = UtilBoss.Cuadrado();
        dibujoSombra.sortingOrder = 5;

        sombra = go.transform;
        ActualizarSombra(0f);
    }

    private void ActualizarSombra(float k)
    {
        if (sombra == null) return;

        sombra.localScale = new Vector3(anchoDeLaSombra * Mathf.Lerp(0.3f, 1f, k), 0.12f, 1f);

        Color c = colorDeLaSombra;
        c.a = colorDeLaSombra.a * Mathf.Lerp(0.2f, 1f, k);
        dibujoSombra.color = c;
    }

    private void Romper()
    {
        if (roto) return;
        roto = true;

        EfectoPuf.Crear(new Vector3(transform.position.x, col.bounds.min.y, 0f), colorDelPuf, 7, 0.18f, 3.5f);
        UtilBoss.Sonar(sonidoAlRomperse, volumen, transform.position);
        UtilBoss.Sacudir(0.15f, 0.12f);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (sombra != null) Destroy(sombra.gameObject);
    }
}
