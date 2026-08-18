using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("UI Audio")]
    [SerializeField] private AudioClip clickSound;

    [Header("Delay después del click")]
    [SerializeField] private float extraDelay = 0.05f;

    [Header("Botón CONTINUE")]
    [Tooltip("El botón 'continue'. Se esconde solo si el jugador todavía no empezó ninguna partida.")]
    [SerializeField] private GameObject botonContinue;

    private bool isChangingScene = false;

    private void Start()
    {
        // Si nunca jugó, no tiene sentido mostrarle "continue".
        if (botonContinue != null)
        {
            botonContinue.SetActive(ProgresoJuego.HayProgreso());
        }
    }

    // BOTÓN PLAY (el de siempre: arranca en la Zona 1 sin tocar el progreso)
    public void PlayGame()
    {
        LoadSceneWithClick(ProgresoJuego.ZONA_INICIAL, true);
    }

    // BOTÓN CONTINUE: vuelve a la zona donde había quedado, con sus monedas y cinemáticas.
    public void ContinueGame()
    {
        LoadSceneWithClick(ProgresoJuego.CargarZona(), true);
    }

    // BOTÓN NEW GAME: borra el progreso guardado y empieza de cero en la Zona 1.
    public void NewGame()
    {
        ScoreManager.NuevaPartida();
        LoadSceneWithClick(ProgresoJuego.ZONA_INICIAL, true);
    }

    // TESTING: borra el progreso sin tener que jugar nada.
    // Clic derecho en el título del componente "Main Menu" (en el Inspector) y elegir esta opción.
    // Es lo mismo que el menú de arriba: Not Home > Borrar progreso guardado.
    [ContextMenu("Borrar progreso guardado")]
    public void BorrarProgresoGuardado()
    {
        ScoreManager.NuevaPartida();

        // Si estamos en Play, escondemos el botón continue al toque.
        if (botonContinue != null)
        {
            botonContinue.SetActive(ProgresoJuego.HayProgreso());
        }

        Debug.Log("Progreso borrado: el juego arranca como si fuera la primera vez.");
    }

    // BOTÓN OPTIONS
    public void Options()
    {
        LoadSceneWithClick("Options");
    }

    // BOTÓN CREDITS
    public void Credits()
    {
        LoadSceneWithClick("Credits");
    }

    // BOTÓN CONTROLS
    public void Controls()
    {
        LoadSceneWithClick("Controls");
    }

    // BOTÓN EXIT
    public void ExitGame()
    {
        StartCoroutine(QuitWithClick());
    }

    private void LoadSceneWithClick(string sceneName, bool usarFundido = false)
    {
        if (isChangingScene) return;

        StartCoroutine(LoadSceneAfterClick(sceneName, usarFundido));
    }

    private IEnumerator LoadSceneAfterClick(string sceneName, bool usarFundido)
    {
        isChangingScene = true;

        float waitTime = PlayClickAndGetDuration();

        yield return new WaitForSecondsRealtime(waitTime + extraDelay);

        if (usarFundido && TransicionZonas.Instancia != null)
        {
            TransicionZonas.Instancia.CargarEscenaDesdeMenu(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private IEnumerator QuitWithClick()
    {
        if (isChangingScene) yield break;

        isChangingScene = true;

        float waitTime = PlayClickAndGetDuration();

        yield return new WaitForSecondsRealtime(waitTime + extraDelay);

        Application.Quit();

        // Esto sirve para probar en Unity
        Debug.Log("Salir del juego");
    }

    private float PlayClickAndGetDuration()
    {
        // El click suena por el AudioManager (grupo SFX), no por un AudioSource suelto.
        if (clickSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clickSound);
            return clickSound.length;
        }

        return 0.15f;
    }
}
