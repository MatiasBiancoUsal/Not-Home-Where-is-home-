using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

// UNICO sistema de audio del juego: musica + ambiente + efectos.
// Se crea solo al arrancar, sobrevive a los cambios de escena y NO reinicia la musica entre menus.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer y fuentes de audio")]
    public AudioMixer audioMixer;   // arrastrar el MainMixer
    public AudioSource musicSource; // la musica (loop)  -> Output: grupo Music
    public AudioSource sfxSource;   // los efectos        -> Output: grupo SFX

    [Header("Escenas donde suena la musica del menu")]
    public string[] menuScenes = { "Main Menu", "Options", "Controls", "Credits" };

    [Header("Musica por zona")]
    public AudioClip musicaMenu;
    public AudioClip musicaZona1;
    public AudioClip musicaZona2;
    public AudioClip musicaZona3;
    public AudioClip musicaZona4;
    public AudioClip musicaZona5;
    public AudioClip musicaZona6;

    // Un "compartimento" de ambiente: que escena, que sonido y a que volumen.
    [System.Serializable]
    public class AmbienteZona
    {
        [Tooltip("Nombre EXACTO de la escena, igual que en Build Settings.")]
        public string escena;
        [Tooltip("Sonido ambiente que suena en loop en esta zona (viento, goteo, cueva...). Se puede dejar vacio.")]
        public AudioClip clip;
        [Range(0f, 1f)]
        [Tooltip("Volumen del ambiente en esta zona. Se puede ajustar en Play y se escucha al toque.")]
        public float volumen = 0.5f;
    }

    [Header("Sonido ambiente por zona")]
    [Tooltip("Suena en loop por debajo de la musica. Una fila por zona.")]
    public AmbienteZona[] ambientes =
    {
        new AmbienteZona { escena = "Zona 1" },
        new AmbienteZona { escena = "Zona 2" },
        new AmbienteZona { escena = "Zona 3" },
        new AmbienteZona { escena = "Zona 4" },
        new AmbienteZona { escena = "Zona 5" },
        new AmbienteZona { escena = "Zona 6" },
    };
    [Tooltip("Grupo del mixer para el ambiente. Si lo dejas vacio usa el mismo que la musica, asi lo regula el slider de Musica.")]
    public AudioMixerGroup grupoAmbiente;
    [Min(0f)]
    [Tooltip("Segundos que tarda el ambiente en aparecer o apagarse al cambiar de zona. 0 = corte seco.")]
    public float fadeAmbiente = 1f;

    private AudioSource ambientSource;
    private AmbienteZona ambienteObjetivo; // el ambiente que tiene que sonar en la escena actual

    // Crea el AudioManager AUTOMATICAMENTE al arrancar el juego (en cualquier escena),
    // antes de que cargue la primera escena. Por eso NO hace falta ponerlo en ninguna escena.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoSpawn()
    {
        if (Instance == null)
        {
            GameObject prefab = Resources.Load<GameObject>("AudioManager");
            if (prefab != null)
            {
                Instantiate(prefab);
            }
        }
    }

    private void Awake()
    {
        // Singleton: si ya existe uno, este se destruye. Asi la musica NUNCA se reinicia.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CrearFuenteAmbiente();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ApplySavedVolumes();
        UpdateMusicForScene(SceneManager.GetActiveScene().name);
        UpdateAmbienteForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateMusicForScene(scene.name);
        UpdateAmbienteForScene(scene.name);
    }

    private void Update()
    {
        ActualizarAmbiente();
    }

    // Decide que musica debe sonar segun la escena actual.
    private void UpdateMusicForScene(string sceneName)
    {
        if (EsEscenaDeMenu(sceneName))
        {
            ReproducirMusica(musicaMenu);
        }
        else if (sceneName == "Zona 1")
        {
            ReproducirMusica(musicaZona1);
        }
        else if (sceneName == "Zona 2")
        {
            ReproducirMusica(musicaZona2);
        }
        else if (sceneName == "Zona 3")
        {
            ReproducirMusica(musicaZona3);
        }
        else if (sceneName == "Zona 4")
        {
            ReproducirMusica(musicaZona4);
        }
        else if (sceneName == "Zona 5")
        {
            ReproducirMusica(musicaZona5);
        }
        else if (sceneName == "Zona 6")
        {
            ReproducirMusica(musicaZona6);
        }
        else
        {
            musicSource.Stop();
        }
    }

    private void ReproducirMusica(AudioClip clip)
    {
        if (musicSource == null) return;

        // Si la zona todavia no tiene musica asignada, no dejamos sonando la
        // pista de la zona anterior.
        if (clip == null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            return;
        }

        // No reinicia el tema si otra escena usa el mismo clip.
        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    private bool EsEscenaDeMenu(string sceneName)
    {
        foreach (string escena in menuScenes)
        {
            if (escena == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    // ---------- Ambiente ----------

    // La fuente del ambiente se crea por codigo, asi no hay que tocar el prefab.
    private void CrearFuenteAmbiente()
    {
        GameObject go = new GameObject("AmbientSource");
        go.transform.SetParent(transform, false);

        ambientSource = go.AddComponent<AudioSource>();
        ambientSource.playOnAwake = false;
        ambientSource.loop = true;
        ambientSource.spatialBlend = 0f;
        ambientSource.volume = 0f;
        ambientSource.outputAudioMixerGroup = grupoAmbiente != null
            ? grupoAmbiente
            : (musicSource != null ? musicSource.outputAudioMixerGroup : null);
    }

    private void UpdateAmbienteForScene(string sceneName)
    {
        ambienteObjetivo = null;
        if (ambientes == null) return;

        foreach (AmbienteZona a in ambientes)
        {
            if (a != null && a.escena == sceneName)
            {
                ambienteObjetivo = a;
                return;
            }
        }
    }

    // Corre todos los frames: si cambio la zona, baja el ambiente viejo, cambia el clip
    // y sube el nuevo. Si no cambio, sigue el volumen que tenga el Inspector.
    private void ActualizarAmbiente()
    {
        if (ambientSource == null) return;

        AudioClip clipObjetivo = ambienteObjetivo != null ? ambienteObjetivo.clip : null;
        float volumenObjetivo = clipObjetivo != null ? ambienteObjetivo.volumen : 0f;

        // Tiempo sin escala: el fade sigue aunque el juego este en pausa.
        float paso = fadeAmbiente > 0f ? Time.unscaledDeltaTime / fadeAmbiente : 1f;

        if (ambientSource.clip != clipObjetivo)
        {
            // Primero apagamos lo que estaba sonando...
            ambientSource.volume = Mathf.MoveTowards(ambientSource.volume, 0f, paso);
            if (ambientSource.volume > 0f && ambientSource.isPlaying) return;

            // ...y recien ahi arrancamos el ambiente nuevo (desde volumen 0, sube solo).
            ambientSource.Stop();
            ambientSource.clip = clipObjetivo;
            if (clipObjetivo != null) ambientSource.Play();
            return;
        }

        ambientSource.volume = Mathf.MoveTowards(ambientSource.volume, volumenObjetivo, paso);
    }

    // Aplica al mixer el volumen guardado en PlayerPrefs (lo mismo que setean los sliders)
    private void ApplySavedVolumes()
    {
        if (audioMixer == null) return;

        float music = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfx   = PlayerPrefs.GetFloat("SFXVolume",   1f);

        audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(music, 0.0001f)) * 20f);
        audioMixer.SetFloat("SFXVolume",   Mathf.Log10(Mathf.Max(sfx,   0.0001f)) * 20f);
    }

    // Para reproducir un efecto desde cualquier script: AudioManager.Instance.PlaySFX(miClip)
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
