using System.Collections;
using UnityEngine;

// Va EN el player. Cuando el player toca el suelo, espera un toque y muestra el cartel de "como
// moverte" con FADE IN; lo oculta con FADE OUT unos segundos despues de que empieza a moverse.
public class TutorialMovimiento : MonoBehaviour
{
    [Tooltip("El cartel (UI) que se muestra y se oculta.")]
    public GameObject cartel;

    [Header("Tiempos")]
    [Tooltip("Segundos que espera despues de tocar el piso, antes de aparecer (para que no sea instantaneo).")]
    public float delayAntesDeAparecer = 0.5f;
    [Tooltip("Segundos visibles despues de que el player empieza a moverse, antes de empezar a desaparecer.")]
    public float tiempoAntesDeOcultar = 3f;

    [Header("Fades")]
    [Tooltip("Duracion del fade in (aparecer), en segundos.")]
    public float fadeIn = 0.4f;
    [Tooltip("Duracion del fade out (desaparecer), en segundos.")]
    public float fadeOut = 0.5f;

    private PlayerJump jump;
    private PlayerMovement movement;
    private CanvasGroup cg;

    private bool detectoPiso = false; // ya toco el suelo (arranco el delay para aparecer)
    private bool visible = false;     // el cartel ya aparecio
    private bool ocultado = false;    // ya empezo a desaparecer
    private bool contando = false;    // ya empezo a moverse: arranca el conteo
    private float timer = 0f;
    private Coroutine fadeCo;

    private void Awake()
    {
        jump = GetComponent<PlayerJump>();
        movement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        if (cartel == null) return;

        // Para hacer fade necesitamos un CanvasGroup; si el cartel no tiene, se lo agregamos solos.
        cg = cartel.GetComponent<CanvasGroup>();
        if (cg == null) cg = cartel.AddComponent<CanvasGroup>();

        cg.blocksRaycasts = false; // es solo un cartel: que no tape clicks de otra UI
        cg.alpha = 0f;             // arranca invisible
        cartel.SetActive(true);    // activo pero transparente, listo para el fade in
    }

    private void Update()
    {
        if (cg == null) return;

        // Apenas toca el suelo, arranca el delay antes de aparecer (al caer, o si arranca apoyado).
        if (!detectoPiso && jump != null && jump.IsGrounded)
        {
            detectoPiso = true;
            StartCoroutine(AparecerConDelay());
        }

        // El conteo para irse solo corre DESPUES de que el cartel aparecio.
        if (!visible || ocultado) return;

        // Cuando el player se mueve (A/D), arranca el conteo.
        if (!contando && movement != null && movement.IsMoving)
        {
            contando = true;
        }

        // Pasado el tiempo, se va con FADE OUT.
        if (contando)
        {
            timer += Time.deltaTime;
            if (timer >= tiempoAntesDeOcultar)
            {
                ocultado = true;
                FadeTo(0f, fadeOut, true);
            }
        }
    }

    private IEnumerator AparecerConDelay()
    {
        if (delayAntesDeAparecer > 0f)
            yield return new WaitForSeconds(delayAntesDeAparecer);

        visible = true;
        FadeTo(1f, fadeIn, false);
    }

    private void FadeTo(float objetivo, float dur, bool apagarAlFinal)
    {
        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(FadeRoutine(objetivo, dur, apagarAlFinal));
    }

    private IEnumerator FadeRoutine(float objetivo, float dur, bool apagarAlFinal)
    {
        float inicial = cg.alpha;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(inicial, objetivo, t / dur);
            yield return null;
        }
        cg.alpha = objetivo;

        if (apagarAlFinal)
        {
            cartel.SetActive(false);
            enabled = false; // ya cumplio su funcion
        }
    }
}
