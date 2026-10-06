using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[ExecuteAlways]
public class TutorialInicialPaginas : MonoBehaviour
{
    // El menu la activa justo antes de cargar una partida nueva. Al consumirla vuelve
    // a false, por lo que recargar Zona 1 al morir no repite la presentacion ni el tutorial.
    private static bool mostrarEnProximaCarga;

    public static void MostrarEnLaProximaCarga()
    {
        mostrarEnProximaCarga = true;
    }

    [Header("Presentacion antes del tutorial")]
    [Tooltip("Imagen completa del logo de Not Home. Aparece SOLO y centrado.")]
    public Texture logoDelJuego;
    [Tooltip("La frase que se escribe sola, con animacion de tipeo, despues de que el titulo se va.")]
    public string fraseInicial = "Intenta escapar...";

    [Header("Titulo: tiempos (todo en segundos)")]
    [Tooltip("Pantalla en negro antes de que el titulo empiece a aparecer.")]
    [Min(0f)] public float esperaAntesDelTitulo = 1.2f;
    [Tooltip("Cuanto tarda el titulo en aparecer. Cuanto mas alto, mas lento y pausado.")]
    [Min(0.05f)] public float duracionFadeIn = 4f;
    [Tooltip("Cuanto se queda el titulo quieto en pantalla.")]
    [Min(0f)] public float tiempoVisible = 2.5f;
    [Tooltip("Cuanto tarda el titulo en desvanecerse.")]
    [Min(0.05f)] public float duracionFadeOut = 2.5f;
    [Tooltip("Pantalla en negro entre que el titulo se fue y empieza a escribirse la frase.")]
    [Min(0f)] public float esperaEntreTituloYFrase = 1f;

    [Header("Frase: tipeo")]
    [Tooltip("Letras por segundo. Pocas letras por segundo = mas lento y dramatico.")]
    [Min(1f)] public float letrasPorSegundo = 9f;
    [Tooltip("Cuanto se queda la frase completa en pantalla antes de irse.")]
    [Min(0f)] public float esperaDespuesDeLaFrase = 2f;
    [Tooltip("Cuanto tarda la frase en desvanecerse.")]
    [Min(0.05f)] public float fadeOutFrase = 1.8f;
    [Tooltip("Sonidos de cada letra (opcional). Si pones varios, en cada letra se elige uno al azar.")]
    public AudioClip[] sonidosTipeo;
    [Range(0f, 1f)] public float volumenTipeo = 0.5f;

    [Header("Musica de la intro")]
    [Tooltip("Musica propia de esta presentacion. Mientras suena, la musica de la zona queda en silencio. " +
             "Si lo dejas vacio, sigue sonando la musica normal del juego.")]
    public AudioClip musicaDeLaIntro;
    [Range(0f, 1f)] public float volumenMusicaIntro = 0.8f;
    [Tooltip("Cuanto tarda la musica de la intro en entrar de a poco.")]
    [Min(0f)] public float fadeInMusicaIntro = 3f;

    [Header("Salida: cuando se aprieta ESPACIO")]
    [Tooltip("Cuanto tarda la pantalla en fundirse a negro al empezar a jugar.")]
    [Min(0f)] public float fundidoANegro = 2f;
    [Tooltip("Cuanto tarda la musica de la intro en apagarse. Va en paralelo con el fundido a negro.")]
    [Min(0f)] public float fadeOutMusicaIntro = 2f;
    [Tooltip("Silencio en negro antes de que aparezca el juego.")]
    [Min(0f)] public float esperaEnNegro = 0.6f;
    [Tooltip("Cuanto tarda el juego en aparecer desde el negro.")]
    [Min(0f)] public float volverDelNegro = 2f;
    [Tooltip("Cuanto tarda la musica del juego en entrar de a poco.")]
    [Min(0f)] public float fadeInMusicaDelJuego = 3f;

    [Header("Entrada de las paginas del tutorial")]
    [Tooltip("Los elementos de cada pagina (titulos, teclas, iconos) aparecen con fade, uno despues " +
             "del otro, de arriba hacia abajo. Destildalo y la pagina aparece entera de una.")]
    public bool entradaEscalonada = true;
    [Tooltip("Cuanto tarda en aparecer CADA elemento.")]
    [Min(0.05f)] public float fadeDeCadaElemento = 0.55f;
    [Tooltip("Cuanto espera entre que arranca un elemento y el siguiente. Mas alto = mas pausado.")]
    [Min(0f)] public float retrasoEntreElementos = 0.12f;
    [Tooltip("Activo: la entrada escalonada se ve solo la PRIMERA vez que se abre cada pagina. " +
             "Si el jugador va y vuelve con las flechas, la pagina aparece entera (no lo hacemos esperar de nuevo).")]
    public bool soloLaPrimeraVez = true;

    public Sprite iconoIzquierda;
    public Sprite iconoDerecha;

    private GameObject paginaUno;
    private GameObject paginaDos;
    private float escalaAnterior = 1f;
    private bool abierto;
    private bool saliendo;
    private Coroutine entradaCo;
    private GameObject paginaEnEntrada;
    private readonly HashSet<GameObject> paginasYaMostradas = new HashSet<GameObject>();
    private AudioSource musicaIntroSource;
    private float volumenMusicaOriginal = 1f;
    private bool musicaDelJuegoSilenciada;
    private static bool armandoVisuales;

    private void OnEnable()
    {
        BuscarPaginas();
        if (!Application.isPlaying) ArmarVisuales();
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        if (!mostrarEnProximaCarga)
        {
            gameObject.SetActive(false);
            return;
        }

        mostrarEnProximaCarga = false;
        BuscarPaginas();
        ConfigurarBotones();
        MostrarPagina(0);
        escalaAnterior = Time.timeScale;
        Time.timeScale = 0f;
        abierto = true;
        StartCoroutine(MostrarPresentacionInicial());
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            ArmarVisuales();
            return;
        }

        if (!abierto || Keyboard.current == null) return;

        if (paginaUno != null && paginaUno.activeSelf && Keyboard.current.rightArrowKey.wasPressedThisFrame)
            MostrarPagina(2);
        else if (paginaDos != null && paginaDos.activeSelf && Keyboard.current.leftArrowKey.wasPressedThisFrame)
            MostrarPagina(1);
        else if (paginaDos != null && paginaDos.activeSelf && Keyboard.current.spaceKey.wasPressedThisFrame)
            ComenzarAJugar();
    }

    public void MostrarPagina(int numero)
    {
        // Si veniamos animando otra pagina, la dejamos visible y cortamos.
        if (entradaCo != null)
        {
            StopCoroutine(entradaCo);
            entradaCo = null;
            PonerElementosVisibles(paginaEnEntrada);
            paginaEnEntrada = null;
        }

        if (paginaUno != null) paginaUno.SetActive(numero == 1);
        if (paginaDos != null) paginaDos.SetActive(numero == 2);

        GameObject paginaAbierta = numero == 1 ? paginaUno : (numero == 2 ? paginaDos : null);
        if (paginaAbierta == null || !Application.isPlaying) return;

        if (!entradaEscalonada)
        {
            PonerElementosVisibles(paginaAbierta);
            return;
        }

        // Solo la primera vez: si el jugador va y vuelve con las flechas, aparece entera.
        if (soloLaPrimeraVez && paginasYaMostradas.Contains(paginaAbierta))
        {
            PonerElementosVisibles(paginaAbierta);
            return;
        }

        paginasYaMostradas.Add(paginaAbierta);
        paginaEnEntrada = paginaAbierta;
        entradaCo = StartCoroutine(EntradaDeLaPagina(paginaAbierta));
    }

    // Hace aparecer los elementos de la pagina uno despues del otro, ordenados de ARRIBA
    // hacia abajo (y a igual altura, de izquierda a derecha), asi se lee como que la pagina
    // se va dibujando sola.
    private IEnumerator EntradaDeLaPagina(GameObject pagina)
    {
        List<RectTransform> elementos = new List<RectTransform>();
        foreach (Transform hijo in pagina.transform)
        {
            RectTransform rect = hijo as RectTransform;
            if (rect != null) elementos.Add(rect);
        }

        elementos.Sort((a, b) =>
        {
            float ay = a.anchoredPosition.y;
            float by = b.anchoredPosition.y;
            if (!Mathf.Approximately(ay, by)) return by.CompareTo(ay);   // mas arriba primero
            return a.anchoredPosition.x.CompareTo(b.anchoredPosition.x); // despues, de izq a der
        });

        // Todos arrancan invisibles.
        List<CanvasGroup> grupos = new List<CanvasGroup>();
        foreach (RectTransform rect in elementos)
        {
            CanvasGroup grupo = rect.GetComponent<CanvasGroup>();
            if (grupo == null) grupo = rect.gameObject.AddComponent<CanvasGroup>();
            grupo.alpha = 0f;
            grupos.Add(grupo);
        }

        // Cada uno arranca su propio fade un ratito despues que el anterior, asi se superponen.
        foreach (CanvasGroup grupo in grupos)
        {
            StartCoroutine(CambiarAlpha(grupo, 0f, 1f, fadeDeCadaElemento));
            if (retrasoEntreElementos > 0f) yield return new WaitForSecondsRealtime(retrasoEntreElementos);
        }

        yield return new WaitForSecondsRealtime(fadeDeCadaElemento);

        PonerElementosVisibles(pagina);
        entradaCo = null;
        paginaEnEntrada = null;
    }

    private void PonerElementosVisibles(GameObject pagina)
    {
        if (pagina == null) return;

        foreach (CanvasGroup grupo in pagina.GetComponentsInChildren<CanvasGroup>(true))
        {
            grupo.alpha = 1f;
        }
    }

    private IEnumerator MostrarPresentacionInicial()
    {
        GameObject presentacion = new GameObject("PRESENTACION NOT HOME", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        presentacion.layer = gameObject.layer;

        RectTransform fondoRect = presentacion.GetComponent<RectTransform>();
        fondoRect.SetParent(transform, false);
        fondoRect.anchorMin = Vector2.zero;
        fondoRect.anchorMax = Vector2.one;
        fondoRect.offsetMin = Vector2.zero;
        fondoRect.offsetMax = Vector2.zero;
        presentacion.GetComponent<Image>().color = Color.black;
        presentacion.transform.SetAsLastSibling();

        // El titulo y la frase van en grupos SEPARADOS: primero se ve uno, se va, y recien
        // despues aparece el otro.
        GameObject contenido = new GameObject("TITULO", typeof(RectTransform), typeof(CanvasGroup));
        contenido.layer = gameObject.layer;
        RectTransform contenidoRect = contenido.GetComponent<RectTransform>();
        contenidoRect.SetParent(presentacion.transform, false);
        contenidoRect.anchorMin = Vector2.zero;
        contenidoRect.anchorMax = Vector2.one;
        contenidoRect.offsetMin = Vector2.zero;
        contenidoRect.offsetMax = Vector2.zero;
        CanvasGroup grupoContenido = contenido.GetComponent<CanvasGroup>();
        grupoContenido.alpha = 0f;

        if (logoDelJuego != null)
        {
            GameObject logo = new GameObject("LOGO", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter));
            logo.layer = gameObject.layer;
            RectTransform logoRect = logo.GetComponent<RectTransform>();
            logoRect.SetParent(contenido.transform, false);
            // Centrado y solo: sin la frase abajo, no hace falta correrlo para arriba.
            logoRect.anchorMin = logoRect.anchorMax = new Vector2(0.5f, 0.5f);
            logoRect.anchoredPosition = Vector2.zero;
            logoRect.sizeDelta = new Vector2(880f, 530f);

            RawImage imagen = logo.GetComponent<RawImage>();
            imagen.texture = logoDelJuego;
            imagen.color = Color.white;
            imagen.raycastTarget = false;

            AspectRatioFitter proporcion = logo.GetComponent<AspectRatioFitter>();
            proporcion.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            proporcion.aspectRatio = logoDelJuego.width / (float)logoDelJuego.height;
        }

        // La frase cuelga de su propio grupo, centrada en la pantalla.
        GameObject grupoFrase = new GameObject("GRUPO FRASE", typeof(RectTransform), typeof(CanvasGroup));
        grupoFrase.layer = gameObject.layer;
        RectTransform grupoFraseRect = grupoFrase.GetComponent<RectTransform>();
        grupoFraseRect.SetParent(presentacion.transform, false);
        grupoFraseRect.anchorMin = Vector2.zero;
        grupoFraseRect.anchorMax = Vector2.one;
        grupoFraseRect.offsetMin = Vector2.zero;
        grupoFraseRect.offsetMax = Vector2.zero;
        CanvasGroup grupoDeLaFrase = grupoFrase.GetComponent<CanvasGroup>();
        grupoDeLaFrase.alpha = 0f;

        GameObject frase = new GameObject("FRASE", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        frase.layer = gameObject.layer;
        RectTransform fraseRect = frase.GetComponent<RectTransform>();
        fraseRect.SetParent(grupoFrase.transform, false);
        fraseRect.anchorMin = fraseRect.anchorMax = new Vector2(0.5f, 0.5f);
        fraseRect.anchoredPosition = Vector2.zero;
        fraseRect.sizeDelta = new Vector2(1200f, 110f);

        TextMeshProUGUI texto = frase.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI referencia = paginaUno != null ? paginaUno.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        if (referencia != null) texto.font = referencia.font;
        texto.text = fraseInicial;
        texto.fontSize = 46f;
        texto.fontStyle = FontStyles.Italic;
        texto.color = Color.white;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;

        // La musica de la intro arranca junto con la pantalla negra y entra de a poco.
        ArrancarMusicaDeLaIntro();

        // 1) Negro un rato, para que no arranque de golpe.
        if (esperaAntesDelTitulo > 0f) yield return new WaitForSecondsRealtime(esperaAntesDelTitulo);

        // 2) El TITULO aparece solo, bien lento, se queda y se va.
        yield return CambiarAlpha(grupoContenido, 0f, 1f, duracionFadeIn);
        if (tiempoVisible > 0f) yield return new WaitForSecondsRealtime(tiempoVisible);
        yield return CambiarAlpha(grupoContenido, 1f, 0f, duracionFadeOut);
        Destroy(contenido);

        // 3) Un respiro en negro.
        if (esperaEntreTituloYFrase > 0f) yield return new WaitForSecondsRealtime(esperaEntreTituloYFrase);

        // 4) La FRASE se escribe letra por letra.
        texto.maxVisibleCharacters = 0;
        texto.ForceMeshUpdate();
        grupoDeLaFrase.alpha = 1f;
        yield return TipearFrase(texto);

        if (esperaDespuesDeLaFrase > 0f) yield return new WaitForSecondsRealtime(esperaDespuesDeLaFrase);
        yield return CambiarAlpha(grupoDeLaFrase, 1f, 0f, fadeOutFrase);

        CanvasGroup grupoFondo = presentacion.AddComponent<CanvasGroup>();
        grupoFondo.alpha = 1f;
        yield return CambiarAlpha(grupoFondo, 1f, 0f, 0.45f);

        Destroy(presentacion);
        MostrarPagina(1);
    }

    // Va mostrando la frase letra por letra. Tiempo REAL: durante la intro el juego
    // esta congelado (timeScale 0).
    private IEnumerator TipearFrase(TextMeshProUGUI texto)
    {
        int total = texto.textInfo.characterCount;
        float esperaPorLetra = 1f / Mathf.Max(1f, letrasPorSegundo);

        for (int i = 1; i <= total; i++)
        {
            texto.maxVisibleCharacters = i;
            SonarTipeo();
            yield return new WaitForSecondsRealtime(esperaPorLetra);
        }

        texto.maxVisibleCharacters = int.MaxValue;
    }

    private AudioClip ultimoClipDeTipeo;

    private void SonarTipeo()
    {
        if (sonidosTipeo == null || sonidosTipeo.Length == 0) return;

        AudioClip elegido = null;
        for (int intento = 0; intento < 6; intento++)
        {
            AudioClip candidato = sonidosTipeo[Random.Range(0, sonidosTipeo.Length)];
            if (candidato == null) continue;

            elegido = candidato;
            if (candidato != ultimoClipDeTipeo || sonidosTipeo.Length == 1) break;
        }

        if (elegido == null) return;

        ultimoClipDeTipeo = elegido;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(elegido, volumenTipeo);
    }

    private IEnumerator CambiarAlpha(CanvasGroup grupo, float desde, float hasta, float duracion)
    {
        float tiempo = 0f;
        grupo.alpha = desde;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(desde, hasta, Mathf.Clamp01(tiempo / duracion));
            yield return null;
        }

        grupo.alpha = hasta;
    }

    public void ComenzarAJugar()
    {
        if (!Application.isPlaying || saliendo) return;

        saliendo = true;
        abierto = false;
        StartCoroutine(SalidaSuave());
    }

    // ESPACIO: la musica de la intro se apaga mientras la pantalla se funde a negro, y el
    // juego aparece desde el negro con su propia musica entrando de a poco.
    private IEnumerator SalidaSuave()
    {
        // Un panel negro propio, encima de todo, que es el que hace los dos fundidos.
        GameObject negro = new GameObject("FUNDIDO", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        negro.layer = gameObject.layer;
        RectTransform rect = negro.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        negro.GetComponent<Image>().color = Color.black;
        negro.transform.SetAsLastSibling();

        CanvasGroup grupoNegro = negro.GetComponent<CanvasGroup>();
        grupoNegro.alpha = 0f;

        // 1) A negro y, al mismo tiempo, la musica de la intro se va.
        if (musicaIntroSource != null)
        {
            StartCoroutine(CambiarVolumen(musicaIntroSource, musicaIntroSource.volume, 0f, fadeOutMusicaIntro));
        }
        yield return CambiarAlpha(grupoNegro, 0f, 1f, fundidoANegro);

        if (musicaIntroSource != null)
        {
            musicaIntroSource.Stop();
            Destroy(musicaIntroSource.gameObject);
            musicaIntroSource = null;
        }

        // Ya no se ve nada: sacamos el tutorial de la pantalla.
        MostrarPagina(0);

        if (esperaEnNegro > 0f) yield return new WaitForSecondsRealtime(esperaEnNegro);

        // 2) Arranca el juego (se descongela) y vuelve su musica de a poco.
        Time.timeScale = escalaAnterior;

        AudioSource musicaDelJuego = AudioManager.Instance != null ? AudioManager.Instance.musicSource : null;
        if (musicaDelJuego != null && musicaDelJuegoSilenciada)
        {
            StartCoroutine(CambiarVolumen(musicaDelJuego, 0f, volumenMusicaOriginal, fadeInMusicaDelJuego));
            musicaDelJuegoSilenciada = false;
        }

        // 3) El juego aparece desde el negro.
        yield return CambiarAlpha(grupoNegro, 1f, 0f, volverDelNegro);

        Destroy(negro);
        gameObject.SetActive(false);
    }

    // Crea una AudioSource propia para la musica de la intro y la hace entrar de a poco.
    // Mientras tanto, la musica normal del juego queda en silencio (no se corta: se baja,
    // asi al volver sigue sonando donde estaba).
    private void ArrancarMusicaDeLaIntro()
    {
        AudioSource musicaDelJuego = AudioManager.Instance != null ? AudioManager.Instance.musicSource : null;

        if (musicaDeLaIntro == null) return; // sin musica propia, la del juego sigue como estaba

        if (musicaDelJuego != null)
        {
            volumenMusicaOriginal = musicaDelJuego.volume;
            musicaDelJuego.volume = 0f;
            musicaDelJuegoSilenciada = true;
        }

        GameObject go = new GameObject("MUSICA INTRO");
        go.transform.SetParent(transform, false);

        musicaIntroSource = go.AddComponent<AudioSource>();
        musicaIntroSource.clip = musicaDeLaIntro;
        musicaIntroSource.loop = true;
        musicaIntroSource.playOnAwake = false;
        musicaIntroSource.spatialBlend = 0f;
        musicaIntroSource.volume = 0f;
        // Mismo canal del mixer que la musica del juego: la regula el slider de Musica.
        if (musicaDelJuego != null) musicaIntroSource.outputAudioMixerGroup = musicaDelJuego.outputAudioMixerGroup;

        musicaIntroSource.Play();
        StartCoroutine(CambiarVolumen(musicaIntroSource, 0f, volumenMusicaIntro, fadeInMusicaIntro));
    }

    private IEnumerator CambiarVolumen(AudioSource fuente, float desde, float hasta, float duracion)
    {
        if (fuente == null) yield break;

        float tiempo = 0f;
        fuente.volume = desde;

        while (tiempo < duracion && fuente != null)
        {
            tiempo += Time.unscaledDeltaTime;
            fuente.volume = Mathf.Lerp(desde, hasta, Mathf.Clamp01(tiempo / duracion));
            yield return null;
        }

        if (fuente != null) fuente.volume = hasta;
    }

    private void ConfigurarBotones()
    {
        if (paginaUno != null)
        {
            Button siguiente = paginaUno.transform.Find("SIGUIENTE")?.GetComponent<Button>();
            if (siguiente != null)
            {
                siguiente.onClick.RemoveAllListeners();
                siguiente.onClick.AddListener(() => MostrarPagina(2));
            }
        }

        if (paginaDos != null)
        {
            Button anterior = paginaDos.transform.Find("ANTERIOR")?.GetComponent<Button>();
            if (anterior != null)
            {
                anterior.onClick.RemoveAllListeners();
                anterior.onClick.AddListener(() => MostrarPagina(1));
            }
        }
    }

    private void BuscarPaginas()
    {
        Transform uno = transform.Find("PAGINA 1 - MOVIMIENTO Y SALTO");
        Transform dos = transform.Find("PAGINA 2 - MAPA Y ELEMENTOS");
        paginaUno = uno != null ? uno.gameObject : null;
        paginaDos = dos != null ? dos.gameObject : null;
    }

    private void ArmarVisuales()
    {
        if (armandoVisuales || paginaUno == null || paginaDos == null) return;
        armandoVisuales = true;

        CrearBotonSiFalta(paginaUno.transform, "SIGUIENTE", iconoDerecha, new Vector2(790f, -400f), () => MostrarPagina(2));
        CrearBotonSiFalta(paginaDos.transform, "ANTERIOR", iconoIzquierda, new Vector2(-790f, -400f), () => MostrarPagina(1));
        CrearTextoFinalSiFalta(paginaDos.transform);

        armandoVisuales = false;
    }

    private void CrearBotonSiFalta(Transform pagina, string nombre, Sprite sprite, Vector2 posicion, UnityEngine.Events.UnityAction accion)
    {
        if (pagina.Find(nombre) != null) return;

        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        objeto.layer = gameObject.layer;
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.SetParent(pagina, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = new Vector2(150f, 150f);

        Image imagen = objeto.GetComponent<Image>();
        imagen.sprite = sprite;
        imagen.preserveAspect = true;
        Button boton = objeto.GetComponent<Button>();
        boton.targetGraphic = imagen;
        boton.onClick.AddListener(accion);
    }

    private void CrearTextoFinalSiFalta(Transform pagina)
    {
        if (pagina.Find("TEXTO COMENZAR") != null) return;

        GameObject objeto = new GameObject("TEXTO COMENZAR", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        objeto.layer = gameObject.layer;
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.SetParent(pagina, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -430f);
        rect.sizeDelta = new Vector2(1000f, 80f);

        TextMeshProUGUI texto = objeto.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI referencia = paginaUno != null ? paginaUno.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        if (referencia != null) texto.font = referencia.font;
        texto.text = "PRESIONA ESPACIO PARA COMENZAR A JUGAR";
        texto.fontSize = 32f;
        texto.color = Color.white;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;
    }

    private void OnDisable()
    {
        if (Application.isPlaying && abierto)
        {
            Time.timeScale = escalaAnterior;
            abierto = false;
        }
    }
}
