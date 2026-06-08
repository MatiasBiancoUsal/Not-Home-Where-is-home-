using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class ZoneSkipByKey : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current == null) return;

        // Tecla N -> pasa a la SIGUIENTE escena. Despues de la ultima, vuelve a la primera (la 0).
        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            LoadNextScene();
        }

        // Tecla ESC -> vuelve directo al Main Menu. Sirve como "por las dudas" en cualquier escena.
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(0); // 0 = Main Menu (la primera en tu Build Settings)
        }
    }

    private void LoadNextScene()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;

        // si nos pasamos de la ultima escena, volvemos a la primera (escena 0 = Main Menu)
        // asi se puede dar toda la vuelta a todas las escenas del juego
        if (next >= SceneManager.sceneCountInBuildSettings)
        {
            next = 0;
        }

        SceneManager.LoadScene(next);
    }
}
