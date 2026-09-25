using System.Collections;
using UnityEngine;

public enum TipoDeBicho { Rastrero, Volador, Saltarin }

// ============================================================
//  MINI BICHO (los que invoca el boss final)
//  Un solo script para los 3 tipos; se elige con el campo "Tipo":
//    - Rastrero: camina por el piso hacia la niña.
//    - Volador:  vuela hacia la niña ondulando.
//    - Saltarin: se agacha, y salta justo hacia donde esta la niña.
//  Mueren de un golpe. Aparecer, parpadear y morir se hace por codigo:
//  el dibujo solo necesita su loop (caminar / aletear) o sus 2 poses (saltarin).
//
//  Para armar el prefab: un objeto con SpriteRenderer + un Collider2D (NO trigger) +
//  este script. Al agregarlo se suman solos el Rigidbody2D, HealthHandler y Damageable.
// ============================================================
[DefaultExecutionOrder(-50)] // antes que el HealthHandler, para poder fijarle la vida
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(HealthHandler))]
[RequireComponent(typeof(Damageable))]
public class MiniBicho : MonoBehaviour
{
    [Header("Vida")]
    [Tooltip("Cuantos golpes aguanta.")]
    [Min(1)] public int golpesQueAguanta = 3;

    [Header("Que bicho es")]
    public TipoDeBicho tipo = TipoDeBicho.Rastrero;
    [Tooltip("Activo si en el dibujo el bicho mira hacia la DERECHA. Se da vuelta solo segun donde este la niña.")]
    public bool dibujoMiraALaDerecha = true;

    [Header("Daño a la niña")]
    public int danio = 1;
    public float empuje = 6f;

    [Header("Rastrero")]
    public float velocidadCaminando = 2.5f;

    [Header("Volador")]
    public float velocidadVolando = 2f;
    [Tooltip("Cuanto se mueve de costado mientras vuela. 0 = va derecho.")]
    public float ondulacion = 1.2f;
    [Tooltip("Apunta un poco por arriba de la niña, asi no se arrastra por el piso.")]
    public float alturaSobreLaNina = 0.3f;

    [Header("Saltarin")]
    [Tooltip("Altura de cada salto, en unidades.")]
    public float alturaDelSalto = 5f;
    [Tooltip("Distancia maxima que puede avanzar en un salto.")]
    public float alcanceMaximo = 5f;
    [Tooltip("Segundos quieto en el piso entre salto y salto.")]
    public float esperaEntreSaltos = 0.9f;
    [Tooltip("Segundos agachado antes de saltar (es el aviso para el jugador).")]
    public float tiempoAgachado = 0.35f;
    [Tooltip("Dibujo mientras se agacha. Si lo dejas vacio, se aplasta por codigo.")]
    public Sprite spriteAgachado;
    [Tooltip("Dibujo en el aire. Si lo dejas vacio, usa el mismo del SpriteRenderer.")]
    public Sprite spriteEnElAire;

    [Header("Animacion (si usas un Animator)")]
    [Tooltip("Si tu bicho tiene UNA sola animacion en loop (caminar, aletear), dejalo VACIO: " +
             "el Animator la reproduce solo y no hay que tocar nada.\n\n" +
             "Ponelo solo si el bicho tiene varias animaciones (util para el Saltarin: " +
             "quieto, agachado y en el aire).")]
    public Animator animador;
    [Tooltip("Nombre EXACTO del parametro Int del Animator.")]
    public string parametroDeEstado = "estado";
    public int animNormal = 1;
    public int animAgachado = 2;
    public int animEnElAire = 3;

    [Header("Aparicion")]
    [Tooltip("Segundos que tarda en salir de la grieta (crece desde cero). Mientras sale no pega ni se le puede pegar.")]
    public float duracionAparicion = 0.5f;
    public AudioClip sonidoAlAparecer;

    [Header("Muerte")]
    public int puntosPorMatar = 10;
    [Tooltip("Efecto que queda al morir (la animacion de muerte de los monstruos del juego). " +
             "Es un prefab suelto con su Animator y el script AutoDestruir. Vacio = solo el puf.")]
    public GameObject prefabDeLaMuerte;
    [Tooltip("Agranda o achica ese efecto. 1 = como es.")]
    public float escalaDelEfectoDeMuerte = 1f;
    [Tooltip("Los cuadraditos que saltan al morir. Con animacion de muerte podes apagarlos.")]
    public bool mostrarPufAlMorir = true;
    public Color colorDelPuf = Color.white;
    public AudioClip sonidoAlMorir;
    [Tooltip("Suena cuando le pegan y NO se muere.")]
    public AudioClip sonidoAlRecibirGolpe;
    [Range(0f, 1f)] public float volumen = 0.8f;

    // El boss se entera cuando muere. El bool dice si lo mato la niña (true) o el boss lo saco (false).
    public event System.Action<MiniBicho, bool> AlMorir;
    public bool Muerto => muerto;

    private Rigidbody2D rb;
    private HealthHandler vida;
    private SpriteRenderer sr;
    private Collider2D col;
    private Sprite spriteNormal;
    private Vector3 escalaOriginal;
    private LayerMask piso;

    private bool apareciendo = true;
    private bool muerto;
    private bool agachado;
    private float tAparicion;
    private float cooldownGolpe;
    private float esperaSalto;
    private float sinChequearPiso;
    private float fase;
    private int vidaAnterior;
    private float golpeado;
    private float yMinimo = float.NegativeInfinity;

    // Se ejecuta al agregar el script en el editor: deja todo listo.
    private void Reset()
    {
        GetComponent<HealthHandler>().maxHealth = golpesQueAguanta;

        Damageable d = GetComponent<Damageable>();
        d.activeKnockBack = false;
        d.activeInvulnerability = false;

        Rigidbody2D r = GetComponent<Rigidbody2D>();
        r.freezeRotation = true;
        r.gravityScale = 3f;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        vida = GetComponent<HealthHandler>();
        sr = GetComponentInChildren<SpriteRenderer>();

        col = GetComponent<Collider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();

        if (sr != null && sr.sprite == null) sr.sprite = UtilBoss.Cuadrado();
        spriteNormal = sr != null ? sr.sprite : null;

        escalaOriginal = transform.localScale;
        fase = Random.value * 10f;

        // Esto corre ANTES del Awake del HealthHandler (ver DefaultExecutionOrder arriba),
        // asi el bicho arranca con la vida que diga "Golpes Que Aguanta".
        vida.maxHealth = Mathf.Max(1, golpesQueAguanta);
        vidaAnterior = vida.maxHealth;

        vida.destroyOnDeath = false;
        vida.OnDeath += AlQuedarseSinVida;
        vida.OnHealthChanged += AlRecibirGolpe;

        rb.freezeRotation = true;
    }

    // La llama el boss al crearlo: si se cae por debajo de esa altura, se elimina solo.
    public void Iniciar(float alturaMinima)
    {
        yMinimo = alturaMinima;
    }

    private void Start()
    {
        piso = UtilBoss.MascaraDePiso();

        // Que no empuje fisicamente a la niña: el daño lo maneja este script.
        Collider2D colNina = UtilBoss.ColliderJugador();
        if (colNina != null) Physics2D.IgnoreCollision(col, colNina, true);

        if (tipo == TipoDeBicho.Volador) rb.gravityScale = 0f;
        esperaSalto = esperaEntreSaltos * 0.5f;

        // Sale de la grieta: arranca en tamaño cero y sin fisica.
        transform.localScale = Vector3.zero;
        rb.simulated = false;
        UtilBoss.Sonar(sonidoAlAparecer, volumen, transform.position);
    }

    private void OnDestroy()
    {
        if (vida != null) vida.OnDeath -= AlQuedarseSinVida;
        if (vida != null) vida.OnHealthChanged -= AlRecibirGolpe;
    }

    private void Update()
    {
        if (muerto) return;

        if (apareciendo)
        {
            tAparicion += Time.deltaTime;
            float k = Mathf.Clamp01(tAparicion / Mathf.Max(0.01f, duracionAparicion));
            transform.localScale = escalaOriginal * RebotarAlFinal(k);

            if (k >= 1f)
            {
                apareciendo = false;
                transform.localScale = escalaOriginal;
                rb.simulated = true;
            }
            return;
        }

        // Daño por contacto (con un respiro, por si la niña no tiene invulnerabilidad).
        if (golpeado > 0f)
        {
            // Aplastadito al recibir un golpe, para que se note.
            golpeado -= Time.deltaTime;
            float k = Mathf.Max(0f, golpeado) / 0.18f * 0.25f;
            transform.localScale = new Vector3(escalaOriginal.x * (1f + k), escalaOriginal.y * (1f - k), escalaOriginal.z);
        }

        if (cooldownGolpe > 0f) cooldownGolpe -= Time.deltaTime;
        if (cooldownGolpe <= 0f && UtilBoss.TocaAlJugador(col))
        {
            cooldownGolpe = UtilBoss.PegarAlJugador(danio, transform.position, empuje) ? 1f : 0.2f;
        }

        // Se cayo del ring.
        if (transform.position.y < yMinimo)
        {
            Morir(false);
            return;
        }

        Mirar();
        ActualizarDibujoDelSaltarin();
    }

    private void FixedUpdate()
    {
        if (muerto || apareciendo) return;

        switch (tipo)
        {
            case TipoDeBicho.Rastrero: MoverRastrero(); break;
            case TipoDeBicho.Volador:  MoverVolador();  break;
            case TipoDeBicho.Saltarin: MoverSaltarin(); break;
        }
    }

    // ---------- Movimientos ----------

    private void MoverRastrero()
    {
        float dx = UtilBoss.PosicionJugador().x - transform.position.x;
        float dir = Mathf.Abs(dx) > 0.1f ? Mathf.Sign(dx) : 0f;
        rb.linearVelocity = new Vector2(dir * velocidadCaminando, rb.linearVelocity.y);
    }

    private void MoverVolador()
    {
        Vector2 objetivo = UtilBoss.PosicionJugador() + Vector2.up * alturaSobreLaNina;
        Vector2 dir = (objetivo - (Vector2)transform.position).normalized;
        Vector2 costado = new Vector2(-dir.y, dir.x);
        float vaiven = Mathf.Sin(Time.time * 4f + fase) * ondulacion;

        rb.linearVelocity = dir * velocidadVolando + costado * vaiven;
    }

    private void MoverSaltarin()
    {
        if (agachado) return;

        if (sinChequearPiso > 0f)
        {
            sinChequearPiso -= Time.fixedDeltaTime;
            return;
        }

        if (!EnElPiso()) return;

        // Quieto en el piso esperando el proximo salto.
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        esperaSalto -= Time.fixedDeltaTime;

        if (esperaSalto <= 0f) StartCoroutine(Saltar());
    }

    private IEnumerator Saltar()
    {
        agachado = true;

        // Se agacha (el aviso). Si no hay dibujo de agachado, lo aplastamos.
        if (animador == null && spriteAgachado == null) transform.localScale = new Vector3(escalaOriginal.x * 1.2f, escalaOriginal.y * 0.7f, escalaOriginal.z);
        yield return new WaitForSeconds(tiempoAgachado);
        transform.localScale = escalaOriginal;

        if (muerto) yield break;

        // Calculamos el salto para caer justo donde esta la niña (sin pasarse del alcance).
        float g = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
        if (g < 0.01f) g = 9.81f;

        float vy = Mathf.Sqrt(2f * g * Mathf.Max(0.1f, alturaDelSalto));
        float tiempoEnElAire = 2f * vy / g;
        float dx = Mathf.Clamp(UtilBoss.PosicionJugador().x - transform.position.x, -alcanceMaximo, alcanceMaximo);

        rb.linearVelocity = new Vector2(dx / tiempoEnElAire, vy);

        agachado = false;
        esperaSalto = esperaEntreSaltos;
        sinChequearPiso = 0.15f;   // que no se crea que sigue en el piso en el primer frame del salto
    }

    private bool EnElPiso()
    {
        return rb.IsTouchingLayers(piso) && rb.linearVelocity.y <= 0.05f;
    }

    // ---------- Dibujo ----------

    private void Mirar()
    {
        if (sr == null) return;

        float dx = tipo == TipoDeBicho.Volador
            ? rb.linearVelocity.x
            : UtilBoss.PosicionJugador().x - transform.position.x;

        if (Mathf.Abs(dx) < 0.05f) return;

        bool haciaLaDerecha = dx > 0f;
        sr.flipX = dibujoMiraALaDerecha ? !haciaLaDerecha : haciaLaDerecha;
    }

    private void ActualizarDibujoDelSaltarin()
    {
        if (sr == null) return;

        bool enElAire = tipo == TipoDeBicho.Saltarin && !agachado && (sinChequearPiso > 0f || !EnElPiso());

        // Con Animator no tocamos el sprite: le avisamos en que estado esta.
        if (animador != null)
        {
            if (string.IsNullOrEmpty(parametroDeEstado)) return;

            int estado = animNormal;
            if (tipo == TipoDeBicho.Saltarin)
            {
                if (agachado) estado = animAgachado;
                else if (enElAire) estado = animEnElAire;
            }

            animador.SetInteger(parametroDeEstado, estado);
            return;
        }

        if (tipo != TipoDeBicho.Saltarin) return;

        Sprite s = spriteNormal;
        if (agachado && spriteAgachado != null) s = spriteAgachado;
        else if (enElAire && spriteEnElAire != null) s = spriteEnElAire;

        if (s != null && sr.sprite != s) sr.sprite = s;
    }

    // ---------- Muerte ----------

    // Aguantan varios golpes, asi que hace falta que se note cuando les pegan.
    private void AlRecibirGolpe(int vidaActual)
    {
        if (vidaActual < vidaAnterior && vidaActual > 0)
        {
            EfectoPuf.Crear(sr != null ? sr.bounds.center : transform.position, Color.white, 4, 0.12f, 3f);
            UtilBoss.Sonar(sonidoAlRecibirGolpe, volumen, transform.position);
            golpeado = 0.18f;
        }
        vidaAnterior = vidaActual;
    }

    private void AlQuedarseSinVida()
    {
        Morir(true);
    }

    // Para que el boss saque los bichos que quedaron (al morir el boss, o si la ola dura demasiado).
    public void Eliminar()
    {
        Morir(false);
    }

    private void Morir(bool loMatoLaNina)
    {
        if (muerto) return;
        muerto = true;

        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        col.enabled = false;

        Vector3 centro = sr != null ? sr.bounds.center : transform.position;
        SoltarEfectoDeMuerte(centro, prefabDeLaMuerte, escalaDelEfectoDeMuerte);
        if (mostrarPufAlMorir) EfectoPuf.Crear(centro, colorDelPuf, 9, 0.2f, 4.5f);
        UtilBoss.Sonar(sonidoAlMorir, volumen, transform.position);

        if (loMatoLaNina && ScoreManager.Instance != null) ScoreManager.Instance.AddPoints(puntosPorMatar);

        AlMorir?.Invoke(this, loMatoLaNina);

        StartCoroutine(Encoger());
    }

    // Deja la animacion de muerte en el lugar. Es un objeto aparte: el bicho se borra
    // enseguida, pero el efecto se queda hasta que termina (lo borra su AutoDestruir).
    public static void SoltarEfectoDeMuerte(Vector3 donde, GameObject prefab, float escala)
    {
        if (prefab == null) return;

        GameObject efecto = Instantiate(prefab, donde, Quaternion.identity);
        if (escala > 0f && !Mathf.Approximately(escala, 1f)) efecto.transform.localScale *= escala;
    }

    private IEnumerator Encoger()
    {
        Vector3 inicial = transform.localScale;
        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(inicial, Vector3.zero, t / 0.15f);
            yield return null;
        }
        Destroy(gameObject);
    }

    // Crece, se pasa un poquito y vuelve: da la sensacion de que "salta" de la grieta.
    private float RebotarAlFinal(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
