using UnityEngine;

// ============================================================
//  PROYECTIL DEL BOSS (las bolas del abanico)
//  Va en linea recta. Si toca a la niña le pega; si choca con el piso o una pared
//  se rompe. La crea el boss: no hace falta ponerla en ninguna escena.
//
//  Para usar tu dibujo: prefab con SpriteRenderer (y Animator si queres) + este script,
//  arrastrado al campo "Prefab Bola" del boss.
// ============================================================
public class ProyectilBoss : MonoBehaviour
{
    [Header("Daño a la niña")]
    public int danio = 1;
    public float empuje = 6f;

    [Header("Movimiento")]
    [Tooltip("Segundos maximos de vida, por si se escapa del ring sin chocar con nada.")]
    public float vidaMaxima = 6f;
    [Tooltip("Si esta activo, el dibujo gira para apuntar hacia donde va.")]
    public bool girarHaciaDondeVa = false;

    [Header("Al romperse")]
    public Color colorDelPuf = new Color(0.75f, 0.3f, 1f);
    public AudioClip sonidoAlChocar;
    [Range(0f, 1f)] public float volumen = 0.6f;

    private Vector2 direccion = Vector2.down;
    private float velocidad = 7f;
    private float t;
    private Collider2D col;
    private LayerMask piso;
    private bool roto;

    public void Iniciar(Vector2 dir, float vel)
    {
        direccion = dir.sqrMagnitude > 0f ? dir.normalized : Vector2.down;
        velocidad = vel;
    }

    private void Start()
    {
        col = GetComponent<Collider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;

        piso = UtilBoss.MascaraDePiso();

        if (girarHaciaDondeVa)
        {
            float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angulo);
        }
    }

    private void Update()
    {
        if (roto) return;

        t += Time.deltaTime;
        transform.position += (Vector3)(direccion * velocidad * Time.deltaTime);

        if (UtilBoss.TocaAlJugador(col))
        {
            UtilBoss.PegarAlJugador(danio, transform.position, empuje);
            Romper();
            return;
        }

        // Un ratito de gracia al salir, por si el boss esta pegado a una pared.
        if (t > 0.15f && Physics2D.OverlapPoint(transform.position, piso) != null)
        {
            Romper();
            return;
        }

        if (t >= vidaMaxima) Destroy(gameObject);
    }

    private void Romper()
    {
        roto = true;
        EfectoPuf.Crear(transform.position, colorDelPuf, 5, 0.14f, 3f);
        UtilBoss.Sonar(sonidoAlChocar, volumen, transform.position);
        Destroy(gameObject);
    }
}
