using System.Collections;
using UnityEngine;

// Va EN el PANEL del menu de pausa (la caja centrada con los botones, NO el canvas completo).
// Al abrir lo anima: FADE (transparencia) + ESCALA, y opcionalmente hace aparecer los botones
// ESCALONADOS (uno detras de otro). Corre en tiempo REAL (unscaled), porque al pausar el
// timeScale queda en 0. Es todo animacion de UI: anda en WebGL/itch.io sin shaders.
//
// Lo dispara el PauseMenuManager al abrir la pausa (campo "transition").
[RequireComponent(typeof(CanvasGroup))]
public class PauseMenuTransition : MonoBehaviour
{
    [Header("Panel: fade + escala")]
    [Tooltip("Cuanto dura la aparicion del panel (segundos).")]
    public float duracion = 0.25f;
    [Tooltip("Escala desde la que arranca el panel (1 = sin escala). 0.7 = entra creciendo.")]
    public float escalaInicial = 0.7f;

    [Header("Botones escalonados (opcional)")]
    [Tooltip("Si esta activo, los botones aparecen uno detras de otro DESPUES del panel.")]
    public bool escalonarBotones = false;
    [Tooltip("Los botones EN ORDEN de aparicion (arrastralos aca). Funciona con botones TextMeshPro igual.")]
    public RectTransform[] botones;
    [Tooltip("Retraso entre un boton y el siguiente (segundos).")]
    public float retrasoEntreBotones = 0.07f;
    [Tooltip("Cuanto dura el 'pop' de cada boton (segundos).")]
    public float duracionBoton = 0.18f;

    private CanvasGroup cg;
    private RectTransform rt;

    private void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        rt = GetComponent<RectTransform>();
    }

    // Lo llama el PauseMenuManager al abrir la pausa.
    public void PlayOpen()
    {
        StopAllCoroutines();
        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        // estado inicial: invisible y mas chico
        cg.alpha = 0f;
        rt.localScale = Vector3.one * escalaInicial;

        // si escalonamos, los botones arrancan en escala 0 (no se ven todavia)
        if (escalonarBotones && botones != null)
        {
            foreach (RectTransform b in botones)
                if (b != null) b.localScale = Vector3.zero;
        }

        // animacion del panel: fade + escala (en tiempo REAL, asi corre con timeScale en 0)
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float e = EaseOut(Mathf.Clamp01(t / duracion));
            cg.alpha = e;
            rt.localScale = Vector3.one * Mathf.Lerp(escalaInicial, 1f, e);
            yield return null;
        }
        cg.alpha = 1f;
        rt.localScale = Vector3.one;

        // botones escalonados (cada uno arranca con su retraso)
        if (escalonarBotones && botones != null)
        {
            for (int i = 0; i < botones.Length; i++)
            {
                if (botones[i] != null)
                    StartCoroutine(PopBoton(botones[i], i * retrasoEntreBotones));
            }
        }
    }

    private IEnumerator PopBoton(RectTransform b, float retraso)
    {
        float d = 0f;
        while (d < retraso) { d += Time.unscaledDeltaTime; yield return null; }

        float t = 0f;
        while (t < duracionBoton)
        {
            t += Time.unscaledDeltaTime;
            b.localScale = Vector3.one * EaseOut(Mathf.Clamp01(t / duracionBoton));
            yield return null;
        }
        b.localScale = Vector3.one;
    }

    // ease-out cubico: arranca rapido y frena suave al final.
    private float EaseOut(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }
}
