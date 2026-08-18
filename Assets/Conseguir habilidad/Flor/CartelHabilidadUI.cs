using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[System.Serializable]
public class EstiloCartelHabilidad
{
    [Header("Panel")]
    public Vector2 posicionPanel = new Vector2(0f, 220f);
    public Vector2 tamanoPanel = new Vector2(900f, 300f);
    public Color colorFondo = new Color(0.035f, 0.04f, 0.065f, 0.97f);
    public Color colorBorde = new Color(0f, 0f, 0f, 0.9f);
    public Vector2 grosorBorde = new Vector2(4f, -4f);

    [Header("Imagen")]
    public Vector2 posicionImagen = new Vector2(0f, 90f);
    public Vector2 tamanoImagen = new Vector2(220f, 220f);

    [Header("Titulo")]
    public TMP_FontAsset tipografiaTitulo;
    public float tamanoTitulo = 46f;
    public Color colorTitulo = Color.white;
    public Vector2 posicionTitulo = new Vector2(0f, 75.2f);
    public Vector2 cajaTitulo = new Vector2(760f, 1074.9f);
    public FontStyles estiloTitulo = FontStyles.Bold;

    [Header("Descripcion")]
    public TMP_FontAsset tipografiaDescripcion;
    public float tamanoDescripcion = 20f;
    public Color colorDescripcion = Color.white;
    public Vector2 posicionDescripcion = new Vector2(0f, 10.7f);
    public Vector2 cajaDescripcion = new Vector2(760f, 45.5f);
    public FontStyles estiloDescripcion = FontStyles.Normal;

    [Header("Texto para cerrar")]
    public TMP_FontAsset tipografiaAyuda;
    public float tamanoAyuda = 21f;
    public Color colorAyuda = Color.white;
    public Vector2 posicionAyuda = new Vector2(0f, -100f);
    public Vector2 cajaAyuda = new Vector2(760f, 45f);
    public FontStyles estiloAyuda = FontStyles.Italic;
}

public class CartelHabilidadUI : MonoBehaviour
{
    private static CartelHabilidadUI instance;

    private GameObject panel;
    private Image icono;
    private TMP_Text titulo;
    private TMP_Text descripcion;
    private TMP_Text ayudaCerrar;
    private Image fondo;
    private Outline borde;
    private bool abierto;
    private bool pausarJuego;
    private float escalaAnterior = 1f;
    private float abiertoDesde;
    private EstiloCartelHabilidad estiloActivo;

    public static void Mostrar(Sprite imagen, string textoTitulo, string textoDescripcion, string textoAyuda, bool pausa, EstiloCartelHabilidad estilo)
    {
        if (instance == null) CrearInterfaz();
        instance.MostrarInterno(imagen, textoTitulo, textoDescripcion, textoAyuda, pausa, estilo);
    }

    private static void CrearInterfaz()
    {
        GameObject root = new GameObject("CartelHabilidadUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CartelHabilidadUI));
        DontDestroyOnLoad(root);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        instance = root.GetComponent<CartelHabilidadUI>();
        instance.ConstruirPanel(root.transform);
    }

    private void ConstruirPanel(Transform canvasRoot)
    {
        panel = CrearObjetoUI("PanelCartelHabilidad", canvasRoot);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(900f, 520f);

        fondo = panel.AddComponent<Image>();
        fondo.color = new Color(0.035f, 0.04f, 0.065f, 0.97f);

        borde = panel.AddComponent<Outline>();
        borde.effectColor = new Color(0.55f, 0.9f, 1f, 0.9f);
        borde.effectDistance = new Vector2(4f, -4f);

        icono = CrearImagen("ImagenHabilidad", panel.transform, new Vector2(0f, 90f), new Vector2(220f, 220f));
        titulo = CrearTexto("TituloHabilidad", panel.transform, new Vector2(0f, -65f), new Vector2(760f, 70f), 46f, FontStyles.Bold);
        descripcion = CrearTexto("DescripcionHabilidad", panel.transform, new Vector2(0f, -155f), new Vector2(760f, 105f), 27f, FontStyles.Normal);
        ayudaCerrar = CrearTexto("AyudaCerrar", panel.transform, new Vector2(0f, -225f), new Vector2(760f, 45f), 21f, FontStyles.Italic);
        ayudaCerrar.text = "Presiona ESPACIO, ENTER o ESC para continuar";

        panel.SetActive(false);
    }

    private void MostrarInterno(Sprite imagen, string textoTitulo, string textoDescripcion, string textoAyuda, bool pausa, EstiloCartelHabilidad estilo)
    {
        estiloActivo = estilo ?? new EstiloCartelHabilidad();
        AplicarEstilo(estiloActivo);
        icono.sprite = imagen;
        icono.gameObject.SetActive(imagen != null);
        titulo.text = string.IsNullOrWhiteSpace(textoTitulo) ? "NUEVA HABILIDAD" : textoTitulo;
        descripcion.text = textoDescripcion ?? string.Empty;
        ayudaCerrar.text = string.IsNullOrWhiteSpace(textoAyuda)
            ? "Presiona ESPACIO, ENTER o ESC para continuar"
            : textoAyuda;

        pausarJuego = pausa;
        escalaAnterior = Time.timeScale;
        if (pausarJuego) Time.timeScale = 0f;

        abierto = true;
        abiertoDesde = Time.unscaledTime;
        panel.SetActive(true);
    }

    private void AplicarEstilo(EstiloCartelHabilidad estilo)
    {
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchoredPosition = estilo.posicionPanel;
        panelRect.sizeDelta = estilo.tamanoPanel;
        fondo.color = estilo.colorFondo;
        borde.effectColor = estilo.colorBorde;
        borde.effectDistance = estilo.grosorBorde;

        AplicarRect(icono.rectTransform, estilo.posicionImagen, estilo.tamanoImagen);
        AplicarTexto(titulo, estilo.tipografiaTitulo, estilo.tamanoTitulo, estilo.colorTitulo,
            estilo.posicionTitulo, estilo.cajaTitulo, estilo.estiloTitulo);
        AplicarTexto(descripcion, estilo.tipografiaDescripcion, estilo.tamanoDescripcion, estilo.colorDescripcion,
            estilo.posicionDescripcion, estilo.cajaDescripcion, estilo.estiloDescripcion);
        AplicarTexto(ayudaCerrar, estilo.tipografiaAyuda, estilo.tamanoAyuda, estilo.colorAyuda,
            estilo.posicionAyuda, estilo.cajaAyuda, estilo.estiloAyuda);
    }

    private static void AplicarRect(RectTransform rect, Vector2 posicion, Vector2 tamano)
    {
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
    }

    private static void AplicarTexto(TMP_Text texto, TMP_FontAsset fuente, float tamano, Color color,
        Vector2 posicion, Vector2 caja, FontStyles estilo)
    {
        if (fuente != null) texto.font = fuente;
        texto.fontSize = tamano;
        texto.color = color;
        texto.fontStyle = estilo;
        AplicarRect(texto.rectTransform, posicion, caja);
    }

    private void Update()
    {
        if (!abierto) return;

        // Permite editar posiciones, tamanos, tipografias y colores desde el Inspector
        // y ver el cambio inmediatamente mientras el cartel esta abierto.
        if (estiloActivo != null) AplicarEstilo(estiloActivo);

        if (Keyboard.current == null || Time.unscaledTime - abiertoDesde < 0.2f) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame ||
            Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cerrar();
        }
    }

    public void Cerrar()
    {
        if (!abierto) return;
        abierto = false;
        panel.SetActive(false);
        if (pausarJuego) Time.timeScale = escalaAnterior;
    }

    private static GameObject CrearObjetoUI(string nombre, Transform padre)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.transform.SetParent(padre, false);
        return objeto;
    }

    private static Image CrearImagen(string nombre, Transform padre, Vector2 posicion, Vector2 tamano)
    {
        GameObject objeto = CrearObjetoUI(nombre, padre);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
        Image imagen = objeto.AddComponent<Image>();
        imagen.preserveAspect = true;
        imagen.raycastTarget = false;
        return imagen;
    }

    private static TMP_Text CrearTexto(string nombre, Transform padre, Vector2 posicion, Vector2 tamano, float tamanoFuente, FontStyles estilo)
    {
        GameObject objeto = CrearObjetoUI(nombre, padre);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;

        TextMeshProUGUI texto = objeto.AddComponent<TextMeshProUGUI>();
        texto.alignment = TextAlignmentOptions.Center;
        texto.fontSize = tamanoFuente;
        texto.fontStyle = estilo;
        texto.color = Color.white;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.raycastTarget = false;
        return texto;
    }
}
