using System.Collections;
using UnityEngine;

// ============================================================
//  ENEMIGO MOCO (obstaculo que va y viene)
//
//  Un monstruo que esta quieto hasta que detecta a la niña. Ahi crece hasta volverse
//  una pared que le corta el paso, se queda un rato y se vuelve a retraer. Mientras la
//  niña siga cerca, repite el ciclo, asi que hay que cronometrar el paso.
//
//  No se puede matar: no tiene vida ni muerte, es un obstaculo. Pero toca hacer daño,
//  de eso se encarga su HurtBox (el mismo componente que usan los otros enemigos).
//
//  LAS TRES ANIMACIONES
//  A la IDA se reproducen en orden: inicio -> pared -> final.
//  A la VUELTA se reproducen al reves: final -> pared -> inicio.
//  El IDLE es opcional: si se le carga uno, lo reproduce en loop mientras espera; si no,
//  queda congelado en el primer cuadro de "inicio".
//
//  EL COLLIDER
//  El collider que corta el paso NO cambia de golpe: cada fase dice a que tamaño y
//  posicion tiene que llegar, y el script lo va estirando de a poco durante la fase,
//  asi acompaña al dibujo en vez de aparecer de una.
//
//  En el Animator NO hace falta armar transiciones ni parametros: alcanza con los tres
//  estados sueltos con su clip (mas el de idle, si lo hay). El script los dispara por nombre.
// ============================================================
[RequireComponent(typeof(Animator))]
public class EnemigoMoco : MonoBehaviour
{
    // Una de las tres etapas por las que pasa el moco.
    [System.Serializable]
    public class Fase
    {
        [Tooltip("Nombre EXACTO del estado en el Animator (no el del clip, el del cuadrito).")]
        public string estadoAnimator = "";
        [Tooltip("Segundos que dura esta fase. Poné lo que dura la animacion, o menos si querés que corra mas rapido.")]
        public float duracion = 0.5f;
        [Tooltip("Si esta activo, en esta fase el moco CORTA EL PASO (el collider solido se prende).")]
        public bool cortaElPaso = false;

        [Header("Collider al terminar esta fase")]
        [Tooltip("Tamaño al que llega el collider al final de esta fase. Se estira de a poco desde el tamaño anterior.")]
        public Vector2 tamanoCollider = new Vector2(1f, 1f);
        [Tooltip("Posicion del collider respecto al centro del objeto, al final de esta fase.")]
        public Vector2 offsetCollider = Vector2.zero;
    }

    [Header("Deteccion de la niña")]
    [Tooltip("A que distancia detecta a la niña y arranca el ciclo. Se ve como un circulo amarillo en la Scene.")]
    public float radioDeteccion = 8f;
    [Tooltip("Corre el centro del circulo de deteccion respecto al objeto.")]
    public Vector2 offsetDeteccion = Vector2.zero;
    public string tagJugador = "Player";
    [Tooltip("Activo: mientras la niña siga cerca, el moco repite el ciclo una y otra vez.\n\n" +
             "Desactivo: hace UN solo ciclo por cada vez que la niña entra al radio.")]
    public bool repetirMientrasEsteCerca = true;

    [Header("Reposo (mientras espera a la niña)")]
    [Tooltip("Estado del Animator con la animacion de ESPERA, en loop: el moco quieto pero vivo " +
             "(respirando, burbujeando).\n\n" +
             "Se puede dejar VACIO: en ese caso el moco se queda congelado en el primer cuadro de " +
             "'inicio', sin animacion. Funciona, pero quieto del todo se lee mas como escenografia " +
             "que como enemigo.")]
    public string estadoIdle = "";

    [Header("Las tres fases (ida: inicio -> pared -> final)")]
    [Tooltip("El moco brotando. Si no cargaste un idle, en reposo queda congelado en su primer cuadro.")]
    public Fase inicio = new Fase { estadoAnimator = "MocoInicio", duracion = 0.5f, cortaElPaso = false };
    [Tooltip("El moco convertido en pared. Es la fase que CORTA EL PASO.")]
    public Fase pared = new Fase { estadoAnimator = "MocoPared", duracion = 0.4f, cortaElPaso = true };
    [Tooltip("El moco terminado de formar. La vuelta arranca desde aca.")]
    public Fase final = new Fase { estadoAnimator = "MocoFinal", duracion = 0.5f, cortaElPaso = true };

    [Header("Esperas")]
    [Tooltip("Segundos que se queda quieto en la fase FINAL antes de empezar a volver.")]
    public float esperaEnFinal = 1.5f;
    [Tooltip("Segundos que se queda en reposo antes de volver a salir (si la niña sigue cerca).")]
    public float esperaEnReposo = 1f;
    [Tooltip("Segundos que espera al detectarla, antes de empezar a crecer. Le da al jugador un aviso.")]
    public float esperaAntesDeSalir = 0.2f;

    [Header("Collider que corta el paso")]
    [Tooltip("El collider SOLIDO (Is Trigger DESTILDADO) que le impide pasar a la niña. " +
             "Si lo dejas vacio lo busca en este objeto.")]
    public BoxCollider2D colliderBloqueo;
    [Tooltip("Tamaño y posicion del collider cuando el moco esta en reposo (retraido).")]
    public Vector2 tamanoEnReposo = new Vector2(1f, 0.3f);
    public Vector2 offsetEnReposo = Vector2.zero;

    [Header("Daño")]
    [Tooltip("El HurtBox que lastima a la niña. Si lo dejas vacio lo busca en este objeto y sus hijos.")]
    public HurtBox hurtBox;
    [Tooltip("Activo: el moco solo hace daño en las fases que cortan el paso. " +
             "Desactivo: hace daño siempre, incluso retraido.")]
    public bool soloLastimaCuandoCortaElPaso = true;

    [Header("Tamaño segun el hueco donde lo pongas")]
    [Tooltip("Activo: al arrancar la escena el moco MIDE el hueco entre el piso y el techo que tiene " +
             "encima y se estira para taparlo. Asi el mismo prefab sirve en pasillos de distinta " +
             "altura sin escalarlo a mano.\n\n" +
             "Desactivo: usa el Scale del Transform, como cualquier objeto.")]
    public bool ajustarAlHueco = false;
    [Tooltip("Que cuenta como piso y techo. Normalmente el layer Ground.")]
    public LayerMask capaDelEscenario = ~0;
    [Tooltip("Hasta que distancia busca el piso y el techo.")]
    public float distanciaDeBusqueda = 30f;
    [Tooltip("Cuanto le SOBRA o le FALTA para llegar justo. Positivo lo deja mas corto (queda un " +
             "huequito arriba), negativo lo pasa un poco del techo.")]
    public float margen = 0f;
    [Tooltip("Cuanto mide de ALTO el moco dibujado, cuando esta del todo extendido y a escala 1.\n\n" +
             "OJO: no es el alto de la imagen (que es 5.4), sino el del moco que se ve adentro. " +
             "Medilo una vez con el moco en su fase mas alta y anotalo aca: de ese numero sale la cuenta.")]
    public float alturaDelMocoExtendido = 5.4f;
    [Tooltip("Activo: tambien lo ensancha en la misma proporcion, asi no se deforma. " +
             "Desactivo: solo lo estira a lo alto y el ancho queda como esta.")]
    public bool mantenerLaProporcion = false;
    [Tooltip("Activo: al estirarse, la BASE queda clavada donde esta (crece hacia arriba). " +
             "Desactivo: crece para los dos lados desde el centro.")]
    public bool anclarEnLaBase = true;

    [Header("Ayudas visuales")]
    [Tooltip("Dibuja en la Scene el radio de deteccion y el collider de cada fase.")]
    public bool mostrarGizmos = true;
    public Color colorGizmoDeteccion = new Color(1f, 0.9f, 0.2f, 0.9f);

    private Animator animator;
    private Transform jugador;
    private bool cicloEnCurso;
    private bool yaLaDetecte;

    // Lo que el collider tiene puesto AHORA. Sirve de punto de partida de cada estirada.
    private Vector2 tamanoActual;
    private Vector2 offsetActual;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (colliderBloqueo == null) colliderBloqueo = GetComponent<BoxCollider2D>();
        if (hurtBox == null) hurtBox = GetComponentInChildren<HurtBox>(true);

        // Se estira ANTES de que empiece el ciclo, asi el collider ya nace del tamaño correcto.
        if (ajustarAlHueco) AjustarAlHueco();

        PonerCollider(tamanoEnReposo, offsetEnReposo);
        CortarElPaso(false);
    }

    private void Start()
    {
        // Idle en loop si hay, o congelado en el primer cuadro de "inicio" si no.
        PonerEnReposo();
    }

    private void Update()
    {
        if (cicloEnCurso) return;

        bool cerca = LaNiñaEstaCerca();

        // Arranca cuando entra al radio. Si repetirMientrasEsteCerca esta apagado, hay
        // que esperar a que se vaya y vuelva para que se dispare de nuevo.
        if (cerca && (repetirMientrasEsteCerca || !yaLaDetecte))
        {
            yaLaDetecte = true;
            StartCoroutine(Ciclo());
        }
        else if (!cerca)
        {
            yaLaDetecte = false;
        }
    }

    // ---------- El ciclo ----------

    private IEnumerator Ciclo()
    {
        cicloEnCurso = true;

        if (esperaAntesDeSalir > 0f) yield return new WaitForSeconds(esperaAntesDeSalir);

        // IDA: crece hasta ser pared y terminar de formarse.
        yield return Reproducir(inicio, false);
        yield return Reproducir(pared, false);
        yield return Reproducir(final, false);

        // Se queda extendido un rato: es la ventana en la que el paso esta cortado.
        if (esperaEnFinal > 0f) yield return new WaitForSeconds(esperaEnFinal);

        // VUELTA: las mismas tres animaciones, al reves.
        yield return Reproducir(final, true);
        yield return Reproducir(pared, true);
        yield return Reproducir(inicio, true);

        // De nuevo en reposo.
        CortarElPaso(false);
        PonerCollider(tamanoEnReposo, offsetEnReposo);
        PonerEnReposo();

        if (esperaEnReposo > 0f) yield return new WaitForSeconds(esperaEnReposo);

        cicloEnCurso = false;
    }

    // Reproduce una fase y estira el collider hasta donde diga.
    //
    // alReves = true reproduce el clip de atras para adelante (se usa en la vuelta) y
    // lleva el collider al tamaño de REPOSO en vez de al de la fase, porque volviendo
    // el moco se va achicando.
    private IEnumerator Reproducir(Fase fase, bool alReves)
    {
        if (fase == null) yield break;

        CortarElPaso(fase.cortaElPaso);

        if (animator != null && !string.IsNullOrEmpty(fase.estadoAnimator))
        {
            animator.speed = alReves ? -1f : 1f;
            // normalizedTime 1 = el ultimo cuadro: yendo al reves hay que arrancar del final.
            animator.Play(fase.estadoAnimator, 0, alReves ? 1f : 0f);
        }

        Vector2 tamanoDestino = alReves ? tamanoEnReposo : fase.tamanoCollider;
        Vector2 offsetDestino = alReves ? offsetEnReposo : fase.offsetCollider;

        yield return EstirarCollider(tamanoDestino, offsetDestino, fase.duracion);

        if (animator != null) animator.speed = 1f;
    }

    // Lleva el collider de a poco de donde esta al tamaño y posicion que le pidamos,
    // durante los segundos que dure la fase. Asi el collider acompaña al dibujo.
    private IEnumerator EstirarCollider(Vector2 tamanoDestino, Vector2 offsetDestino, float duracion)
    {
        Vector2 tamanoDesde = tamanoActual;
        Vector2 offsetDesde = offsetActual;

        float t = 0f;
        float dur = Mathf.Max(0.01f, duracion);

        while (t < dur)
        {
            t += Time.deltaTime;
            float avance = Mathf.Clamp01(t / dur);

            PonerCollider(Vector2.Lerp(tamanoDesde, tamanoDestino, avance),
                          Vector2.Lerp(offsetDesde, offsetDestino, avance));

            yield return null;
        }

        PonerCollider(tamanoDestino, offsetDestino);
    }

    // ---------- Piezas ----------

    private void PonerCollider(Vector2 tamano, Vector2 offset)
    {
        tamanoActual = tamano;
        offsetActual = offset;

        if (colliderBloqueo == null) return;

        colliderBloqueo.size = new Vector2(Mathf.Max(0.01f, tamano.x), Mathf.Max(0.01f, tamano.y));
        colliderBloqueo.offset = offset;
    }

    // Prende o apaga el bloqueo, y con el mismo criterio el daño (si asi se configuro).
    private void CortarElPaso(bool corta)
    {
        if (colliderBloqueo != null) colliderBloqueo.enabled = corta;

        if (hurtBox != null && soloLastimaCuandoCortaElPaso)
        {
            hurtBox.enabled = corta;
        }
    }

    // Pone el moco en reposo, esperando a la niña.
    //
    // Con animacion de idle cargada, la reproduce en loop (el moco quieto pero vivo).
    // Sin ella, se queda congelado en el primer cuadro de "inicio": funciona igual, pero
    // completamente inmovil se lee mas como parte del decorado que como un enemigo.
    private void PonerEnReposo()
    {
        if (animator == null) return;

        if (!string.IsNullOrEmpty(estadoIdle))
        {
            animator.speed = 1f;
            animator.Play(estadoIdle, 0, 0f);
            return;
        }

        if (inicio == null || string.IsNullOrEmpty(inicio.estadoAnimator)) return;

        animator.speed = 0f; // congelado en el primer cuadro
        animator.Play(inicio.estadoAnimator, 0, 0f);
    }

    private bool LaNiñaEstaCerca()
    {
        if (jugador == null)
        {
            // Por componente y no por tag: en Zona 4 quedo un objeto del escenario
            // tagueado como Player por error y rompia todo sin motivo aparente.
            PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
            if (pc != null) jugador = pc.transform;
        }

        if (jugador == null) return false;

        Vector2 centro = (Vector2)transform.position + offsetDeteccion;
        return Vector2.Distance(centro, jugador.position) <= radioDeteccion;
    }

    // ---------- Tamaño segun el hueco ----------

    // Mide de un rayo cuanto hay entre el piso y el techo, y estira el moco para taparlo.
    //
    // El rayo sale desde el centro del objeto para arriba y para abajo. Se apaga el collider
    // propio antes de tirarlo: si no, el rayo choca contra el mismo moco y mide cualquier cosa.
    [ContextMenu("Medir el hueco y ajustar el tamaño")]
    public void AjustarAlHueco()
    {
        if (alturaDelMocoExtendido <= 0.01f)
        {
            Debug.LogWarning("EnemigoMoco: 'Altura Del Moco Extendido' tiene que ser mayor que cero.", this);
            return;
        }

        bool colliderEstaba = colliderBloqueo != null && colliderBloqueo.enabled;
        if (colliderBloqueo != null) colliderBloqueo.enabled = false;

        Vector2 origen = transform.position;
        RaycastHit2D techo = Physics2D.Raycast(origen, Vector2.up, distanciaDeBusqueda, capaDelEscenario);
        RaycastHit2D piso = Physics2D.Raycast(origen, Vector2.down, distanciaDeBusqueda, capaDelEscenario);

        if (colliderBloqueo != null) colliderBloqueo.enabled = colliderEstaba;

        if (techo.collider == null || piso.collider == null)
        {
            Debug.LogWarning("EnemigoMoco: no encontre " +
                             (techo.collider == null ? "TECHO" : "PISO") +
                             " desde este moco. Revisa que el escenario este en la capa elegida " +
                             "en 'Capa Del Escenario', o apaga 'Ajustar Al Hueco' y escalalo a mano.", this);
            return;
        }

        float hueco = (techo.point.y - piso.point.y) - margen;
        if (hueco <= 0.01f)
        {
            Debug.LogWarning("EnemigoMoco: el hueco medido dio " + hueco.ToString("F2") +
                             ". Revisa el margen.", this);
            return;
        }

        float escala = hueco / alturaDelMocoExtendido;

        Vector3 nueva = transform.localScale;
        nueva.y = escala;
        if (mantenerLaProporcion) nueva.x = escala * Mathf.Sign(nueva.x == 0f ? 1f : nueva.x);
        transform.localScale = nueva;

        // Al escalar desde el centro, la base se hunde o se despega. La reapoyamos en el piso.
        if (anclarEnLaBase)
        {
            float mitadAhora = (alturaDelMocoExtendido * escala) * 0.5f;
            Vector3 pos = transform.position;
            pos.y = piso.point.y + mitadAhora;
            transform.position = pos;
        }

        Debug.Log("EnemigoMoco: hueco de " + hueco.ToString("F2") + " unidades -> escala Y " +
                  escala.ToString("F2"), this);
    }

    // ---------- Gizmos ----------

    private void OnDrawGizmos()
    {
        if (!mostrarGizmos) return;

        // Radio de deteccion.
        Gizmos.color = colorGizmoDeteccion;
        Gizmos.DrawWireSphere((Vector2)transform.position + offsetDeteccion, radioDeteccion);

        // Los rayos con los que mide el hueco, para ver si dan contra lo que uno espera.
        if (ajustarAlHueco)
        {
            Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.9f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * distanciaDeBusqueda);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * distanciaDeBusqueda);
        }

        // El collider de cada fase, para acomodarlos a ojo sin tener que darle Play.
        DibujarCaja(tamanoEnReposo, offsetEnReposo, new Color(0.5f, 0.5f, 0.5f, 0.8f)); // reposo
        DibujarCaja(inicio.tamanoCollider, inicio.offsetCollider, new Color(0.3f, 0.8f, 1f, 0.8f));
        DibujarCaja(pared.tamanoCollider, pared.offsetCollider, new Color(1f, 0.3f, 0.3f, 0.9f)); // el que corta
        DibujarCaja(final.tamanoCollider, final.offsetCollider, new Color(1f, 0.6f, 0.2f, 0.8f));
    }

    private void DibujarCaja(Vector2 tamano, Vector2 offset, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube((Vector2)transform.position + offset, tamano);
    }
}
