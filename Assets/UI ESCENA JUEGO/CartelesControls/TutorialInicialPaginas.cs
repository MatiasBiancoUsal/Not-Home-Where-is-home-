using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[ExecuteAlways]
public class TutorialInicialPaginas : MonoBehaviour
{
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
        BuscarPaginas();
        ConfigurarBotones();
        MostrarPagina(1);
        escalaAnterior = Time.timeScale;
        Time.timeScale = 0f;
        abierto = true;
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
