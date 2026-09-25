using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// ============================================================
//  BOSS FINAL (Zona 6)
//
//  Flota en el medio del ring y repite esta vuelta:
//     1. Lluvia de estalactitas.
//     2. Abanico de bolas.
//     3. Invoca mini bichos. Cuando la niña los mata a todos...
//     4. ...el boss CAE al piso mareado y ahi se le puede pegar.
//     5. Se vuelve a elevar y arranca de nuevo.
//
//  Son 3 caidas de 3 golpes. En la ultima fase, despues de los bichos, hace el
//  DERRUMBE (filas de estalactitas con un hueco + embestidas) y recien ahi cae.
//
//  Va EN el objeto del boss (el que tiene el SpriteRenderer). Al agregarlo se suman
//  solos el HealthHandler y el Damageable. La vida se calcula sola.
//
//  Todo lo visual es opcional: si no hay prefabs ni dibujos, usa cuadrados de colores
//  para poder probar la pelea antes de tener el arte.
// ============================================================
[DefaultExecutionOrder(-50)] // antes que el HealthHandler, para poder fijarle la vida maxima
[RequireComponent(typeof(HealthHandler))]
[RequireComponent(typeof(Damageable))]
public class BossFinal : MonoBehaviour
{
    [System.Serializable]
    public class FaseDelBoss
    {
        public string nombre = "Fase";
        [Tooltip("Cuantas estalactitas tira en la lluvia. 0 = no hace este ataque.")]
        [Min(0)] public int estalactitas = 4;
        [Tooltip("Cuantas bolas tira en cada abanico. 0 = no hace este ataque.")]
        [Min(0)] public int bolas = 3;
        [Tooltip("Los bichos que invoca, en orden. Se pueden repetir.")]
        public TipoDeBicho[] bichos = { TipoDeBicho.Rastrero, TipoDeBicho.Volador };
        [Tooltip("Despues de los bichos hace el ataque final (derrumbe) antes de caer.")]
        public bool terminaConDerrumbe = false;
        [Tooltip("1 = normal. Mas alto = ataques y avisos mas rapidos.")]
        [Range(0.5f, 2f)] public float velocidad = 1f;
    }

    //  1. CAMPOS DEL INSPECTOR

    [Header("Cuando empieza la pelea")]
    [Tooltip("La cinematica del final. Cuando TERMINA, arranca la pelea. Se engancha sola.")]
    public CinematicaFrames cinematica;
    [Tooltip("PARA PROBAR: arranca la pelea apenas das Play, sin cinematica. Destildalo al terminar.")]
    public bool empezarAlDarPlay = false;
    [Tooltip("Si esta activo, el boss es invisible hasta que empieza la pelea y aparece con un fundido.")]
    public bool ocultoHastaQueEmpiece = false;
    [Tooltip("Donde reaparece la niña si muere en la pelea (un objeto vacio adentro del ring).")]
    public Transform puntoDeReaparicion;
    [Tooltip("Segundos entre que reaparece la niña y que el boss vuelve a atacar.")]
    public float esperaAlReaparecer = 1.5f;

    [Header("El ring")]
    [Tooltip("Objeto vacio en la esquina de ARRIBA a la IZQUIERDA del ring (a la altura del techo).")]
    public Transform esquinaSuperiorIzquierda;
    [Tooltip("Objeto vacio en la esquina de ABAJO a la DERECHA del ring (a la altura del piso).")]
    public Transform esquinaInferiorDerecha;
    [Tooltip("De donde salen los bichos. Objetos vacios apoyados en el piso (o en el aire para los voladores). " +
             "Si lo dejas vacio, salen en lugares al azar.")]
    public Transform[] grietas;
    [Tooltip("La Cinemachine Camera fija que muestra todo el ring. Se prende sola al empezar la pelea.")]
    public CinemachineCamera camaraDelRing;
    public bool volverALaCamaraNormalAlGanar = true;

    [Header("Musica")]
    public AudioClip musicaDelBoss;
    [Tooltip("Al ganar vuelve la musica de la zona. Si lo destildas, queda en silencio.")]
    public bool volverALaMusicaDeLaZonaAlGanar = true;

    [Header("Vida y fases")]
    [Tooltip("Golpes que hay que darle en cada caida.")]
    [Min(1)] public int golpesPorCaida = 3;
    [Tooltip("Una fase por caida. La vida total es (fases x golpes por caida).")]
    public FaseDelBoss[] fases =
    {
        new FaseDelBoss { nombre = "Fase 1", estalactitas = 4, bolas = 3,
                          bichos = new[] { TipoDeBicho.Rastrero, TipoDeBicho.Volador }, velocidad = 1f },
        new FaseDelBoss { nombre = "Fase 2", estalactitas = 6, bolas = 5,
                          bichos = new[] { TipoDeBicho.Rastrero, TipoDeBicho.Volador, TipoDeBicho.Rastrero, TipoDeBicho.Volador }, velocidad = 1.15f },
        new FaseDelBoss { nombre = "Fase final", estalactitas = 6, bolas = 5,
                          bichos = new[] { TipoDeBicho.Rastrero, TipoDeBicho.Volador, TipoDeBicho.Saltarin, TipoDeBicho.Saltarin },
                          terminaConDerrumbe = true, velocidad = 1.3f },
    };

    [Header("Barra de vida")]
    public bool mostrarBarraDeVida = true;
    [Tooltip("Nombre que aparece arriba de la barra. Vacio = sin nombre.")]
    public string nombreEnLaBarra = "";
    public TMP_FontAsset fuenteDelNombre;
    public Color colorDeLaBarra = new Color(0.75f, 0.3f, 1f);

    [Header("Como se ve vivo")]
    [Tooltip("Cuanto sube y baja mientras flota.")]
    public float alturaDeFlotacion = 0.3f;
    public float velocidadDeFlotacion = 1.5f;
    [Tooltip("Cuanto se agranda y achica al 'respirar'. 0 = nada.")]
    public float respiracion = 0.04f;
    [Tooltip("Cuanto tiembla cuando avisa que va a atacar.")]
    public float temblorAlAvisar = 0.12f;
    public Color colorDeAviso = new Color(1f, 0.65f, 0.65f);
    public Color colorDeFuria = new Color(1f, 0.35f, 0.35f);
    public Color colorAlRecibirGolpe = new Color(1f, 1f, 1f, 0.4f);

    [Header("Tiempos generales")]
    [Tooltip("Segundos que tiembla avisando antes de cada ataque.")]
    public float tiempoDeAviso = 0.8f;
    public float pausaEntreAtaques = 1.2f;

    [Header("Ataque 1: lluvia de estalactitas")]
    [Tooltip("Los prefabs de estalactita. Si pones varios, elige uno al azar en cada caida " +
             "(asi la lluvia no se ve toda igual). Vacio = cuadrado violeta de prueba.")]
    public GameObject[] prefabsDeEstalactita;
    [Tooltip("Segundos que cada estalactita tiembla en el techo antes de caer.")]
    public float avisoDeCadaEstalactita = 0.9f;
    public float intervaloEntreEstalactitas = 0.25f;
    [Tooltip("La primera cae justo arriba de la niña, asi tiene que moverse si o si.")]
    public bool unaApuntaALaNina = true;

    [Header("Ataque 2: abanico de bolas")]
    [Tooltip("Vacio = cuadrado violeta de prueba.")]
    public GameObject prefabBola;
    public float velocidadDeLasBolas = 7f;
    [Tooltip("Grados de apertura del abanico.")]
    public float anguloDelAbanico = 60f;
    [Min(1)] public int rafagas = 2;
    public float esperaEntreRafagas = 0.6f;

    [Header("Ataque 3: mini bichos")]
    [Tooltip("Vacio = cuadrado verde de prueba.")]
    public GameObject prefabRastrero;
    [Tooltip("Vacio = cuadrado celeste de prueba.")]
    public GameObject prefabVolador;
    [Tooltip("Vacio = cuadrado naranja de prueba.")]
    public GameObject prefabSaltarin;
    public float esperaEntreBichos = 0.4f;
    [Tooltip("Si la ola dura mas que esto (un bicho trabado), se eliminan los que quedan y el boss cae igual.")]
    public float tiempoMaximoDeOla = 40f;

    [Header("Orbes de vida que sueltan los bichos")]
    [Tooltip("Arrastrar el prefab del orbe (Assets/PreFabs/OrbeVida). Vacio = no sueltan nada.")]
    public GameObject prefabOrbeDeVida;
    [Range(0f, 1f)] public float probabilidadDeOrbe = 0.25f;
    [Min(0)] public int maximoDeOrbesPorOla = 1;

    [Header("Ataque final: derrumbe")]
    [Min(1)] public int filasDeEstalactitas = 3;
    [Tooltip("Distancia entre estalactitas de una misma fila.")]
    public float separacionEnLaFila = 1.2f;
    [Tooltip("Ancho de cada hueco libre donde se tiene que meter la niña. Mas ancho = mas facil.")]
    public float anchoDelHueco = 4f;
    [Tooltip("Cuantos huecos deja la PRIMERA fila. Cada fila siguiente deja uno menos, " +
             "hasta que la ultima deja uno solo. Con 3 filas y 3 huecos: 3, 2 y 1.")]
    [Min(1)] public int huecosEnLaPrimeraFila = 3;
    public float avisoDeLaFila = 1.1f;
    [Tooltip("Entre fila y fila cruza el ring de punta a punta.")]
    public bool embestirEntreFilas = true;
    [Tooltip("Altura del borde de ABAJO del boss sobre el piso durante la embestida. " +
             "Bajo = hay que saltarlo. Alto = hay que quedarse abajo.")]
    public float alturaDeLaEmbestida = 0.2f;
    public float velocidadDeLaEmbestida = 14f;
    public float avisoDeLaEmbestida = 0.8f;

    [Header("Esconderse de la embestida (click derecho)")]
    [Tooltip("La niña puede agacharse y meterse bajo el piso mientras el boss embiste.")]
    public bool sePuedeEsconder = true;
    [Tooltip("El cartel grande que aparece la PRIMERA vez, para enseñar la mecanica.")]
    public string textoDelCartel = "manten CLICK DERECHO para esconderte";
    [Tooltip("Cuantos segundos avisa el boss ANTES de la primera embestida (mas largo, para " +
             "que de tiempo a leer el cartel). Las siguientes usan 'Aviso De La Embestida'.")]
    public float avisoDeLaPrimeraEmbestida = 2.2f;
    [Tooltip("Cuanto se queda el cartel en pantalla.")]
    public float duracionDelCartel = 2.5f;
    [Tooltip("Cuanto se hunde la niña, en unidades.")]
    public float profundidadDelEscondite = 1.4f;
    [Tooltip("Cuanto tarda en meterse y en salir.")]
    public float duracionDelEscondite = 0.25f;
    [Tooltip("Valor de 'stateAnim' de la niña mientras esta escondida. 0 = no tocar la animacion. " +
             "Cuando tengas la animacion de agacharse, pone aca su numero.")]
    public int animacionAgachada = 0;
    public AudioClip sonidoAlEsconderse;
    public AudioClip sonidoAlSalirDelEscondite;

    [Header("La caida (cuando se le puede pegar)")]
    [Tooltip("Dibujo mareado en el piso. Vacio = usa el mismo y se bambolea.")]
    public Sprite spriteCaido;

    [Header("Animacion (si usas un Animator)")]
    [Tooltip("El Animator del boss. Si lo dejas VACIO, el boss usa los dibujos sueltos de abajo.\n\n" +
             "Si lo ponés, el script NO toca el sprite: le avisa al Animator en que estado esta, " +
             "con el numero del parametro de abajo.")]
    public Animator animador;
    [Tooltip("Nombre EXACTO del parametro Int del Animator (como el 'stateAnim' de la niña).")]
    public string parametroDeEstado = "estado";
    [Tooltip("Valor del parametro cuando esta flotando tranquilo.")]
    public int animFlotando = 1;
    [Tooltip("Valor cuando esta avisando que va a atacar (tiembla).")]
    public int animAvisando = 2;
    [Tooltip("Valor cuando esta caido en el piso (cuando se le puede pegar).")]
    public int animCaido = 3;
    [Tooltip("Valor cuando esta enfurecido (el derrumbe final).")]
    public int animFuria = 4;
    [Tooltip("Valor cuando se esta muriendo.")]
    public int animMuerte = 5;
    [Tooltip("Segundos que queda en el piso. Si no le pegaste todo, se levanta igual y repite la fase.")]
    public float tiempoCaido = 4.5f;
    [Tooltip("Corrige a que altura queda el boss cuando esta apoyado en el piso (al caerse Y en la embestida). El juego lo calcula con el " +
             "rectangulo del dibujo, asi que si tu animacion tiene espacio transparente abajo, " +
             "el boss frena en el aire. Bajá este numero (por ejemplo -2) hasta que quede apoyado.")]
    public float ajusteDeAlturaAlCaer = 0f;
    public float tiempoParaLevantarse = 1f;

    [Header("Daño por tocarlo")]
    [Tooltip("Si la niña lo toca mientras flota o embiste, se lastima. Caido no hace daño.")]
    public bool lastimaAlTocarlo = true;
    public int danioPorContacto = 1;
    public float empujePorContacto = 8f;

    [Header("Muerte")]
    public float duracionDeLaMuerte = 2.5f;
    [Tooltip("La MISMA animacion de muerte que usan los monstruos del juego (un prefab suelto " +
             "con su Animator y el script AutoDestruir). Vacio = solo los pufs.")]
    public GameObject prefabDeLaMuerte;
    [Tooltip("Como el boss es grande, conviene agrandarla. 3 es un buen punto de partida.")]
    public float escalaDelEfectoDeMuerte = 3f;
    [Tooltip("Cuantas veces aparece mientras se muere, repartidas por el cuerpo. 1 = solo una al final.")]
    [Min(1)] public int cuantasVecesAparece = 3;
    public int puntosPorVencerlo = 500;

    [Header("Sonidos (todos opcionales)")]
    public AudioClip sonidoRugido;
    public AudioClip sonidoAviso;
    public AudioClip sonidoDisparo;
    public AudioClip sonidoInvocar;
    public AudioClip sonidoCaida;
    public AudioClip sonidoGolpe;
    public AudioClip sonidoLevantarse;
    public AudioClip sonidoMuerte;
    [Range(0f, 1f)] public float volumen = 0.9f;

    [Header("Eventos")]
    [Tooltip("Al empezar la pelea (por ejemplo, cerrar una puerta).")]
    public UnityEvent alEmpezar;
    [Tooltip("Al vencerlo (por ejemplo, la cinematica final o abrir el camino).")]
    public UnityEvent alGanar;

    //  2. ESTADO

    private static readonly Color COLOR_PELIGRO = new Color(0.75f, 0.3f, 1f);

    private HealthHandler vida;
    private Damageable damageable;
    private SpriteRenderer sr;
    private Collider2D cuerpo;
    private Sprite spriteNormal;
    private Color colorOriginal;
    private Vector3 escalaOriginal;
    private Vector3 posicionDeReposo;
    private Vector3 posLogica;   // donde "esta" el boss; el temblor y la flotacion se suman encima

    private bool peleando, muerto, caido, avisando, furia, oculto;
    private bool flotando = true;
    private float temblando, flash, aplastado, cooldownContacto;
    private float alpha = 1f;
    private int capaGolpeable = -1, capaIntocable = 2;
    private int golpesTotales, vidaAnterior, orbesEnEstaOla;

    private EscondersePlayer escondite;
    private int embestidasHechas;
    private TextMeshProUGUI cartelGrande;
    private Coroutine cartelCo;

    private readonly List<MiniBicho> vivos = new List<MiniBicho>();
    private readonly List<GameObject> peligros = new List<GameObject>();

    // Barra de vida (se arma por codigo)
    private CanvasGroup grupoBarra;
    private RectTransform relleno;
    private float rellenoMostrado = 1f, rellenoObjetivo = 1f, alphaBarraObjetivo;

    //  3. ARRANQUE

    private void Awake()
    {
        vida = GetComponent<HealthHandler>();
        damageable = GetComponent<Damageable>();
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        if (sr == null)
        {
            Debug.LogError("BossFinal: el boss necesita un SpriteRenderer (en el mismo objeto o en un hijo).", this);
            enabled = false;
            return;
        }
        if (sr.sprite == null) sr.sprite = UtilBoss.Cuadrado();

        if (fases == null || fases.Length == 0) fases = new[] { new FaseDelBoss() };

        // La vida sale de las fases: 3 caidas x 3 golpes = 9. Se fija ANTES del Awake del
        // HealthHandler (por eso el DefaultExecutionOrder), asi arranca con la vida llena.
        golpesTotales = fases.Length * Mathf.Max(1, golpesPorCaida);
        vida.maxHealth = golpesTotales;
        vida.destroyOnDeath = false;

        // El boss no sale volando al pegarle, y los golpes se cuentan todos.
        damageable.activeKnockBack = false;
        damageable.activeInvulnerability = false;

        // El cuerpo es trigger: la niña lo atraviesa (el daño por contacto lo maneja este script).
        cuerpo = GetComponent<Collider2D>();
        if (cuerpo == null) cuerpo = gameObject.AddComponent<BoxCollider2D>();
        cuerpo.isTrigger = true;

        spriteNormal = sr.sprite;
        colorOriginal = sr.color;
        escalaOriginal = transform.localScale;
        posicionDeReposo = transform.position;
        posLogica = posicionDeReposo;

        int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        capaIntocable = ignoreRaycast >= 0 ? ignoreRaycast : 2;

        vida.OnHealthChanged += AlCambiarLaVida;
        vida.OnDeath += AlMorir;

        // La camara del ring arranca apagada: se prende al empezar la pelea.
        if (camaraDelRing != null) camaraDelRing.gameObject.SetActive(false);
    }

    private void Start()
    {
        vidaAnterior = vida.CurrentHealth;

        capaGolpeable = UtilBoss.CapaDeEnemigos();
        if (capaGolpeable < 0)
        {
            capaGolpeable = gameObject.layer;
            Debug.LogWarning("BossFinal: no encontre el 'Enemy Layer' de PlayerAttacks. Uso el layer del boss.", this);
        }
        SetGolpeable(false);

        // Ya lo vencieron en esta partida: no vuelve.
        if (ProgresoJuego.YaMostrado(ClaveDerrotado()))
        {
            Debug.Log("[Boss] '" + name + "' no aparece porque YA LO VENCISTE en esta partida. " +
                      "Para volver a pelear: menu 'Not Home > Borrar progreso guardado'.", this);
            gameObject.SetActive(false);
            return;
        }

        if (ocultoHastaQueEmpiece)
        {
            oculto = true;
            alpha = 0f;
        }

        if (cinematica != null) cinematica.alTerminar.AddListener(Empezar);

        // Si la pelea ya habia empezado (la niña murio) o estamos probando, la niña va
        // directo al ring y la pelea arranca sola.
        if (ProgresoJuego.YaMostrado(ClaveEmpezada()) || empezarAlDarPlay) StartCoroutine(VolverALaPelea());
    }

    private void OnDestroy()
    {
        if (cinematica != null) cinematica.alTerminar.RemoveListener(Empezar);
        if (vida != null)
        {
            vida.OnHealthChanged -= AlCambiarLaVida;
            vida.OnDeath -= AlMorir;
        }
        if (grupoBarra != null) Destroy(grupoBarra.transform.root.gameObject);
    }

    // La niña murio en la pelea y se recargo la escena: la ponemos en el ring y seguimos,
    // sin tener que ver la cinematica de nuevo.
    private IEnumerator VolverALaPelea()
    {
        PlayerController pc = UtilBoss.Jugador();

        if (puntoDeReaparicion != null && pc != null)
        {
            pc.transform.position = puntoDeReaparicion.position;
            if (pc.rb != null) pc.rb.linearVelocity = Vector2.zero;
        }
        else if (puntoDeReaparicion == null)
        {
            Debug.LogWarning("BossFinal: falta el 'Punto De Reaparicion'. La niña va a aparecer al principio de la zona.", this);
        }

        ActivarCamaraDelRing();

        yield return new WaitForSeconds(esperaAlReaparecer);
        Empezar();
    }

    //  4. LA PELEA

    [ContextMenu("Empezar la pelea ahora")]
    public void Empezar()
    {
        if (peleando || muerto || !isActiveAndEnabled) return;
        peleando = true;

        ProgresoJuego.MarcarMostrado(ClaveEmpezada());

        ActivarCamaraDelRing();
        if (musicaDelBoss != null && AudioManager.Instance != null) AudioManager.Instance.ReproducirMusicaEspecial(musicaDelBoss);
        if (mostrarBarraDeVida) MostrarBarra();

        alEmpezar?.Invoke();
        StartCoroutine(Pelea());
    }

    private IEnumerator Pelea()
    {
        yield return Presentarse();

        while (!muerto)
        {
            int indice = FaseActual();
            FaseDelBoss fase = fases[indice];
            float v = Mathf.Max(0.1f, fase.velocidad);

            yield return new WaitForSeconds(pausaEntreAtaques / v);

            if (fase.estalactitas > 0)
            {
                yield return LluviaDeEstalactitas(fase.estalactitas, v);
                yield return new WaitForSeconds(pausaEntreAtaques / v);
            }

            if (fase.bolas > 0)
            {
                yield return AbanicoDeBolas(fase.bolas, v);
                yield return new WaitForSeconds(pausaEntreAtaques / v);
            }

            if (fase.bichos != null && fase.bichos.Length > 0) yield return Invocar(fase.bichos, v);

            if (fase.terminaConDerrumbe) yield return Derrumbe(v);

            yield return Caer(indice);
        }
    }

    private IEnumerator Presentarse()
    {
        if (oculto)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime;
                alpha = Mathf.Clamp01(t);
                yield return null;
            }
            alpha = 1f;
            oculto = false;
        }

        UtilBoss.Sonar(sonidoRugido, volumen, transform.position);
        UtilBoss.Sacudir(1f, 0.6f);
        yield return Avisar(1f, null);
    }

    // Que fase toca segun los golpes recibidos (0, 1, 2...).
    private int FaseActual()
    {
        int golpes = golpesTotales - vida.CurrentHealth;
        return Mathf.Clamp(golpes / Mathf.Max(1, golpesPorCaida), 0, fases.Length - 1);
    }

    //  5. ATAQUES

    private IEnumerator LluviaDeEstalactitas(int cantidad, float v)
    {
        yield return Avisar(tiempoDeAviso / v, sonidoAviso);
        UtilBoss.Sacudir(0.5f, 0.4f);

        Rect ring = Ring();
        List<float> usadas = new List<float>();

        for (int i = 0; i < cantidad; i++)
        {
            float x = (i == 0 && unaApuntaALaNina)
                ? Mathf.Clamp(UtilBoss.PosicionJugador().x, ring.xMin + 0.5f, ring.xMax - 0.5f)
                : XLibre(ring, usadas, 1f);

            usadas.Add(x);
            CrearEstalactita(x, avisoDeCadaEstalactita / v);
            yield return new WaitForSeconds(intervaloEntreEstalactitas / v);
        }

        yield return new WaitForSeconds(avisoDeCadaEstalactita / v + 0.4f);
    }

    private IEnumerator AbanicoDeBolas(int cantidad, float v)
    {
        for (int r = 0; r < rafagas; r++)
        {
            yield return Avisar((r == 0 ? tiempoDeAviso : tiempoDeAviso * 0.5f) / v, sonidoAviso);

            Vector2 origen = sr.bounds.center;
            Vector2 haciaLaNina = (UtilBoss.PosicionJugador() - origen).normalized;
            if (haciaLaNina == Vector2.zero) haciaLaNina = Vector2.down;

            UtilBoss.Sonar(sonidoDisparo, volumen, origen);
            aplastado = -0.15f;   // se estira al disparar

            for (int k = 0; k < cantidad; k++)
            {
                float angulo = cantidad == 1 ? 0f : Mathf.Lerp(-anguloDelAbanico * 0.5f, anguloDelAbanico * 0.5f, k / (float)(cantidad - 1));
                Vector2 dir = Quaternion.Euler(0f, 0f, angulo) * haciaLaNina;
                CrearBola(origen, dir, velocidadDeLasBolas * v);
            }

            if (r < rafagas - 1) yield return new WaitForSeconds(esperaEntreRafagas / v);
        }
    }

    private IEnumerator Invocar(TipoDeBicho[] bichos, float v)
    {
        yield return Avisar(tiempoDeAviso / v, sonidoInvocar);
        UtilBoss.Sacudir(0.6f, 0.3f);

        orbesEnEstaOla = 0;
        Rect ring = Ring();

        for (int i = 0; i < bichos.Length; i++)
        {
            CrearBicho(bichos[i], PuntoDeAparicion(bichos[i], i, ring));
            yield return new WaitForSeconds(esperaEntreBichos / v);
        }

        // Esperamos a que la niña los mate a todos.
        float t = 0f;
        while (t < tiempoMaximoDeOla)
        {
            vivos.RemoveAll(b => b == null || b.Muerto);
            if (vivos.Count == 0) break;

            t += Time.deltaTime;
            yield return null;
        }

        EliminarBichos();
        yield return new WaitForSeconds(0.4f);
    }

    // EL ATAQUE FINAL: sube, se enfurece, tira filas de estalactitas con un solo hueco
    // y entre fila y fila embiste de punta a punta del ring.
    private IEnumerator Derrumbe(float v)
    {
        Rect ring = Ring();
        Vector3 arriba = new Vector3(ring.center.x, ring.yMax - MitadDelAlto() - 0.3f, posLogica.z);

        furia = true;
        UtilBoss.Sonar(sonidoRugido, volumen, transform.position);
        UtilBoss.Sacudir(1.3f, 0.8f);
        yield return MoverA(arriba, 1f);
        yield return Avisar(1f, null);

        for (int fila = 0; fila < filasDeEstalactitas; fila++)
        {
            // Primera fila: varios huecos. Cada fila deja uno menos, hasta que queda uno solo.
            yield return FilaDeEstalactitas(v, huecosEnLaPrimeraFila - fila);

            bool quedanFilas = fila < filasDeEstalactitas - 1;
            if (embestirEntreFilas && quedanFilas)
            {
                yield return Embestida(v);
                yield return MoverA(arriba, 0.7f);
            }
        }
    }

    // 'huecos' es cuantos espacios libres deja esta fila. La primera deja varios (es facil
    // llegar a alguno) y van bajando hasta que queda uno solo: la fila mas dificil es la ultima.
    private IEnumerator FilaDeEstalactitas(float v, int huecos)
    {
        Rect ring = Ring();
        float mitadHueco = anchoDelHueco * 0.5f;
        huecos = Mathf.Max(1, huecos);

        // Repartimos los huecos: uno por cada franja del ring, en un lugar al azar de su franja.
        // Asi nunca quedan dos pegados ni todos juntos de un lado.
        List<float> centros = new List<float>();
        float franja = ring.width / huecos;

        for (int i = 0; i < huecos; i++)
        {
            float minX = ring.xMin + franja * i + mitadHueco + 0.3f;
            float maxX = ring.xMin + franja * (i + 1) - mitadHueco - 0.3f;

            centros.Add(maxX > minX ? Random.Range(minX, maxX) : ring.xMin + franja * (i + 0.5f));
        }

        float paso = Mathf.Max(0.3f, separacionEnLaFila);
        for (float x = ring.xMin + paso * 0.5f; x <= ring.xMax - paso * 0.5f + 0.001f; x += paso)
        {
            bool enUnHueco = false;
            foreach (float c in centros)
            {
                if (Mathf.Abs(x - c) <= mitadHueco) { enUnHueco = true; break; }
            }
            if (enUnHueco) continue;

            CrearEstalactita(x, avisoDeLaFila / v);
        }

        UtilBoss.Sacudir(0.7f, 0.5f);
        yield return new WaitForSeconds(avisoDeLaFila / v + 0.6f);
    }

    private IEnumerator Embestida(float v)
    {
        Rect ring = Ring();
        float mitadAncho = MitadDelAncho();
        // Misma cuenta que al caerse: asi el ajuste de altura vale para los dos.
        float y = ring.yMin + alturaDeLaEmbestida + AlturaDeApoyo();

        // Arranca del lado opuesto a la niña, asi tiene tiempo de verlo venir.
        bool ninaALaIzquierda = UtilBoss.PosicionJugador().x < ring.center.x;
        float xInicio = ninaALaIzquierda ? ring.xMax - mitadAncho : ring.xMin + mitadAncho;
        float xFin = ninaALaIzquierda ? ring.xMin + mitadAncho : ring.xMax - mitadAncho;

        flotando = false;

        // Se abre la ventana para esconderse: desde que se va al costado hasta que termina
        // de cruzar. La PRIMERA embestida avisa mas tiempo y muestra el cartel que enseña
        // la mecanica; las siguientes avisan menos.
        bool primera = embestidasHechas == 0;
        embestidasHechas++;

        AbrirElEscondite(true);
        if (primera && sePuedeEsconder) MostrarCartelGrande(textoDelCartel, duracionDelCartel);

        yield return MoverA(new Vector3(xInicio, y, posLogica.z), 0.7f / v);

        float aviso = primera ? avisoDeLaPrimeraEmbestida : avisoDeLaEmbestida;
        yield return Avisar(aviso / v, sonidoAviso);

        UtilBoss.Sonar(sonidoRugido, volumen, transform.position);
        float velocidad = velocidadDeLaEmbestida * v;
        while (Mathf.Abs(posLogica.x - xFin) > 0.01f)
        {
            posLogica.x = Mathf.MoveTowards(posLogica.x, xFin, velocidad * Time.deltaTime);
            yield return null;
        }

        UtilBoss.Sacudir(0.9f, 0.3f);
        aplastado = 0.2f;
        flotando = true;

        AbrirElEscondite(false);
    }

    // Prende y apaga la posibilidad de esconderse. La primera vez le agrega el script a la
    // niña y le pasa los ajustes, asi no hay que configurar nada a mano.
    private void AbrirElEscondite(bool abierta)
    {
        if (!sePuedeEsconder) return;

        if (escondite == null)
        {
            PlayerController pc = UtilBoss.Jugador();
            if (pc == null) return;

            escondite = pc.GetComponent<EscondersePlayer>();
            if (escondite == null) escondite = pc.gameObject.AddComponent<EscondersePlayer>();
        }

        escondite.profundidad = profundidadDelEscondite;
        escondite.duracion = duracionDelEscondite;
        escondite.animAgachada = animacionAgachada;
        escondite.sonidoAlEsconderse = sonidoAlEsconderse;
        escondite.sonidoAlSalir = sonidoAlSalirDelEscondite;
        escondite.volumen = volumen;

        if (abierta) escondite.ventanaAbierta = true;
        else escondite.SacarDelEscondite();
    }

    //  6. LA CAIDA (la ventana para pegarle)

    private IEnumerator Caer(int indiceDeFase)
    {
        caido = true;
        flotando = false;
        furia = false;
        if (animador == null && spriteCaido != null) sr.sprite = spriteCaido;

        // Cae acelerando hasta el piso del ring.
        float yPiso = Ring().yMin + AlturaDeApoyo();
        float velocidad = 0f;
        while (posLogica.y > yPiso)
        {
            velocidad += 35f * Time.deltaTime;
            posLogica.y = Mathf.Max(yPiso, posLogica.y - velocidad * Time.deltaTime);
            yield return null;
        }

        UtilBoss.Sacudir(1.2f, 0.4f);
        UtilBoss.Sonar(sonidoCaida, volumen, transform.position);
        EfectoPuf.Crear(new Vector3(posLogica.x, Ring().yMin + 0.1f, 0f), new Color(0.6f, 0.55f, 0.5f), 14, 0.25f, 5f);
        aplastado = 0.3f;

        SetGolpeable(true);

        // Se queda hasta que le den los golpes de esta fase (o se acabe el tiempo).
        int vidaParaLevantarse = golpesTotales - (indiceDeFase + 1) * golpesPorCaida;
        float t = 0f;
        while (t < tiempoCaido && vida.CurrentHealth > vidaParaLevantarse && !muerto)
        {
            t += Time.deltaTime;
            yield return null;
        }

        SetGolpeable(false);
        if (muerto) yield break;

        yield return new WaitForSeconds(0.3f);

        UtilBoss.Sonar(sonidoLevantarse, volumen, transform.position);
        if (animador == null) sr.sprite = spriteNormal;
        caido = false;
        yield return MoverA(posicionDeReposo, tiempoParaLevantarse);
        flotando = true;
    }

    // Solo mientras esta caido lo encuentra el ataque de la niña.
    private void SetGolpeable(bool golpeable)
    {
        gameObject.layer = golpeable ? capaGolpeable : capaIntocable;
    }

    //  7. GOLPES Y MUERTE

    private void AlCambiarLaVida(int vidaActual)
    {
        if (vidaActual < vidaAnterior)
        {
            flash = 0.12f;
            aplastado = 0.2f;
            UtilBoss.Sonar(sonidoGolpe, volumen, transform.position);
            UtilBoss.Sacudir(0.35f, 0.15f);
            EfectoPuf.Crear(sr.bounds.center, Color.white, 6, 0.15f, 4f);
        }

        vidaAnterior = vidaActual;
        rellenoObjetivo = golpesTotales > 0 ? Mathf.Clamp01(vidaActual / (float)golpesTotales) : 0f;
    }

    private void AlMorir()
    {
        if (muerto) return;
        muerto = true;

        StopAllCoroutines();
        StartCoroutine(Muerte());
    }

    private IEnumerator Muerte()
    {
        peleando = false;
        caido = true;
        flotando = false;
        furia = false;
        SetGolpeable(false);

        AbrirElEscondite(false);
        if (escondite != null) escondite.ventanaAbierta = false;
        EliminarBichos();
        foreach (GameObject p in peligros) if (p != null) Destroy(p);
        peligros.Clear();

        ProgresoJuego.MarcarMostrado(ClaveDerrotado());
        UtilBoss.Sonar(sonidoMuerte, volumen, transform.position);

        // Tiembla y larga "pufs" por todos lados. Si hay animacion de muerte, van apareciendo
        // varias repartidas por el cuerpo mientras se desarma.
        float t = 0f;
        float proximoPuf = 0f;
        float entreMuertes = duracionDeLaMuerte / Mathf.Max(1, cuantasVecesAparece);
        float proximaMuerte = 0f;

        while (t < duracionDeLaMuerte)
        {
            t += Time.deltaTime;
            temblando = 1f;

            Bounds b = Cuerpo();

            if (prefabDeLaMuerte != null && t >= proximaMuerte)
            {
                proximaMuerte += entreMuertes;
                Vector3 donde = new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), 0f);
                MiniBicho.SoltarEfectoDeMuerte(donde, prefabDeLaMuerte, escalaDelEfectoDeMuerte);
            }

            if (t >= proximoPuf)
            {
                proximoPuf = t + 0.2f;
                Vector3 punto = new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), 0f);
                EfectoPuf.Crear(punto, Random.value < 0.5f ? Color.white : colorDeFuria, 6, 0.2f, 4f);
                UtilBoss.Sacudir(0.5f, 0.2f);
                flash = 0.08f;
            }
            yield return null;
        }

        // Estallido final.
        temblando = 0f;
        OcultarBarra();
        UtilBoss.Sacudir(1.6f, 0.7f);
        MiniBicho.SoltarEfectoDeMuerte(Cuerpo().center, prefabDeLaMuerte, escalaDelEfectoDeMuerte * 1.5f);
        EfectoPuf.Crear(Cuerpo().center, Color.white, 24, 0.3f, 8f);
        EfectoPuf.Crear(Cuerpo().center, colorDeFuria, 16, 0.25f, 6f);

        t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            alpha = 1f - t / 0.5f;
            yield return null;
        }

        if (ScoreManager.Instance != null) ScoreManager.Instance.AddPoints(puntosPorVencerlo);

        if (volverALaMusicaDeLaZonaAlGanar && AudioManager.Instance != null) AudioManager.Instance.VolverALaMusicaDeLaZona();
        else if (musicaDelBoss != null && AudioManager.Instance != null) AudioManager.Instance.ReproducirMusicaEspecial(null);

        if (volverALaCamaraNormalAlGanar && camaraDelRing != null) camaraDelRing.gameObject.SetActive(false);

        alGanar?.Invoke();

        gameObject.SetActive(false);
    }

    private void EliminarBichos()
    {
        foreach (MiniBicho b in new List<MiniBicho>(vivos))
        {
            if (b != null && !b.Muerto) b.Eliminar();
        }
        vivos.Clear();
    }

    //  8. CADA FRAME: DAÑO POR CONTACTO Y COMO SE VE

    private void Update()
    {
        if (cooldownContacto > 0f) cooldownContacto -= Time.deltaTime;

        bool peligroso = peleando && !muerto && !caido && !oculto && lastimaAlTocarlo;
        if (peligroso && cooldownContacto <= 0f && UtilBoss.TocaAlJugador(cuerpo))
        {
            cooldownContacto = UtilBoss.PegarAlJugador(danioPorContacto, transform.position, empujePorContacto) ? 1f : 0.2f;
        }

        if (flash > 0f) flash -= Time.deltaTime;
        aplastado = Mathf.MoveTowards(aplastado, 0f, Time.deltaTime * 1.5f);

        ActualizarBarra();
    }

    private void LateUpdate()
    {
        // Posicion: la logica + la flotacion + el temblor.
        Vector3 pos = posLogica;
        if (flotando) pos.y += Mathf.Sin(Time.time * velocidadDeFlotacion) * alturaDeFlotacion;
        if (temblando > 0f) pos += (Vector3)(Random.insideUnitCircle * temblorAlAvisar * temblando);
        transform.position = pos;

        // Escala: respira mientras flota y se aplasta al recibir golpes / caer.
        float resp = flotando ? 1f + Mathf.Sin(Time.time * velocidadDeFlotacion * 1.3f) * respiracion : 1f;
        transform.localScale = new Vector3(
            escalaOriginal.x * resp * (1f + aplastado),
            escalaOriginal.y * resp * (1f - aplastado),
            escalaOriginal.z);

        // Mareado en el piso: se bambolea.
        float giro = caido && !muerto ? Mathf.Sin(Time.time * 6f) * 6f : 0f;
        transform.rotation = Quaternion.Euler(0f, 0f, giro);

        // Color.
        Color c = colorOriginal;
        if (furia) c *= colorDeFuria;
        if (avisando) c *= colorDeAviso;
        if (flash > 0f) c = Color.Lerp(c, colorAlRecibirGolpe, 0.8f);
        c.a = colorOriginal.a * alpha;
        sr.color = c;

        AvisarleAlAnimator();
    }

    // Le cuenta al Animator en que estado esta el boss, con el parametro Int que elijas.
    // El orden importa: gana lo mas "urgente".
    private void AvisarleAlAnimator()
    {
        if (animador == null || string.IsNullOrEmpty(parametroDeEstado)) return;

        int estado;
        if (muerto) estado = animMuerte;
        else if (caido) estado = animCaido;
        else if (avisando) estado = animAvisando;
        else if (furia) estado = animFuria;
        else estado = animFlotando;

        animador.SetInteger(parametroDeEstado, estado);
    }

    //  9. AYUDANTES

    private IEnumerator Avisar(float segundos, AudioClip sonido)
    {
        avisando = true;
        temblando = 1f;
        UtilBoss.Sonar(sonido, volumen, transform.position);

        yield return new WaitForSeconds(segundos);

        avisando = false;
        temblando = 0f;
    }

    private IEnumerator MoverA(Vector3 destino, float segundos)
    {
        Vector3 inicio = posLogica;
        float t = 0f;
        float dur = Mathf.Max(0.01f, segundos);

        while (t < dur)
        {
            t += Time.deltaTime;
            posLogica = Vector3.Lerp(inicio, destino, Mathf.SmoothStep(0f, 1f, t / dur));
            yield return null;
        }
        posLogica = destino;
    }

    // El rectangulo del ring, armado con las dos esquinas.
    private Rect Ring()
    {
        if (esquinaSuperiorIzquierda == null || esquinaInferiorDerecha == null)
        {
            // Sin esquinas: un ring de 16x9 alrededor del boss, para poder probar igual.
            return new Rect(posicionDeReposo.x - 8f, posicionDeReposo.y - 6f, 16f, 9f);
        }

        Vector2 a = esquinaSuperiorIzquierda.position;
        Vector2 b = esquinaInferiorDerecha.position;
        return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }

    // El tamaño del boss sale de SU COLLIDER si lo tiene (el cuerpo de verdad) y, si no,
    // del rectangulo del dibujo. Con collider, al caerse apoya donde apoya el cuerpo y no
    // queda flotando por culpa del espacio transparente del sprite.
    private Bounds Cuerpo() => cuerpo != null ? cuerpo.bounds : sr.bounds;

    private float MitadDelAncho() => Cuerpo().extents.x;
    private float MitadDelAlto() => Cuerpo().extents.y;
    private float DistanciaAlBordeDeAbajo() => transform.position.y - Cuerpo().min.y;

    // A que altura queda el boss cuando esta APOYADO en el piso del ring, ya con la
    // correccion a mano. La usan la caida y la embestida.
    private float AlturaDeApoyo() => DistanciaAlBordeDeAbajo() + ajusteDeAlturaAlCaer;

    // Una X al azar dentro del ring que no quede pegada a las que ya se usaron.
    private float XLibre(Rect ring, List<float> usadas, float distanciaMinima)
    {
        float x = 0f;
        for (int intento = 0; intento < 10; intento++)
        {
            x = Random.Range(ring.xMin + 0.5f, ring.xMax - 0.5f);

            bool lejos = true;
            foreach (float u in usadas)
            {
                if (Mathf.Abs(u - x) < distanciaMinima) { lejos = false; break; }
            }
            if (lejos) return x;
        }
        return x;
    }

    private Vector3 PuntoDeAparicion(TipoDeBicho tipo, int indice, Rect ring)
    {
        if (grietas != null && grietas.Length > 0)
        {
            Transform g = grietas[indice % grietas.Length];
            if (g != null) return g.position;
        }

        float x = Random.Range(ring.xMin + 1f, ring.xMax - 1f);
        float y = tipo == TipoDeBicho.Volador ? ring.yMax - 1.5f : ring.yMin + 0.6f;
        return new Vector3(x, y, 0f);
    }

    private void ActivarCamaraDelRing()
    {
        if (camaraDelRing == null) return;

        camaraDelRing.gameObject.SetActive(true);
        camaraDelRing.Prioritize();

        // Sin esto la camara del ring no se sacude.
        if (camaraDelRing.GetComponent<CinemachineImpulseListener>() == null)
        {
            camaraDelRing.gameObject.AddComponent<CinemachineImpulseListener>();
        }
    }

    private string ClaveEmpezada() => "BossEmpezado@" + gameObject.scene.name + "@" + name;
    private string ClaveDerrotado() => "BossDerrotado@" + gameObject.scene.name + "@" + name;

    //  10. CREAR ESTALACTITAS, BOLAS, BICHOS Y ORBES

    private void CrearEstalactita(float x, float aviso)
    {
        GameObject prefab = UnaEstalactitaAlAzar();

        GameObject go;
        if (prefab != null)
        {
            go = Instantiate(prefab);
        }
        else
        {
            go = UtilBoss.CrearCuadrado("Estalactita (prueba)", COLOR_PELIGRO, new Vector2(0.45f, 1.1f), 20);
            go.AddComponent<Estalactita>();
            go.SetActive(true);
        }

        // Colgando del techo: la punta de arriba toca el borde superior del ring.
        Renderer rend = go.GetComponentInChildren<Renderer>();
        float mitad = rend != null ? rend.bounds.extents.y : 0.5f;
        go.transform.position = new Vector3(x, Ring().yMax - mitad, 0f);

        Estalactita e = go.GetComponent<Estalactita>();
        if (e == null) e = go.AddComponent<Estalactita>();
        e.Iniciar(aviso, Ring().yMin);

        AnotarPeligro(go);
    }

    // Elige uno de los prefabs de estalactita cargados (salteando los vacios).
    private GameObject UnaEstalactitaAlAzar()
    {
        if (prefabsDeEstalactita == null || prefabsDeEstalactita.Length == 0) return null;

        int cuantos = 0;
        foreach (GameObject p in prefabsDeEstalactita) if (p != null) cuantos++;
        if (cuantos == 0) return null;

        int elegido = Random.Range(0, cuantos);
        foreach (GameObject p in prefabsDeEstalactita)
        {
            if (p == null) continue;
            if (elegido == 0) return p;
            elegido--;
        }
        return null;
    }

    private void CrearBola(Vector2 origen, Vector2 direccion, float velocidad)
    {
        GameObject go;
        if (prefabBola != null)
        {
            go = Instantiate(prefabBola, origen, Quaternion.identity);
        }
        else
        {
            go = UtilBoss.CrearCuadrado("Bola (prueba)", COLOR_PELIGRO, new Vector2(0.45f, 0.45f), 20);
            go.transform.position = origen;
            go.AddComponent<ProyectilBoss>();
            go.SetActive(true);
        }

        ProyectilBoss p = go.GetComponent<ProyectilBoss>();
        if (p == null) p = go.AddComponent<ProyectilBoss>();
        p.Iniciar(direccion, velocidad);

        AnotarPeligro(go);
    }

    private void CrearBicho(TipoDeBicho tipo, Vector3 posicion)
    {
        GameObject prefab = tipo == TipoDeBicho.Rastrero ? prefabRastrero
                          : tipo == TipoDeBicho.Volador ? prefabVolador
                          : prefabSaltarin;
        GameObject go;

        if (prefab != null)
        {
            go = Instantiate(prefab, posicion, Quaternion.identity);
        }
        else
        {
            Color color = tipo == TipoDeBicho.Rastrero ? new Color(0.35f, 0.85f, 0.35f)
                        : tipo == TipoDeBicho.Volador ? new Color(0.35f, 0.8f, 1f)
                        : new Color(1f, 0.6f, 0.2f);
            Vector2 tamanio = tipo == TipoDeBicho.Rastrero ? new Vector2(0.9f, 0.6f)
                            : tipo == TipoDeBicho.Volador ? new Vector2(0.6f, 0.6f)
                            : new Vector2(0.7f, 0.8f);

            go = UtilBoss.CrearCuadrado(tipo + " (prueba)", color, tamanio, 20);
            go.transform.position = posicion;

            // Se configura APAGADO, asi el HealthHandler arranca con 1 de vida al prenderlo.
            MiniBicho nuevo = go.AddComponent<MiniBicho>();
            nuevo.tipo = tipo;
            nuevo.colorDelPuf = color;
            // La vida la pone el propio MiniBicho en su Awake (campo "Golpes Que Aguanta").
            Damageable d = go.GetComponent<Damageable>();
            d.activeKnockBack = false;
            d.activeInvulnerability = false;
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;

            go.SetActive(true);
        }

        MiniBicho m = go.GetComponent<MiniBicho>();
        if (m == null)
        {
            Debug.LogWarning("BossFinal: el prefab '" + prefab.name + "' no tiene el script MiniBicho. Lo saco.", prefab);
            Destroy(go);
            return;
        }

        // Que el ataque de la niña lo encuentre, aunque el prefab no tenga el layer puesto.
        int capa = UtilBoss.CapaDeEnemigos();
        if (capa >= 0) go.layer = capa;

        m.Iniciar(Ring().yMin - 4f);
        m.AlMorir += AlMorirUnBicho;
        vivos.Add(m);
    }

    // Algunos bichos (pocos) se transforman en un orbe de vida al morir.
    private void AlMorirUnBicho(MiniBicho bicho, bool loMatoLaNina)
    {
        vivos.Remove(bicho);

        if (!loMatoLaNina || prefabOrbeDeVida == null) return;
        if (orbesEnEstaOla >= maximoDeOrbesPorOla) return;
        if (Random.value > probabilidadDeOrbe) return;

        orbesEnEstaOla++;

        // Si el bicho murio en el aire (un volador), el orbe aparece mas abajo, a su alcance.
        Rect ring = Ring();
        Vector3 pos = bicho.transform.position;
        pos.y = Mathf.Clamp(pos.y, ring.yMin + 0.6f, ring.yMin + 2.5f);

        GameObject orbe = Instantiate(prefabOrbeDeVida, pos, Quaternion.identity);
        OrbeDeVida o = orbe.GetComponent<OrbeDeVida>();
        if (o != null) o.reapareceAlVolverALaZona = true;   // si no, un orbe en el mismo lugar no volveria a salir

        EfectoPuf.Crear(pos, new Color(0.4f, 1f, 0.5f), 10, 0.18f, 3f);
    }

    private void AnotarPeligro(GameObject go)
    {
        peligros.RemoveAll(p => p == null);
        peligros.Add(go);
    }

    //  11. BARRA DE VIDA (armada por codigo)

    private void MostrarBarra()
    {
        if (grupoBarra == null) CrearBarra();
        alphaBarraObjetivo = 1f;
    }

    // Un cartel grande en el medio de la pantalla (el que enseña a esconderse).
    private void MostrarCartelGrande(string texto, float segundos)
    {
        if (string.IsNullOrWhiteSpace(texto)) return;
        if (grupoBarra == null) CrearBarra();
        if (cartelGrande == null) return;

        if (cartelCo != null) StopCoroutine(cartelCo);
        cartelCo = StartCoroutine(RutinaDelCartelGrande(texto, segundos));
    }

    private IEnumerator RutinaDelCartelGrande(string texto, float segundos)
    {
        cartelGrande.text = texto;

        yield return FundirCartel(0f, 1f, 0.35f);
        yield return new WaitForSeconds(Mathf.Max(0.1f, segundos));
        yield return FundirCartel(1f, 0f, 0.6f);

        cartelGrande.text = "";
        cartelCo = null;
    }

    private IEnumerator FundirCartel(float desde, float hasta, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            cartelGrande.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        cartelGrande.alpha = hasta;
    }

    private void OcultarBarra()
    {
        alphaBarraObjetivo = 0f;
    }

    private void ActualizarBarra()
    {
        if (grupoBarra == null) return;

        grupoBarra.alpha = Mathf.MoveTowards(grupoBarra.alpha, alphaBarraObjetivo, Time.unscaledDeltaTime * 2f);
        rellenoMostrado = Mathf.MoveTowards(rellenoMostrado, rellenoObjetivo, Time.unscaledDeltaTime * 0.8f);
        relleno.anchorMax = new Vector2(rellenoMostrado, 1f);
    }

    private void CrearBarra()
    {
        GameObject canvasGo = new GameObject("BarraDelBoss");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Contenedor arriba al centro.
        GameObject contGo = new GameObject("Barra");
        contGo.transform.SetParent(canvasGo.transform, false);
        grupoBarra = contGo.AddComponent<CanvasGroup>();
        grupoBarra.alpha = 0f;
        grupoBarra.blocksRaycasts = false;

        RectTransform cont = contGo.AddComponent<RectTransform>();
        cont.anchorMin = cont.anchorMax = new Vector2(0.5f, 1f);
        cont.pivot = new Vector2(0.5f, 1f);
        cont.anchoredPosition = new Vector2(0f, -70f);
        cont.sizeDelta = new Vector2(700f, 22f);

        Image fondo = CrearImagen("Fondo", cont, new Color(0f, 0f, 0f, 0.65f));
        Estirar(fondo.rectTransform, 0f);

        // El relleno vive adentro de una zona con margen, y se achica cambiando su anchorMax.
        Image zona = CrearImagen("Zona", cont, new Color(0f, 0f, 0f, 0f));
        Estirar(zona.rectTransform, 3f);

        Image r = CrearImagen("Relleno", zona.rectTransform, colorDeLaBarra);
        relleno = r.rectTransform;
        relleno.anchorMin = Vector2.zero;
        relleno.anchorMax = Vector2.one;
        relleno.offsetMin = relleno.offsetMax = Vector2.zero;

        // Una rayita por cada caida, asi se ve cuanto falta.
        for (int i = 1; i < fases.Length; i++)
        {
            Image div = CrearImagen("Division", zona.rectTransform, new Color(0f, 0f, 0f, 0.85f));
            RectTransform rt = div.rectTransform;
            float x = i / (float)fases.Length;
            rt.anchorMin = new Vector2(x, 0f);
            rt.anchorMax = new Vector2(x, 1f);
            rt.sizeDelta = new Vector2(4f, 0f);
            rt.anchoredPosition = Vector2.zero;
        }

        // El cartel grande del medio de la pantalla (vive en el mismo Canvas, pero afuera
        // del contenedor de la barra: asi no se desvanece junto con ella).
        GameObject cartelGo = new GameObject("CartelDelBoss");
        cartelGo.transform.SetParent(canvasGo.transform, false);

        cartelGrande = cartelGo.AddComponent<TextMeshProUGUI>();
        if (fuenteDelNombre != null) cartelGrande.font = fuenteDelNombre;
        cartelGrande.fontSize = 52f;
        cartelGrande.alignment = TextAlignmentOptions.Center;
        cartelGrande.raycastTarget = false;
        cartelGrande.text = "";
        cartelGrande.alpha = 0f;

        RectTransform rtCartel = cartelGrande.rectTransform;
        rtCartel.anchorMin = new Vector2(0f, 0.5f);
        rtCartel.anchorMax = new Vector2(1f, 0.5f);
        rtCartel.pivot = new Vector2(0.5f, 0.5f);
        rtCartel.sizeDelta = new Vector2(-200f, 160f);
        rtCartel.anchoredPosition = new Vector2(0f, -180f);

        if (!string.IsNullOrEmpty(nombreEnLaBarra))
        {
            GameObject textoGo = new GameObject("Nombre");
            textoGo.transform.SetParent(cont, false);
            TextMeshProUGUI texto = textoGo.AddComponent<TextMeshProUGUI>();
            if (fuenteDelNombre != null) texto.font = fuenteDelNombre;
            texto.text = nombreEnLaBarra;
            texto.fontSize = 34f;
            texto.alignment = TextAlignmentOptions.Center;
            texto.raycastTarget = false;

            RectTransform rt = texto.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 6f);
            rt.sizeDelta = new Vector2(0f, 44f);
        }
    }

    private Image CrearImagen(string nombre, RectTransform padre, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private void Estirar(RectTransform rt, float margen)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
    }

    //  12. DIBUJOS EN LA ESCENA (solo en el editor)

    private void OnDrawGizmos()
    {
        if (esquinaSuperiorIzquierda == null || esquinaInferiorDerecha == null) return;

        Rect ring = Ring();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(ring.center, ring.size);

        Gizmos.color = new Color(1f, 0.4f, 0.4f);
        float y = ring.yMin + alturaDeLaEmbestida;
        Gizmos.DrawLine(new Vector3(ring.xMin, y, 0f), new Vector3(ring.xMax, y, 0f));

        if (grietas != null)
        {
            Gizmos.color = Color.green;
            foreach (Transform g in grietas) if (g != null) Gizmos.DrawWireSphere(g.position, 0.3f);
        }

        if (puntoDeReaparicion != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(puntoDeReaparicion.position, 0.4f);
        }
    }
}
