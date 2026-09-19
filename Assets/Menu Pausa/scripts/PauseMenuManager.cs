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

    // PANTALLA COMPLETA: en el navegador, ESC siempre saca de pantalla completa y el juego
    // ni se entera de la tecla. Por eso, si se pierde la pantalla completa, pausamos solos,
    // y al volver con "resume" la recuperamos (el navegador solo deja hacerlo con un clic).
    private bool estabaEnPantallaCompleta;
    private bool recuperarPantallaCompleta;

    private void Start()
    {
        Time.timeScale = 1f;
        estabaEnPantallaCompleta = Screen.fullScreen;

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

        bool perdioPantallaCompleta = estabaEnPantallaCompleta && !Screen.fullScreen;
        estabaEnPantallaCompleta = Screen.fullScreen;

        // Si el mapa esta abierto, ESC lo maneja MinimapUI. LastClosedFrame evita
        // que este mismo ESC abra la pausa inmediatamente despues de cerrarlo.
        if (minimapUI != null &&
            (minimapUI.IsOpen || minimapUI.LastClosedFrame == Time.frameCount))
        {
            return;
        }

        // Durante una cinematica, ESC tampoco abre la pausa (la cinematica ya congela el juego
        // y el menu de pausa le devolveria el timeScale a 1 en el medio).
        if (CinematicaFrames.EnCurso)
        {
            return;
        }

        if (perdioPantallaCompleta && !isPaused)
        {
            recuperarPantallaCompleta = true;
            PauseGame();
            return;
        }

        // ESC o P SOLO pausan o despausan. NO van al Main Menu. La P sirve en pantalla
        // completa, donde el ESC lo usa el navegador para salir.
        if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame)
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

        // Si la pausa vino de perder la pantalla completa, la recuperamos.
        if (recuperarPantallaCompleta)
        {
            recuperarPantallaCompleta = false;
            Screen.fullScreen = true;
            estabaEnPantallaCompleta = true;
        }
    }

    public void OpenMapFromPauseMenu()
    {
        // Cierra visualmente el menu de pausa.
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }

        isPaused = false;

        // El mapa guarda este estado y mantiene el juego pausado mientras esta abierto.
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
