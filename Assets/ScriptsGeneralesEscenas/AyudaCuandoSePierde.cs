using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ============================================================
//  AYUDA CUANDO EL JUGADOR SE PIERDE
//
//  Muestra un cartelito discreto abajo de la pantalla cuando parece que el jugador
//  no sabe que hacer. Se dispara por DOS motivos:
//
//    a) QUIETO: pasa un rato sin tocar ninguna tecla y sin moverse.
//    b) PERDIDO: lleva mucho tiempo en la zona sin avanzar (sin encontrar la flor de
//       la habilidad, o sin salir de la zona despues de haberla conseguido).
//
//  El mensaje cambia solo segun en que esta:
//    - si todavia queda una flor sin agarrar  -> "...encontrar la habilidad"
//    - si ya la agarro                        -> "...encontrar la salida"
//
//  NO molesta: no aparece con el mapa abierto, ni en pausa, ni durante una cinematica,
//  y entre aviso y aviso espera un rato largo.
//
//  Va en UN objeto cualquiera de la zona (por ejemplo el mismo que tiene el CANVATODO,
//  o un objeto vacio llamado "AYUDAS"). El cartel se dibuja solo por codigo.
// ============================================================
public class AyudaCuandoSePierde : MonoBehaviour
{
    [Header("Cuando aparece por estar QUIETO")]
    [Tooltip("Segundos sin apretar ninguna tecla y sin moverse para que aparezca la ayuda. 0 = apagado.")]
    [Min(0f)] public float segundosQuieto = 15f;

    [Header("Cuando aparece por estar PERDIDO")]
    [Tooltip("Segundos dando vueltas por la zona sin encontrar la flor de la habilidad. 0 = apagado.")]
    [Min(0f)] public float segundosSinEncontrarLaHabilidad = 60f;
    [Tooltip("Segundos dando vueltas despues de tener la habilidad, sin encontrar la salida. 0 = apagado.")]
    [Min(0f)] public float segundosSinEncontrarLaSalida = 60f;

    [Header("Los mensajes")]
    public string mensajeHabilidad = "presiona TAB para abrir el mapa y encontrar la habilidad";
    public string mensajeSalida = "presiona TAB para abrir el mapa y encontrar la salida";
    [Tooltip("Se usa cuando el jugador esta quieto pero la zona no tiene flor (por ejemplo la del boss).")]
    public string mensajeGenerico = "presiona TAB para abrir el mapa";

    [Header("Como se ve")]
    [Min(1f)] public float tamanoDelTexto = 30f;
    public Color colorDelTexto = new Color(1f, 1f, 1f, 0.85f);
    [Tooltip("Tipografia del cartel. Vacio = la que trae TextMeshPro por defecto.")]
    public TMP_FontAsset tipografia;
    [Tooltip("Que tan arriba del borde de abajo aparece.")]
    public float alturaDesdeAbajo = 140f;
    [Tooltip("El mensaje entra LETRA POR LETRA, igual que el cartel de 'te faltan X puntos'. " +
             "Destildalo y el cartel aparece entero con un fade simple.")]
    public bool entrarLetraPorLetra = true;
    [Tooltip("Ajustes de esa entrada: que tan seguido arranca cada letra, cuanto tarda y de que " +
             "tamaño empieza.")]
    public EntradaDeTexto entrada = new EntradaDeTexto
    {
        activa = true,
        retrasoEntreLetras = 0.035f,
        duracionPorLetra = 0.3f,
        zoomInicial = 0.5f,
        desplazamientoInicial = 12f
    };
    [Tooltip("Solo se usa si la entrada letra por letra esta apagada.")]
    [Min(0.05f)] public float fadeIn = 0.8f;
    [Tooltip("Cuanto se queda el cartel en pantalla.")]
    [Min(0.5f)] public float duracionEnPantalla = 5f;
    [Min(0.05f)] public float fadeOut = 0.8f;

    [Header("Para no cansar")]
    [Tooltip("Segundos minimos entre un aviso y el siguiente.")]
    [Min(0f)] public float esperaEntreAvisos = 45f;
    [Tooltip("Cuantas veces como maximo se muestra en una misma zona. 0 = sin limite.")]
    [Min(0f)] public int maximoDeAvisosPorZona = 3;
    [Tooltip("Se esconde apenas el jugador abre el mapa (ya entendio el mensaje).")]
    public bool ocultarSiAbreElMapa = true;

    private PlayerController jugador;
    private MinimapUI mapa;
    private Rigidbody2D rbJugador;
    private ActivarFlor[] flores;

    private float tiempoQuieto;
    private float tiempoEnLaZona;
    private float tiempoDesdeElUltimoAviso = 9999f;
    private int avisosMostrados;

    private CanvasGroup grupo;
    private TextMeshProUGUI texto;
    private Coroutine rutina;

    private void Start()
    {
        jugador = FindFirstObjectByType<PlayerController>();
        mapa = FindFirstObjectByType<MinimapUI>();
        flores = FindObjectsByType<ActivarFlor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (jugador != null) rbJugador = jugador.GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        tiempoDesdeElUltimoAviso += Time.deltaTime;

        if (EstaQuieto()) tiempoQuieto += Time.deltaTime;
        else tiempoQuieto = 0f;

        // El reloj de "perdido" solo corre mientras DEAMBULA. Si esta peleando, en una
        // cinematica o con el mapa abierto, no esta perdido: esta jugando.
        if (EstaDeambulando()) tiempoEnLaZona += Time.deltaTime;

        // El jugador abrio el mapa: ya no hace falta insistir.
        if (ocultarSiAbreElMapa && mapa != null && mapa.IsOpen)
        {
            tiempoEnLaZona = 0f;
            tiempoQuieto = 0f;
            Esconder();
            return;
        }

        if (!SePuedeMostrar()) return;

        // a) Quieto hace rato.
        if (segundosQuieto > 0f && tiempoQuieto >= segundosQuieto)
        {
            Mostrar(MensajeQueCorresponde());
            return;
        }

        // b) Mucho tiempo en la zona sin avanzar.
        bool faltaLaFlor = QuedaFlorSinAgarrar();
        float limite = faltaLaFlor ? segundosSinEncontrarLaHabilidad : segundosSinEncontrarLaSalida;

        if (limite > 0f && tiempoEnLaZona >= limite)
        {
            Mostrar(MensajeQueCorresponde());
        }
    }

    // ---------- Decisiones ----------

    private bool EstaQuieto()
    {
        bool tecla = Keyboard.current != null && Keyboard.current.anyKey.isPressed;
        bool clic = Mouse.current != null &&
                    (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed);
        bool moviendose = rbJugador != null && rbJugador.linearVelocity.sqrMagnitude > 0.05f;

        return !tecla && !clic && !moviendose;
    }

    // El jugador esta DEAMBULANDO por la zona: ni peleando, ni en una cinematica, ni con
    // el mapa abierto, ni en pausa. Solo en ese caso tiene sentido decir que esta perdido.
    private bool EstaDeambulando()
    {
        if (CinematicaFrames.EnCurso) return false;

        // Durante la pelea del boss no esta perdido: esta ocupado.
        if (BossFinal.PeleaEnCurso) return false;
        if (Time.timeScale <= 0.01f) return false;
        if (mapa != null && mapa.IsOpen) return false;

        // Si la niña esta muerta o no hay niña, tampoco.
        if (jugador == null) return false;

        return true;
    }

    private bool SePuedeMostrar()
    {
        if (tiempoDesdeElUltimoAviso < esperaEntreAvisos) return false;
        if (maximoDeAvisosPorZona > 0 && avisosMostrados >= maximoDeAvisosPorZona) return false;

        return EstaDeambulando();
    }

    // Las flores de la zona se buscan UNA sola vez (en Start): recorrer la escena entera
    // todos los frames seria carisimo, y las flores no aparecen ni desaparecen a mitad de
    // la partida (las ya recogidas se apagan, pero el componente sigue estando).
    private bool QuedaFlorSinAgarrar()
    {
        if (flores == null) return false;

        foreach (ActivarFlor flor in flores)
        {
            if (flor != null && !flor.Recogida) return true;
        }

        return false;
    }

    private string MensajeQueCorresponde()
    {
        if (flores == null || flores.Length == 0) return mensajeGenerico; // zona sin flor (el boss, por ejemplo)
        return QuedaFlorSinAgarrar() ? mensajeHabilidad : mensajeSalida;
    }

    // ---------- El cartel ----------

    public void Mostrar(string mensaje)
    {
        if (string.IsNullOrEmpty(mensaje)) return;

        CrearCartelSiHaceFalta();

        texto.text = mensaje;
        avisosMostrados++;
        tiempoDesdeElUltimoAviso = 0f;
        tiempoQuieto = 0f;
        tiempoEnLaZona = 0f;

        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(RutinaDelCartel());
    }

    public void Esconder()
    {
        if (grupo == null) return;

        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Fundir(grupo.alpha, 0f, fadeOut));
    }

    private IEnumerator RutinaDelCartel()
    {
        if (entrarLetraPorLetra && entrada != null && entrada.activa)
        {
            // El grupo se prende entero: la que hace el fade es cada letra por su cuenta.
            grupo.alpha = 1f;
            yield return AnimacionDeTexto.EntrarPorLetra(texto, entrada);
        }
        else
        {
            yield return Fundir(0f, 1f, fadeIn);
        }

        yield return new WaitForSeconds(duracionEnPantalla);
        yield return Fundir(1f, 0f, fadeOut);
        rutina = null;
    }

    private IEnumerator Fundir(float desde, float hasta, float duracion)
    {
        float t = 0f;
        grupo.alpha = desde;

        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(desde, hasta, Mathf.Clamp01(t / Mathf.Max(0.01f, duracion)));
            yield return null;
        }

        grupo.alpha = hasta;
    }

    private void CrearCartelSiHaceFalta()
    {
        if (grupo != null) return;

        GameObject canvasGo = new GameObject("AYUDA AL JUGADOR", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // encima del HUD, debajo de las cinematicas

        CanvasScaler escala = canvasGo.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);

        GameObject textoGo = new GameObject("TEXTO", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(CanvasGroup));
        RectTransform rect = textoGo.GetComponent<RectTransform>();
        rect.SetParent(canvasGo.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, alturaDesdeAbajo);
        rect.sizeDelta = new Vector2(1400f, 90f);

        texto = textoGo.GetComponent<TextMeshProUGUI>();
        if (tipografia != null) texto.font = tipografia;
        texto.fontSize = tamanoDelTexto;
        texto.color = colorDelTexto;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;

        grupo = textoGo.GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
    }
}
