using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MenuMusicManager : MonoBehaviour
{
    public static MenuMusicManager Instance;

    [Header("Escenas donde la música del menú debe seguir")]
    [SerializeField] private string[] menuScenes =
    {
        "Main Menu",
        "Options",
        "Controls",
        "Credits"
    };

    private AudioSource audioSource;

    private void Awake()
    {
        // Evita que se duplique la música si volvés al Main Menu
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.spatialBlend = 0f; // 2D
    }

    private void Start()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }

        CheckIfShouldKeepMusic(SceneManager.GetActiveScene().name);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckIfShouldKeepMusic(scene.name);
    }

    private void CheckIfShouldKeepMusic(string sceneName)
    {
        if (!IsMenuScene(sceneName))
        {
            Destroy(gameObject);
        }
    }

    private bool IsMenuScene(string sceneName)
    {
        foreach (string menuScene in menuScenes)
        {
            if (sceneName == menuScene)
            {
                return true;
            }
        }

        return false;
    }
}