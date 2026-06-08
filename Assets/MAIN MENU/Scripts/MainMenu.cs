using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("UI Audio")]
    [SerializeField] private AudioClip clickSound;

    [Header("Delay después del click")]
    [SerializeField] private float extraDelay = 0.05f;

    private bool isChangingScene = false;

    // BOTÓN PLAY
    public void PlayGame()
    {
        LoadSceneWithClick("Zona 1");
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

    private void LoadSceneWithClick(string sceneName)
    {
        if (isChangingScene) return;

        StartCoroutine(LoadSceneAfterClick(sceneName));
    }

    private IEnumerator LoadSceneAfterClick(string sceneName)
    {
        isChangingScene = true;

        float waitTime = PlayClickAndGetDuration();

        yield return new WaitForSecondsRealtime(waitTime + extraDelay);

        SceneManager.LoadScene(sceneName);
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