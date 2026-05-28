using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // BOTÓN PLAY
    public void PlayGame()
    {
        SceneManager.LoadScene("Zona 1");
    }

    // BOTÓN CREDITS
    public void Credits()
    {
        SceneManager.LoadScene("Credits");
    }

    // BOTÓN CONTROLS
    public void Controls()
    {
        SceneManager.LoadScene("Controls");
    }

    // BOTÓN EXIT
    public void ExitGame()
    {
        Application.Quit();

        // Esto sirve para probar en Unity
        Debug.Log("Salir del juego");
    }
}