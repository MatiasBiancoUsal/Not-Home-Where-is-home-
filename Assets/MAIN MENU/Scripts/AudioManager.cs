using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

// UNICO sistema de audio del juego: musica + efectos.
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
        ApplySavedVolumes(); // aplica el volumen guardado apenas arranca (sin abrir Opciones)
        UpdateMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateMusicForScene(scene.name);
    }

    // Decide si la musica del menu debe sonar segun la escena actual
    private void UpdateMusicForScene(string sceneName)
    {
        if (EsEscenaDeMenu(sceneName))
        {
            // Si NO esta sonando, la arrancamos. Si YA esta sonando, la dejamos -> NO se reinicia.
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }
        else
        {
            // En gameplay paramos la musica del menu
            musicSource.Stop();
        }
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

    // Aplica al mixer el volumen guardado en PlayerPrefs (lo mismo que setean los sliders)
    private void ApplySavedVolumes()
    {
        if (audioMixer == null) return;

        float music = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 1f);

        audioMixer.SetFloat("MusicVolume", Mathf.Log10(music) * 20f);
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(sfx) * 20f);
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
