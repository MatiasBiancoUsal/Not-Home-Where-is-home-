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

    [Header("Pantalla completa")]
    [Tooltip("Activo: si el juego pierde la pantalla completa (por ejemplo porque el jugador apreto " +
             "ESC en itch.io) se abre la pausa sola, y al darle resume vuelve a pantalla completa.\n\n" +
             "Desactivo (recomendado): salir de pantalla completa no abre nada. La pausa es SOLO con la P.")]
    [SerializeField] private bool pausarAlSalirDePantallaCompleta = false;

    // En el navegador, ESC siempre saca de pantalla completa y el juego
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

        // Si el mapa se acaba de cerrar en este mismo frame, no abrimos la pausa de rebote.
        if (minimapUI != null && minimapUI.LastClosedFrame == Time.frameCount)
        {
            return;
        }

        // Durante una cinematica no se puede pausar: la cinematica ya congela el juego
        // y el menu de pausa le devolveria el timeScale a 1 en el medio.
        if (CinematicaFrames.EnCurso)
        {
            return;
        }

        // Solo si esta activado en el Inspector: perder la pantalla completa abre la pausa.
        if (pausarAlSalirDePantallaCompleta && perdioPantallaCompleta && !isPaused)
        {
            recuperarPantallaCompleta = true;
            PauseGame();
            return;
        }

        // La P es la UNICA tecla que pausa o despausa. NO va al Main Menu.
        //
        // ESC a proposito NO pausa: el juego se publica en itch.io y ahi el navegador usa
        // ESC para salir de pantalla completa. Si ESC tambien pausara, cada vez que el
        // jugador saliera de pantalla completa se le abriria el menu encima.
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            // Si el mapa esta abierto, la P lo cierra primero: asi el menu de pausa no
            // aparece encima del mapa.
            if (!isPaused && minimapUI != null && minimapUI.IsOpen)
            {
                minimapUI.CloseMinimap();
                return;
            }

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
