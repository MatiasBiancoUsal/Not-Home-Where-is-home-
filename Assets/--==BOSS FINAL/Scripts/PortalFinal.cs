using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ============================================================
//  PORTAL FINAL (el cierre del juego)
//
//  Va EN el portal que aparece despues de la cinematica final del boss, en un objeto
//  con un Collider2D marcado como Is Trigger. Cuando la niña lo toca:
//
//     1. Se le sacan los controles y la pantalla se funde a BLANCO.
//     2. Sobre el blanco se escriben, una despues de la otra, las frases del final
//        (con la misma animacion de tipeo que el resto del juego).
//     3. Fundido a NEGRO.
//     4. Carga la escena de CREDITOS.
//
//  Toda la pantalla (el blanco, el negro y los textos) se arma por codigo: no hay que
//  crear ningun Canvas a mano. Los tiempos y las frases se ajustan desde el Inspector.
// ============================================================
[RequireComponent(typeof(Collider2D))]
public class PortalFinal : MonoBehaviour
{
    [Header("Quien lo activa")]
    public string tagJugador = "Player";

    [Header("Frases del final")]
    [Tooltip("Se escriben una despues de la otra, cada una con su fundido. Se pueden agregar mas.")]
    [TextArea]
    public string[] frases =
    {
        "gracias por ayudarme a escapar...",
        "aunque... algunos niños aún quedaron atrapados..."
    };
    [Tooltip("Color del texto sobre el fondo blanco.")]
    public Color colorDelTexto = new Color(0.1f, 0.1f, 0.1f);
    [Min(1f)] public float tamanoDelTexto = 54f;
    [Tooltip("Tipografia de las frases. Si lo dejas vacio usa la que trae TextMeshPro por defecto.")]
    public TMP_FontAsset tipografia;

    [Header("Tiempos (segundos)")]
    [Tooltip("Cuanto tarda la pantalla en ponerse BLANCA al tocar el portal.")]
    [Min(0f)] public float fundidoABlanco = 2.5f;
    [Tooltip("Silencio en blanco antes de que empiece a escribirse la primera frase.")]
    [Min(0f)] public float esperaEnBlanco = 1.2f;
    [Tooltip("Letras por segundo. Pocas = mas lento y pausado.")]
    [Min(1f)] public float letrasPorSegundo = 9f;
    [Tooltip("Cuanto se queda cada frase completa antes de desvanecerse.")]
    [Min(0f)] public float esperaDespuesDeCadaFrase = 2f;
    [Tooltip("Cuanto tarda cada frase en desvanecerse.")]
    [Min(0.05f)] public float fadeOutDeCadaFrase = 1.5f;
    [Tooltip("Silencio entre que se fue una frase y empieza la siguiente.")]
    [Min(0f)] public float esperaEntreFrases = 0.8f;
    [Tooltip("Cuanto tarda el blanco en pasar a NEGRO despues de la ultima frase.")]
    [Min(0f)] public float fundidoANegro = 2.5f;
    [Tooltip("Silencio en negro antes de que carguen los creditos.")]
    [Min(0f)] public float esperaEnNegro = 1f;
    [Tooltip("Cuanto tardan los CREDITOS en aparecer desde el negro, ya con la escena cargada.")]
    [Min(0f)] public float fadeInDeLosCreditos = 3.5f;

    [Header("Sonido")]
    [Tooltip("Sonidos del tipeo (opcional). Si pones varios, en cada letra se elige uno al azar.")]
    public AudioClip[] sonidosTipeo;
    [Range(0f, 1f)] public float volumenTipeo = 0.5f;
    [Tooltip("Sonido al entrar al portal (opcional).")]
    public AudioClip sonidoDelPortal;
    [Range(0f, 1f)] public float volumenDelPortal = 0.8f;
    [Tooltip("Cuanto tarda la musica en apagarse mientras la pantalla se pone blanca.")]
    [Min(0f)] public float fadeOutMusica = 3f;

    [Header("A donde va")]
    [Tooltip("Nombre EXACTO de la escena de creditos, igual que en Build Settings.")]
    public string escenaDeCreditos = "Credits";

    private bool usado;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (usado || !other.CompareTag(tagJugador)) return;

        usado = true;
        StartCoroutine(Secuencia(other));
    }

    private IEnumerator Secuencia(Collider2D quienEntro)
    {
        // La niña deja de responder: a partir de aca la escena es solo pantalla.
        PlayerController pc = quienEntro.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            Rigidbody2D rb = pc.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            pc.enabled = false;
        }

        if (sonidoDelPortal != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(sonidoDelPortal, volumenDelPortal);
        }

        // Canvas propio, por encima de todo lo demas.
        Canvas canvas = CrearCanvas();
        Image velo = CrearVelo(canvas.transform, Color.white);
        CanvasGroup grupoVelo = velo.GetComponent<CanvasGroup>();
        grupoVelo.alpha = 0f;

        // 1) A blanco, mientras la musica se apaga.
        StartCoroutine(BajarLaMusica());
        yield return Fundir(grupoVelo, 0f, 1f, fundidoABlanco);

        if (esperaEnBlanco > 0f) yield return new WaitForSecondsRealtime(esperaEnBlanco);

        // 2) Las frases, una por una.
        TextMeshProUGUI texto = CrearTexto(canvas.transform);
        CanvasGroup grupoTexto = texto.GetComponent<CanvasGroup>();

        foreach (string frase in frases)
        {
            if (string.IsNullOrEmpty(frase)) continue;

            texto.text = frase;
            texto.maxVisibleCharacters = 0;
            texto.ForceMeshUpdate();
            grupoTexto.alpha = 1f;

            yield return Tipear(texto);

            if (esperaDespuesDeCadaFrase > 0f) yield return new WaitForSecondsRealtime(esperaDespuesDeCadaFrase);
            yield return Fundir(grupoTexto, 1f, 0f, fadeOutDeCadaFrase);
            if (esperaEntreFrases > 0f) yield return new WaitForSecondsRealtime(esperaEntreFrases);
        }

        // 3) El blanco se vuelve negro.
        Image negro = CrearVelo(canvas.transform, Color.black);
        CanvasGroup grupoNegro = negro.GetComponent<CanvasGroup>();
        grupoNegro.alpha = 0f;
        yield return Fundir(grupoNegro, 0f, 1f, fundidoANegro);

        // El blanco ya no se ve (el negro lo tapa) y se va a los creditos SOLO el negro.
        // Si lo dejaramos, al apagarse el negro aparecerian los creditos detras del blanco.
        Destroy(velo.gameObject);
        if (texto != null) Destroy(texto.gameObject);

        if (esperaEnNegro > 0f) yield return new WaitForSecondsRealtime(esperaEnNegro);

        // 4) Creditos.
        if (string.IsNullOrEmpty(escenaDeCreditos))
        {
            Destroy(canvas.gameObject);
            yield break;
        }

        // El velo negro sobrevive al cambio de escena (es DontDestroyOnLoad), asi que los
        // creditos aparecen DESDE el negro en vez de aparecer de golpe.
        //
        // De sacarlo se encarga un componente que viaja CON el velo: esta corutina muere
        // al cargar la otra escena (el portal se destruye con la zona), y si nadie lo
        // apagara, la pantalla quedaria negra para siempre.
        FundidoDeEntrada entrada = canvas.gameObject.AddComponent<FundidoDeEntrada>();
        entrada.grupo = grupoNegro;
        entrada.duracion = fadeInDeLosCreditos;

        SceneManager.LoadScene(escenaDeCreditos);
    }

    // Apaga el velo negro una vez que la escena nueva ya esta cargada, y se destruye solo.
    private class FundidoDeEntrada : MonoBehaviour
    {
        public CanvasGroup grupo;
        public float duracion = 2f;

        private IEnumerator Start()
        {
            // Dos frames para que la escena nueva termine de armarse.
            yield return null;
            yield return null;

            float t = 0f;
            while (t < duracion && grupo != null)
            {
                t += Time.unscaledDeltaTime;
                grupo.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / Mathf.Max(0.01f, duracion)));
                yield return null;
            }

            Destroy(gameObject);
        }
    }

    // ---------- Pedazos ----------

    private Canvas CrearCanvas()
    {
        GameObject go = new GameObject("FINAL DEL JUEGO", typeof(Canvas), typeof(CanvasScaler));
        DontDestroyOnLoad(go); // que no se corte el fundido si algo recarga la escena

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // por encima del HUD y de todo cartel

        CanvasScaler escala = go.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);

        return canvas;
    }

    private Image CrearVelo(Transform padre, Color color)
    {
        GameObject go = new GameObject("VELO", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(padre, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        go.transform.SetAsLastSibling();

        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        return img;
    }

    private TextMeshProUGUI CrearTexto(Transform padre)
    {
        GameObject go = new GameObject("FRASE", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(CanvasGroup));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(padre, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1500f, 300f);

        TextMeshProUGUI texto = go.GetComponent<TextMeshProUGUI>();
        if (tipografia != null) texto.font = tipografia;
        texto.fontSize = tamanoDelTexto;
        texto.color = colorDelTexto;
        texto.alignment = TextAlignmentOptions.Center;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.raycastTarget = false;

        return texto;
    }

    // Muestra la frase letra por letra. Todo en tiempo REAL, por si algo dejo el juego congelado.
    private IEnumerator Tipear(TextMeshProUGUI texto)
    {
        int total = texto.textInfo.characterCount;
        float esperaPorLetra = 1f / Mathf.Max(1f, letrasPorSegundo);

        for (int i = 1; i <= total; i++)
        {
            texto.maxVisibleCharacters = i;
            SonarTipeo();
            yield return new WaitForSecondsRealtime(esperaPorLetra);
        }

        texto.maxVisibleCharacters = int.MaxValue;
    }

    private AudioClip ultimoClipDeTipeo;

    private void SonarTipeo()
    {
        if (sonidosTipeo == null || sonidosTipeo.Length == 0) return;
        if (AudioManager.Instance == null) return;

        AudioClip elegido = null;
        for (int intento = 0; intento < 6; intento++)
        {
            AudioClip candidato = sonidosTipeo[Random.Range(0, sonidosTipeo.Length)];
            if (candidato == null) continue;

            elegido = candidato;
            if (candidato != ultimoClipDeTipeo || sonidosTipeo.Length == 1) break;
        }

        if (elegido == null) return;

        ultimoClipDeTipeo = elegido;
        AudioManager.Instance.PlaySFX(elegido, volumenTipeo);
    }

    private IEnumerator BajarLaMusica()
    {
        AudioSource musica = AudioManager.Instance != null ? AudioManager.Instance.musicSource : null;
        if (musica == null) yield break;

        float inicial = musica.volume;
        float t = 0f;

        while (t < fadeOutMusica)
        {
            t += Time.unscaledDeltaTime;
            musica.volume = Mathf.Lerp(inicial, 0f, Mathf.Clamp01(t / Mathf.Max(0.01f, fadeOutMusica)));
            yield return null;
        }

        musica.volume = 0f;
        musica.Stop();
        // Lo dejamos listo para la musica de los creditos, que entra con su propio volumen.
        musica.volume = inicial;
    }

    private IEnumerator Fundir(CanvasGroup grupo, float desde, float hasta, float duracion)
    {
        float t = 0f;
        grupo.alpha = desde;

        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(desde, hasta, Mathf.Clamp01(t / Mathf.Max(0.01f, duracion)));
            yield return null;
        }

        grupo.alpha = hasta;
    }
}
