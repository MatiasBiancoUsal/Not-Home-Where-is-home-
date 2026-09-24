using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class ArmarPaginaMapaElementos : MonoBehaviour
{
    private static bool construyendo;
    public Sprite teclaTab;
    public Sprite checkpoint;
    public Sprite orbeVida;
    public Sprite habilidad;
    public Sprite puerta;

    private void Update()
    {
        if (Application.isPlaying || construyendo) return;
        construyendo = true;

        LimpiarDuplicados();
        if (transform.Find("TITULO MAPA Y ELEMENTOS") != null)
        {
            construyendo = false;
            enabled = false;
            return;
        }

        CrearTexto("TITULO MAPA Y ELEMENTOS", "MAPA Y ELEMENTOS", new Vector2(0f, 390f), new Vector2(1200f, 110f), 70f);

        CrearIcono("TECLA TAB", teclaTab, new Vector2(-500f, 190f), new Vector2(300f, 150f));
        CrearTexto("TEXTO MAPA", "MAPA", new Vector2(-160f, 220f), new Vector2(500f, 70f), 44f);
        CrearTexto("EXPLICACION MAPA", "Presiona TAB para abrir o cerrar el mapa.", new Vector2(40f, 145f), new Vector2(900f, 70f), 29f);

        CrearElemento("CHECKPOINT", checkpoint, new Vector2(-630f, -80f), "Guarda tu punto de reaparicion.");
        CrearElemento("ORBE DE VIDA", orbeVida, new Vector2(-210f, -80f), "Recupera parte de tu vida.");
        CrearElemento("HABILIDAD", habilidad, new Vector2(210f, -80f), "Permite adquirir una habilidad.");
        CrearElemento("PUERTA", puerta, new Vector2(630f, -80f), "Conecta con la siguiente zona.");

        construyendo = false;
        enabled = false;
    }

    private void LimpiarDuplicados()
    {
        ConservarMasCercano("TITULO MAPA Y ELEMENTOS", new Vector2(0f, 390f));
        ConservarMasCercano("TECLA TAB", new Vector2(-500f, 190f));
        ConservarMasCercano("TEXTO MAPA", new Vector2(-160f, 220f));
        ConservarMasCercano("EXPLICACION MAPA", new Vector2(40f, 145f));

        LimpiarElemento("CHECKPOINT", new Vector2(-630f, -80f));
        LimpiarElemento("ORBE DE VIDA", new Vector2(-210f, -80f));
        LimpiarElemento("HABILIDAD", new Vector2(210f, -80f));
        LimpiarElemento("PUERTA", new Vector2(630f, -80f));
    }

    private void LimpiarElemento(string nombre, Vector2 posicion)
    {
        ConservarMasCercano("ICONO " + nombre, posicion);
        ConservarMasCercano("TITULO " + nombre, posicion + new Vector2(0f, -125f));
        ConservarMasCercano("EXPLICACION " + nombre, posicion + new Vector2(0f, -205f));
    }

    private void ConservarMasCercano(string nombre, Vector2 posicionEsperada)
    {
        Transform elegido = null;
        float mejorDistancia = float.PositiveInfinity;

        foreach (Transform hijo in transform)
        {
            if (hijo.name != nombre) continue;
            RectTransform rect = hijo as RectTransform;
            float distancia = rect != null
                ? (rect.anchoredPosition - posicionEsperada).sqrMagnitude
                : float.PositiveInfinity;
            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                elegido = hijo;
            }
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform hijo = transform.GetChild(i);
            if (hijo.name == nombre && hijo != elegido)
                DestroyImmediate(hijo.gameObject);
        }
    }

    private void CrearElemento(string nombre, Sprite sprite, Vector2 posicion, string descripcion)
    {
        CrearIcono("ICONO " + nombre, sprite, posicion, new Vector2(170f, 170f));
        CrearTexto("TITULO " + nombre, nombre, posicion + new Vector2(0f, -125f), new Vector2(360f, 55f), 32f);
        CrearTexto("EXPLICACION " + nombre, descripcion, posicion + new Vector2(0f, -205f), new Vector2(350f, 90f), 23f);
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

    private void CrearTexto(string nombre, string contenido, Vector2 posicion, Vector2 tamano, float fuente)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        objeto.layer = gameObject.layer;
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;

        TextMeshProUGUI texto = objeto.GetComponent<TextMeshProUGUI>();
        Transform paginaUno = transform.parent.Find("PAGINA 1 - MOVIMIENTO Y SALTO");
        TextMeshProUGUI referencia = paginaUno != null ? paginaUno.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        if (referencia != null) texto.font = referencia.font;
        texto.text = contenido;
        texto.fontSize = fuente;
        texto.color = Color.white;
        texto.alignment = TextAlignmentOptions.Center;
        texto.enableWordWrapping = true;
        texto.raycastTarget = false;
    }
}
