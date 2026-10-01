using System.Collections;
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
    [Tooltip("Imagen completa del logo de Not Home.")]
    public Texture logoDelJuego;
    public string fraseInicial = "Intenta escapar...";
    [Min(0.05f)] public float duracionFadeIn = 0.9f;
    [Min(0f)] public float tiempoVisible = 1.6f;
    [Min(0.05f)] public float duracionFadeOut = 0.8f;

    public Sprite iconoIzquierda;
    public Sprite iconoDerecha;

    private GameObject paginaUno;
    private GameObject paginaDos;
    private float escalaAnterior = 1f;
    private bool abierto;
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
        if (paginaUno != null) paginaUno.SetActive(numero == 1);
        if (paginaDos != null) paginaDos.SetActive(numero == 2);
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

        GameObject contenido = new GameObject("Contenido", typeof(RectTransform), typeof(CanvasGroup));
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
            logoRect.anchorMin = logoRect.anchorMax = new Vector2(0.5f, 0.5f);
            logoRect.anchoredPosition = new Vector2(0f, 70f);
            logoRect.sizeDelta = new Vector2(880f, 530f);

            RawImage imagen = logo.GetComponent<RawImage>();
            imagen.texture = logoDelJuego;
            imagen.color = Color.white;
            imagen.raycastTarget = false;

            AspectRatioFitter proporcion = logo.GetComponent<AspectRatioFitter>();
            proporcion.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            proporcion.aspectRatio = logoDelJuego.width / (float)logoDelJuego.height;
        }

        GameObject frase = new GameObject("FRASE", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        frase.layer = gameObject.layer;
        RectTransform fraseRect = frase.GetComponent<RectTransform>();
        fraseRect.SetParent(contenido.transform, false);
        fraseRect.anchorMin = fraseRect.anchorMax = new Vector2(0.5f, 0.5f);
        fraseRect.anchoredPosition = new Vector2(0f, -300f);
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

        yield return CambiarAlpha(grupoContenido, 0f, 1f, duracionFadeIn);
        if (tiempoVisible > 0f) yield return new WaitForSecondsRealtime(tiempoVisible);
        yield return CambiarAlpha(grupoContenido, 1f, 0f, duracionFadeOut);

        CanvasGroup grupoFondo = presentacion.AddComponent<CanvasGroup>();
        grupoFondo.alpha = 1f;
        yield return CambiarAlpha(grupoFondo, 1f, 0f, 0.45f);

        Destroy(presentacion);
        MostrarPagina(1);
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
        if (!Application.isPlaying) return;
        abierto = false;
        Time.timeScale = escalaAnterior;
        gameObject.SetActive(false);
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
