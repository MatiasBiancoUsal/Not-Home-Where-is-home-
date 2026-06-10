using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class ZoneSkipByKey : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current == null) return;

        // Tecla N -> pasa a la siguiente escena.
        // Después de la última, vuelve a la primera escena del Build Settings.
        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;

        // Si nos pasamos de la última escena, volvemos a la primera.
        if (next >= SceneManager.sceneCountInBuildSettings)
        {
            next = 0;
        }

        SceneManager.LoadScene(next);
    }
}