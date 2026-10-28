using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  AVISO EN PANTALLA
//
//  Un cartelito corto abajo en el centro, con la misma entrada letra por letra que
//  usan los carteles de habilidad. Sirve para avisarle cosas al jugador sin frenar
//  el juego: "checkpoint guardado", "posicion guardada", etc.
//
//  NO hay que ponerlo en ninguna escena: se crea solo la primera vez que alguien lo
//  usa, y se borra al cambiar de escena.
//
//  Desde cualquier script:
//      AvisoEnPantalla.Mostrar("checkpoint guardado");
// ============================================================
public class AvisoEnPantalla : MonoBehaviour
{
    private static AvisoEnPantalla instancia;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarStatics()
    {
        // Reload Domain apagado: los static se arrastran entre sesiones de Play.
        instancia = null;
    }

    // ---------- Ajustes (los mismos para todos los avisos) ----------

    private const float TAMANIO = 30f;
    private const float ALTURA_DESDE_ABAJO = 210f;
    private const float DURACION_EN_PANTALLA = 2.2f;
    private const float FADE_OUT = 0.6f;

    private static readonly EntradaDeTexto ENTRADA = new EntradaDeTexto
    {
        activa = true,
        retrasoEntreLetras = 0.03f,
        duracionPorLetra = 0.28f,
        zoomInicial = 0.5f,
        desplazamientoInicial = 10f
    };

    private TextMeshProUGUI texto;
    private CanvasGroup grupo;
    private Coroutine rutina;

    // La cola evita que dos avisos seguidos se pisen: el segundo espera al primero.
    private readonly Queue<string> pendientes = new Queue<string>();
    private bool mostrando;

    // ---------- API ----------

    public static void Mostrar(string mensaje)
    {
        if (string.IsNullOrEmpty(mensaje)) return;
        if (!Application.isPlaying) return;

        if (instancia == null)
        {
            GameObject go = new GameObject("AVISO EN PANTALLA");
            instancia = go.AddComponent<AvisoEnPantalla>();
            instancia.Construir();
        }

        instancia.Encolar(mensaje);
    }

    // ---------- Interno ----------

    private void Encolar(string mensaje)
    {
        pendientes.Enqueue(mensaje);
        if (!mostrando) rutina = StartCoroutine(RutinaDeLaCola());
    }

    private IEnumerator RutinaDeLaCola()
    {
        mostrando = true;

        while (pendientes.Count > 0)
        {
            string mensaje = pendientes.Dequeue();

            texto.text = mensaje;
            texto.ForceMeshUpdate();
            grupo.alpha = 1f;

            yield return AnimacionDeTexto.EntrarPorLetra(texto, ENTRADA);
            yield return new WaitForSeconds(DURACION_EN_PANTALLA);
            yield return Fundir(1f, 0f, FADE_OUT);
        }

        mostrando = false;
        rutina = null;
    }

    private IEnumerator Fundir(float desde, float hasta, float duracion)
    {
        float t = 0f;
        grupo.alpha = desde;

        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(desde, hasta, Mathf.Clamp01(t / duracion));
            yield return null;
        }

        grupo.alpha = hasta;
    }

    private void Construir()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // encima del HUD, debajo de cinematicas y transiciones

        CanvasScaler escalador = gameObject.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        GameObject textoGo = new GameObject("TEXTO", typeof(RectTransform), typeof(CanvasRenderer),
                                            typeof(TextMeshProUGUI), typeof(CanvasGroup));
        RectTransform rect = textoGo.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, ALTURA_DESDE_ABAJO);
        rect.sizeDelta = new Vector2(1400f, 90f);

        texto = textoGo.GetComponent<TextMeshProUGUI>();
        texto.fontSize = TAMANIO;
        texto.color = new Color(1f, 1f, 1f, 0.9f);
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;

        grupo = textoGo.GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
    }
}
