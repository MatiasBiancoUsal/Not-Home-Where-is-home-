using UnityEngine;
using TMPro;

// ============================================================
//  MARCADOR DE PUNTOS
//  Muestra DOS cosas:
//    - lo juntado en ESTA zona, sobre el total de la zona. Ej: "30 / 911"
//    - lo juntado en TODO el juego, solo el numero.         Ej: "totales: 1250"
// ============================================================
public class ScoreUI : MonoBehaviour
{
    [Tooltip("El texto del marcador de la zona (el que ya estaba).")]
    public TextMeshProUGUI scoreText;

    [Header("Marcador del juego entero")]
    [Tooltip("Activo: ademas de los puntos de la zona, muestra los de todo el juego.")]
    public bool mostrarTotalDelJuego = true;
    [Tooltip("Texto aparte para el total del juego. Si lo dejas VACIO, el total se agrega " +
             "como una segunda linea abajo del marcador de la zona.")]
    public TextMeshProUGUI textoTotalDelJuego;
    [Tooltip("Palabra que acompaña al total del juego. Vacio = solo el numero.")]
    public string etiquetaDelTotal = "totales:";
    [Tooltip("Tamaño de la segunda linea respecto del marcador de la zona (solo si no usas un texto aparte).")]
    [Range(0.3f, 1f)] public float tamanioDeLaSegundaLinea = 0.6f;

    [Header("Rebote al sumar puntos")]
    [Tooltip("El marcador pega un salto cuando el numero cambia. Destildalo si te marea.")]
    public bool rebotarAlSumar = true;
    [Tooltip("Cuanto crece de golpe. 1.25 = un 25% mas grande.")]
    [Min(1f)] public float escalaDelRebote = 1.25f;
    [Tooltip("Cuanto dura el rebote, en segundos.")]
    [Min(0.05f)] public float duracionDelRebote = 0.22f;

    private int ultimoPuntaje;
    private bool yaMostroAlgunaVez;
    private Coroutine reboteCo;

    // Hace que el marcador crezca de golpe y vuelva a su tamaño, para que se note que sumo.
    private void Rebotar()
    {
        if (!rebotarAlSumar || scoreText == null) return;

        if (reboteCo != null) StopCoroutine(reboteCo);
        reboteCo = StartCoroutine(RutinaDelRebote());
    }

    private System.Collections.IEnumerator RutinaDelRebote()
    {
        Transform t = scoreText.transform;
        Vector3 normal = Vector3.one;
        float tiempo = 0f;

        while (tiempo < duracionDelRebote)
        {
            tiempo += Time.unscaledDeltaTime;
            float avance = Mathf.Clamp01(tiempo / duracionDelRebote);

            // Sube hasta el pico en el primer tercio y vuelve en el resto.
            float escala = avance < 0.33f
                ? Mathf.Lerp(1f, escalaDelRebote, avance / 0.33f)
                : Mathf.Lerp(escalaDelRebote, 1f, (avance - 0.33f) / 0.67f);

            t.localScale = normal * escala;
            yield return null;
        }

        t.localScale = normal;
        reboteCo = null;
    }

    private void Start()
    {
        ScoreManager.Instance.OnScoreChanged += UpdateScore;
        UpdateScore(ScoreManager.Instance.CurrentScore);
    }

    private void UpdateScore(int score)
    {
        ScoreManager sm = ScoreManager.Instance;
        if (sm == null || scoreText == null) return;

        // El rebote solo cuando el numero CAMBIA de verdad, no en el primer dibujado.
        if (yaMostroAlgunaVez && score != ultimoPuntaje) Rebotar();
        ultimoPuntaje = score;
        yaMostroAlgunaVez = true;

        string zona = score + " / " + sm.PuntajeTotal;
        // El total del juego va solo con el numero: sin el "de tanto".
        string total = (string.IsNullOrEmpty(etiquetaDelTotal) ? "" : etiquetaDelTotal + " ") +
                       sm.PuntajeGlobal;

        if (!mostrarTotalDelJuego)
        {
            scoreText.text = zona;
            return;
        }

        if (textoTotalDelJuego != null)
        {
            scoreText.text = zona;
            textoTotalDelJuego.text = total;
            return;
        }

        // Sin texto aparte: va como segunda linea, un poco mas chica.
        int tamanio = Mathf.RoundToInt(tamanioDeLaSegundaLinea * 100f);
        scoreText.text = zona + "\n<size=" + tamanio + "%>" + total + "</size>";
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= UpdateScore;
    }
}
