using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class ZoneSkipByKey : MonoBehaviour
{
    [Header("Escenas en orden")]
    [SerializeField] private string[] zones =
    {
        "Zona 1",
        "Zona 2",
        "Zona 3",
        "Zona 4",
        "Zona 5",
        "Zona 6"
    };

    [Header("Después de la última zona")]
    [SerializeField] private bool volverAlMenuAlFinal = true;
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    private void Update()
    {
        // Tecla N con el New Input System
        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
        {
            LoadNextZone();
        }
    }

    private void LoadNextZone()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        for (int i = 0; i < zones.Length; i++)
        {
            if (currentSceneName == zones[i])
            {
                int nextIndex = i + 1;

                if (nextIndex < zones.Length)
                {
                    SceneManager.LoadScene(zones[nextIndex]);
                }
                else
                {
                    if (volverAlMenuAlFinal)
                    {
                        SceneManager.LoadScene(mainMenuSceneName);
                    }
                    else
                    {
                        Debug.Log("Ya estás en la última zona.");
                    }
                }

                return;
            }
        }

        Debug.LogWarning("La escena actual no está en la lista de zonas: " + currentSceneName);
    }
}