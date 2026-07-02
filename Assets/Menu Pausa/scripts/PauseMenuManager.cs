using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Canvas de pausa")]
    [SerializeField] private GameObject pauseMenuCanvas;

    [Header("Transicion de apertura (opcional)")]
    [Tooltip("Componente PauseMenuTransition del panel. Si lo dejas vacio, la pausa abre sin animacion.")]
    [SerializeField] private PauseMenuTransition transition;

    [Header("Minimapa")]
    [Tooltip("Arrastrar aca el objeto que tiene el script MinimapUI.")]
    [SerializeField] private MinimapUI minimapUI;

    [Header("Nombre exacto de la escena Main Menu")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    private bool isPaused = false;

    private void Start()
    {
        Time.timeScale = 1f;

        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        // Si el minimapa esta abierto, ESC no abre/cierra el menu de pausa.
        // El minimapa se cierra con su propia X.
        if (minimapUI != null && minimapUI.IsOpen)
        {
            return;
        }

        // ESC SOLO pausa o despausa. NO va al Main Menu.
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;

        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(true);
        }

        // Animacion de apertura del menu de pausa.
        if (transition != null)
        {
            transition.PlayOpen();
        }

        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResumeGame()
    {
        isPaused = false;

        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void OpenMapFromPauseMenu()
    {
        // Cierra visualmente el menu de pausa.
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }

        isPaused = false;

        // Volvemos el tiempo a 1 antes de abrir el minimapa.
        // Si el minimapa tiene "Pause Game While Map Is Open" activado,
        // el propio MinimapUI lo vuelve a pausar.
        Time.timeScale = 1f;

        if (minimapUI != null)
        {
            minimapUI.OpenMinimap();
        }
        else
        {
            Debug.LogWarning("PauseMenuManager: falta asignar MinimapUI en el Inspector.");
        }
    }

    // Esta funcion SOLO se usa si la conectas al boton Main Menu.
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}