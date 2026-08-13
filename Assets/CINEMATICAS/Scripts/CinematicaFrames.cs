using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// ============================================================
//  CINEMATICA FRAME POR FRAME
//  Reproduce una lista de imagenes en orden, una atras de la otra, mezclandose
//  con un DEGRADADO suave (la nueva aparece encima de la anterior).
//
//  Cada frame tiene su propia DURACION y su propio tiempo de MEZCLA, asi se
//  puede ir ajustando a ojo desde el Inspector sin tocar codigo. La lista se
//  puede reordenar y ampliar arrastrando en el Inspector.
//
//  Mientras corre: congela el juego (timeScale 0), apaga los scripts que le
//  digas (el PlayerController) y esconde el HUD. Todos los tiempos van en
//  tiempo REAL (unscaled), justamente porque el juego queda congelado.
//
//  Va EN el objeto que tiene el Canvas de la cinematica (o en uno vacio que lo
//  referencie). Lo dispara el TriggerCinematica.
// ============================================================

// Un frame de la cinematica: la imagen, cuanto dura y como se mezcla con la siguiente.
[System.Serializable]
public class FrameCinematica
{
    [Tooltip("Solo para reconocerlo en la lista del Inspector. No se ve en el juego.")]
    public string nombre = "Frame";

    [Tooltip("La imagen (Sprite) de este frame.")]
    public Sprite imagen;

    [Tooltip("Segundos que el frame queda QUIETO en pantalla, antes de empezar a mezclarse con el siguiente.")]
    public float duracion = 2f;

    [Tooltip("Segundos que tarda el degradado hacia el frame SIGUIENTE. En el ultimo frame no se usa.")]
    public float fadeAlSiguiente = 0.6f;

    [Header("Sonido de este frame (opcional)")]
    [Tooltip("Efecto que suena cuando aparece este frame. Dejalo vacio si no lleva.")]
    public AudioClip sonido;

    [Range(0f, 1f)]
    public float volumenSonido = 1f;

    [Tooltip("Segundos de espera desde que aparece el frame hasta que suena el efecto.")]
    public float retrasoSonido = 0f;
}

public class CinematicaFrames : MonoBehaviour
{
    // Lo usa el PauseMenuManager para que ESC no abra la pausa durante la cinematica.
    public static bool EnCurso { get; private set; }

    // Igual que en el TriggerCinematica: con Reload Domain desactivado los static se
    // arrastran de una sesion de Play a la otra, asi que lo reiniciamos a mano.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarPartida()
    {
        EnCurso = false;
    }

    [Header("Frames (se reproducen en orden)")]
    [Tooltip("Agregar, sacar y reordenar los frames desde aca. Cada uno con su duracion.")]
    public FrameCinematica[] frames;

    [Header("Objetos de la UI")]
    [Tooltip("El CanvasGroup del panel: hace el fade de entrada y de salida de toda la cinematica.")]
    public CanvasGroup grupo;
    [Tooltip("Las DOS Image que se van turnando para hacer el degradado. Tienen que estar una arriba de la otra, ocupando toda la pantalla.")]
    public Image imagenA;
    public Image imagenB;

    [Header("Entrada: juego -> negro -> primer frame")]
    [Tooltip("Segundos que el juego queda CONGELADO con la niña quieta, antes de empezar el fundido a negro. Le da el momento de 'se detiene todo'.")]
    public float esperaConElJuegoCongelado = 1f;
    [Tooltip("Segundos que tarda la pantalla en ponerse NEGRA cuando arranca la cinematica.")]
    public float fadeAlNegro = 0.7f;
    [Tooltip("Segundos de pantalla NEGRA (sin nada) antes de que aparezca la imagen. Le da respiro.")]
    public float esperaEnNegro = 0.4f;
    [Tooltip("Segundos que tarda el PRIMER frame en aparecer desde el negro.")]
    public float fadeAparecerPrimerFrame = 0.8f;

    [Header("Salida: ultimo frame -> negro -> juego")]
    [Tooltip("Segundos que se queda el ULTIMO frame antes de empezar a irse (aparte de su propia duracion).")]
    public float esperaAntesDeSalir = 0.4f;
    [Tooltip("Segundos que tarda el ULTIMO frame en desaparecer hacia el negro.")]
    public float fadeUltimoFrameANegro = 0.8f;
    [Tooltip("Segundos que tarda el juego en volver a aparecer desde el negro (con la niña quieta en idle).")]
    public float fadeVolverAlJuego = 0.8f;

    [Header("Congelar el juego")]
    [Tooltip("Pone el timeScale en 0 mientras dura la cinematica (se frena todo: player, enemigos, trampas).")]
    public bool congelarTiempo = true;
    [Tooltip("Arrastrar aca el objeto de la niña. Se le apagan los controles mientras dura la cinematica.")]
    public PlayerController playerController;
    [Tooltip("Objetos que se esconden mientras dura: el HUD de puntos, los corazones, el boton de pausa, etc.")]
    public GameObject[] ocultarDuranteCine;

    [Header("Musica de la cinematica")]
    [Tooltip("AudioSource propio de la cinematica. En su Output poner el grupo Music del MainMixer, asi lo afecta el slider de opciones.")]
    public AudioSource musicaSource;
    public AudioClip musica;
    public bool loopMusica = true;
    [Range(0f, 1f)]
    public float volumenMusica = 1f;
    [Tooltip("Segundos que tarda la musica en entrar de a poco.")]
    public float fadeInMusica = 1f;
    [Tooltip("Segundos que tarda la musica en apagarse al final.")]
    public float fadeOutMusica = 1.5f;

    [Header("Efectos de sonido")]
    [Tooltip("AudioSource para los efectos de cada frame. Si lo dejas vacio, usa el AudioManager del juego. Output: grupo SFX.")]
    public AudioSource sfxSource;

    [Header("Saltear")]
    public bool sePuedeSaltear = true;
    [Tooltip("Tecla para saltear la cinematica. NO uses Escape: esa es la del menu de pausa.")]
    public Key teclaSaltear = Key.Space;

    [Header("Al terminar")]
    [Tooltip("Que pasa cuando termina: activar el osito, desbloquear el ataque, prender un cartel, etc.")]
    public UnityEvent alTerminar;

    private bool reproduciendo = false;
    private bool salteando = false;
    private float timeScalePrevio = 1f;

    // El Animator de la niña se maneja con un entero llamado "stateAnim" (lo setean las
    // clases de State Anims). El 1 es la animacion de IDLE (ver IdlePlayerStateAnim).
    private const string PARAM_ANIM = "stateAnim";
    private const int ANIM_IDLE = 1;

    private void Awake()
    {
        // Seguro: EnCurso es estatico y sobrevive a la recarga de la escena.
        // Si una cinematica quedo cortada por una muerte, lo volvemos a dejar en falso.
        EnCurso = false;

        // El Canvas queda SIEMPRE prendido, pero invisible y sin tapar clicks.
        // Asi no hay que andar activandolo y desactivandolo a mano.
        if (grupo != null)
        {
            grupo.alpha = 0f;
            grupo.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (!reproduciendo || !sePuedeSaltear) return;

        if (Keyboard.current != null && Keyboard.current[teclaSaltear].wasPressedThisFrame)
        {
            salteando = true;
        }
    }

    // ---------- API publica ----------

    // La llama el TriggerCinematica (o cualquier boton / UnityEvent).
    public void Reproducir()
    {
        if (reproduciendo) return;

        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("CinematicaFrames: no hay frames cargados.");
            return;
        }
        if (grupo == null || imagenA == null || imagenB == null)
        {
            Debug.LogWarning("CinematicaFrames: faltan asignar el CanvasGroup y las dos Image en el Inspector.");
            return;
        }

        StartCoroutine(Rutina());
    }

    // ---------- Rutina principal ----------

    private IEnumerator Rutina()
    {
        reproduciendo = true;
        EnCurso = true;
        salteando = false;

        Preparar();

        // Estado inicial: TODO invisible (ni el negro ni las imagenes).
        // Las imagenes arrancan en 0 para que el primer fundido muestre SOLO el fondo negro.
        grupo.alpha = 0f;
        imagenA.sprite = frames[0].imagen;
        PonerAlpha(imagenA, 0f);
        PonerAlpha(imagenB, 0f);
        imagenA.transform.SetAsLastSibling();

        ArrancarMusica();

        // 1) Momento de silencio: el juego queda congelado, con la niña frenada en idle
        //    y la pantalla todavia limpia. Recien despues arranca el fundido.
        yield return EsperarReal(esperaConElJuegoCongelado);

        // 2) El juego se funde a NEGRO.
        yield return FadeGrupo(1f, fadeAlNegro);

        // 3) Un respiro en negro.
        yield return EsperarReal(esperaEnNegro);

        // 4) Desde el negro aparece el PRIMER frame.
        yield return FadeImagen(imagenA, 1f, fadeAparecerPrimerFrame);

        Image actual = imagenA;
        Image libre = imagenB;

        for (int i = 0; i < frames.Length; i++)
        {
            FrameCinematica f = frames[i];

            // El efecto de sonido del frame arranca junto con el frame.
            if (f.sonido != null)
            {
                StartCoroutine(SonarConRetraso(f));
            }

            // El frame se queda quieto su duracion.
            yield return EsperarReal(f.duracion);
            if (salteando) break;

            // Del ultimo frame no se mezcla a nada.
            if (i == frames.Length - 1) break;

            // Degradado: el frame que viene aparece ENCIMA del actual.
            libre.sprite = frames[i + 1].imagen;
            yield return Mezclar(actual, libre, f.fadeAlSiguiente);

            Image tmp = actual;
            actual = libre;
            libre = tmp;
        }

        if (!salteando)
        {
            yield return EsperarReal(esperaAntesDeSalir);
        }

        // Si el jugador salteo, todo el cierre va rapido.
        float durUltimoANegro = salteando ? 0.2f : fadeUltimoFrameANegro;
        float durEnNegro      = salteando ? 0.1f : esperaEnNegro;
        float durVolver       = salteando ? 0.25f : fadeVolverAlJuego;

        // La musica se apaga mientras la imagen se va al negro.
        StartCoroutine(FadeMusica(0f, salteando ? 0.25f : fadeOutMusica, true));

        // 5) El ULTIMO frame se funde a NEGRO (el fondo negro sigue tapando el juego).
        salteando = false; // de aca en adelante el cierre no se puede cortar: siempre se ve completo
        yield return FadeImagen(actual, 0f, durUltimoANegro);

        // 6) Un respiro en negro.
        yield return EsperarReal(durEnNegro);

        // 7) Vuelve el JUEGO desde el negro. La niña ya esta quieta en idle: el tiempo sigue
        //    congelado durante este fundido y recien se descongela al final, en Restaurar().
        yield return FadeGrupo(0f, durVolver);

        Restaurar();

        reproduciendo = false;
        EnCurso = false;

        alTerminar.Invoke();
    }

    // ---------- Pasos ----------

    // Apaga los controles, esconde el HUD y congela el juego.
    private void Preparar()
    {
        FrenarYPonerEnIdle();

        if (ocultarDuranteCine != null)
        {
            foreach (GameObject go in ocultarDuranteCine)
            {
                if (go != null) go.SetActive(false);
            }
        }

        grupo.blocksRaycasts = true; // mientras dura, la cinematica tapa todo

        if (congelarTiempo)
        {
            timeScalePrevio = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    // La niña FRENA en seco y queda quieta en IDLE, sin responder a los controles.
    // Se hace ANTES de congelar el tiempo, asi la pose queda tomada.
    private void FrenarYPonerEnIdle()
    {
        if (playerController == null) return;

        // Apagar el PlayerController corta los inputs (su OnDisable desactiva los controles),
        // asi no queda ningun salto o ataque "apretado" esperando para salir al volver.
        playerController.enabled = false;

        // Frenar el cuerpo: sin esto seguiria de largo con la velocidad que traia.
        if (playerController.rb != null)
        {
            playerController.rb.linearVelocity = Vector2.zero;
            playerController.rb.angularVelocity = 0f;
        }

        if (playerController.animPlayer != null)
        {
            // Pasar a la animacion de parada.
            playerController.animPlayer.SetInteger(PARAM_ANIM, ANIM_IDLE);

            // Que el idle SIGA animando aunque el juego este congelado (timeScale 0).
            // Si no, la niña queda como una estatua en el fundido de vuelta al juego.
            playerController.animPlayer.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
    }

    // Deja todo como estaba antes de la cinematica.
    private void Restaurar()
    {
        if (congelarTiempo)
        {
            Time.timeScale = timeScalePrevio <= 0f ? 1f : timeScalePrevio;
        }

        if (playerController != null)
        {
            playerController.enabled = true;

            // El Animator vuelve a seguir el tiempo del juego.
            if (playerController.animPlayer != null)
            {
                playerController.animPlayer.updateMode = AnimatorUpdateMode.Normal;
            }
        }

        if (ocultarDuranteCine != null)
        {
            foreach (GameObject go in ocultarDuranteCine)
            {
                if (go != null) go.SetActive(true);
            }
        }

        if (grupo != null)
        {
            grupo.alpha = 0f;
            grupo.blocksRaycasts = false;
        }

        if (musicaSource != null)
        {
            musicaSource.Stop();
        }
    }

    // El degradado de una imagen a la otra: la nueva se dibuja ARRIBA y aparece de a poco.
    // Asi no hay un bajon de brillo en el medio (que es lo que pasa si una se va mientras la otra viene).
    private IEnumerator Mezclar(Image debajo, Image encima, float dur)
    {
        encima.transform.SetAsLastSibling();
        PonerAlpha(encima, 0f);

        float t = 0f;
        while (t < dur && !salteando)
        {
            t += Time.unscaledDeltaTime;
            PonerAlpha(encima, Mathf.Clamp01(t / dur));
            yield return null;
        }

        PonerAlpha(encima, 1f);
        PonerAlpha(debajo, 0f); // la vieja queda libre para el proximo frame
    }

    // Fundido de UNA imagen sola (para entrar desde el negro y para salir al negro).
    private IEnumerator FadeImagen(Image img, float objetivo, float dur)
    {
        float inicial = img.color.a;
        float t = 0f;
        while (t < dur && !salteando)
        {
            t += Time.unscaledDeltaTime;
            PonerAlpha(img, Mathf.Lerp(inicial, objetivo, t / dur));
            yield return null;
        }
        PonerAlpha(img, objetivo);
    }

    private IEnumerator FadeGrupo(float objetivo, float dur)
    {
        float inicial = grupo.alpha;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(inicial, objetivo, t / dur);
            yield return null;
        }
        grupo.alpha = objetivo;
    }

    // Espera en tiempo real, y corta al toque si el jugador saltea.
    private IEnumerator EsperarReal(float segundos)
    {
        float t = 0f;
        while (t < segundos && !salteando)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // ---------- Audio ----------

    private void ArrancarMusica()
    {
        if (musicaSource == null || musica == null) return;

        musicaSource.clip = musica;
        musicaSource.loop = loopMusica;
        musicaSource.volume = fadeInMusica > 0f ? 0f : volumenMusica;
        musicaSource.Play();

        if (fadeInMusica > 0f)
        {
            StartCoroutine(FadeMusica(volumenMusica, fadeInMusica, false));
        }
    }

    private IEnumerator FadeMusica(float objetivo, float dur, bool frenarAlFinal)
    {
        if (musicaSource == null) yield break;

        float inicial = musicaSource.volume;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            musicaSource.volume = Mathf.Lerp(inicial, objetivo, t / dur);
            yield return null;
        }
        musicaSource.volume = objetivo;

        if (frenarAlFinal)
        {
            musicaSource.Stop();
        }
    }

    private IEnumerator SonarConRetraso(FrameCinematica f)
    {
        if (f.retrasoSonido > 0f)
        {
            yield return EsperarReal(f.retrasoSonido);
        }

        // Si mientras esperaba el jugador salteo, el efecto ya no va.
        if (salteando || !reproduciendo) yield break;

        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(f.sonido, f.volumenSonido);
        }
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(f.sonido, f.volumenSonido);
        }
    }

    // ---------- Auxiliares ----------

    private void PonerAlpha(Image img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    // Si la escena se recarga (o alguien apaga el objeto) en el medio de la cinematica,
    // devolvemos el juego a la normalidad para no dejarlo congelado.
    private void OnDisable()
    {
        if (!reproduciendo) return;

        StopAllCoroutines();
        Restaurar();
        reproduciendo = false;
        EnCurso = false;
    }
}
