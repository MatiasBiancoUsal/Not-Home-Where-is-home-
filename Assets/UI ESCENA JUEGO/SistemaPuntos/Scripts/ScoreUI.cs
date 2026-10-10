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

    private void Start()
    {
        ScoreManager.Instance.OnScoreChanged += UpdateScore;
        UpdateScore(ScoreManager.Instance.CurrentScore);
    }

    private void UpdateScore(int score)
    {
        ScoreManager sm = ScoreManager.Instance;
        if (sm == null || scoreText == null) return;

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
