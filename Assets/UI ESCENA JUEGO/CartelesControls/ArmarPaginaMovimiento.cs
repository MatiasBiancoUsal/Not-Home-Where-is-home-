using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Herramienta visual: arma una sola vez la pagina con los iconos dibujados del proyecto.
// Los objetos resultantes quedan normales y editables en la jerarquia.
[ExecuteAlways]
public class ArmarPaginaMovimiento : MonoBehaviour
{
    public Sprite teclaA;
    public Sprite teclaD;
    public Sprite flechaIzquierda;
    public Sprite flechaDerecha;
    public Sprite teclaEspacio;

    private void Update()
    {
        if (Application.isPlaying) return;

        if (transform.Find("TECLA A") == null)
        {
            CrearIcono("TECLA A", teclaA, new Vector2(-600f, 80f), new Vector2(180f, 180f));
            CrearIcono("TECLA D", teclaD, new Vector2(-380f, 80f), new Vector2(180f, 180f));
            CrearIcono("FLECHA IZQUIERDA", flechaIzquierda, new Vector2(-600f, -150f), new Vector2(180f, 180f));
            CrearIcono("FLECHA DERECHA", flechaDerecha, new Vector2(-380f, -150f), new Vector2(180f, 180f));
            CrearIcono("TECLA ESPACIO", teclaEspacio, new Vector2(400f, -20f), new Vector2(520f, 150f));
            CrearTexto("TEXTO MOVIMIENTO", "MOVIMIENTO", new Vector2(-490f, -300f));
            CrearTexto("TEXTO SALTO", "SALTO", new Vector2(400f, -150f));
        }

        CrearPlacaSiFalta("FONDO TITULO MOVIMIENTO", "TEXTO MOVIMIENTO");
        CrearPlacaSiFalta("FONDO TITULO SALTO", "TEXTO SALTO");
    }

    private void CrearPlacaSiFalta(string nombre, string nombreTexto)
    {
        if (transform.Find(nombre) != null) return;
        Transform texto = transform.Find(nombreTexto);
        if (texto == null) return;

        GameObject placa = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(RectanguloRedondeadoUI));
        placa.layer = gameObject.layer;
        RectTransform rect = placa.GetComponent<RectTransform>();
        RectTransform rectTexto = texto.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = rectTexto.anchorMin;
        rect.anchoredPosition = rectTexto.anchoredPosition;
        rect.sizeDelta = rectTexto.sizeDelta + new Vector2(70f, 24f);

        RectanguloRedondeadoUI fondo = placa.GetComponent<RectanguloRedondeadoUI>();
        fondo.color = new Color(0.035f, 0.075f, 0.12f, 0.94f);
        fondo.radio = 30f;
        fondo.raycastTarget = false;

        Outline borde = placa.AddComponent<Outline>();
        borde.effectColor = new Color(0.35f, 0.85f, 1f, 0.8f);
        borde.effectDistance = new Vector2(3f, -3f);

        Shadow halo = placa.AddComponent<Shadow>();
        halo.effectColor = new Color(0.25f, 0.75f, 1f, 0.42f);
        halo.effectDistance = new Vector2(8f, -8f);

        placa.transform.SetSiblingIndex(texto.GetSiblingIndex());
    }

    private void CrearIcono(string nombre, Sprite sprite, Vector2 posicion, Vector2 tamano)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        objeto.layer = gameObject.layer;
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;

        Image imagen = objeto.GetComponent<Image>();
        imagen.sprite = sprite;
        imagen.preserveAspect = true;
        imagen.raycastTarget = false;
    }

    private void CrearTexto(string nombre, string contenido, Vector2 posicion)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        objeto.layer = gameObject.layer;
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = new Vector2(500f, 80f);

        TextMeshProUGUI texto = objeto.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI titulo = GetComponentInChildren<TextMeshProUGUI>();
        if (titulo != null) texto.font = titulo.font;
        texto.text = contenido;
        texto.fontSize = 42f;
        texto.color = Color.white;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;
    }
}
