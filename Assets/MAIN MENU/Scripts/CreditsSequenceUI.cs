using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsSequenceUI : MonoBehaviour
{
    [System.Serializable]
    public class CreditEntry
    {
        [Tooltip("Crédito grande que aparece individualmente en el centro.")]
        public CanvasGroup centerGroup;

        [Tooltip("Versión pequeña del mismo crédito dentro de la lista final.")]
        public CanvasGroup finalGroup;
    }

    [Header("Créditos en orden")]
    [SerializeField] private CreditEntry[] credits;

    [Header("Primera parte: créditos individuales")]
    [SerializeField] private float centerFadeInDuration = 0.8f;
    [SerializeField] private float centerVisibleDuration = 1.8f;
    [SerializeField] private float centerFadeOutDuration = 0.8f;
    [SerializeField] private float delayBetweenCenterCredits = 0.25f;

    [Header("Espera antes de mostrar la lista")]
    [SerializeField] private float delayBeforeFinalList = 0.6f;

    [Header("Segunda parte: lista final")]
    [SerializeField] private float finalFadeInDuration = 0.5f;
    [SerializeField] private float delayBetweenFinalCredits = 0.2f;

    [Header("Botón para volver")]
    [SerializeField] private CanvasGroup backButtonGroup;
    [SerializeField] private float delayBeforeBackButton = 0.5f;
    [SerializeField] private float backButtonFadeDuration = 0.5f;

    [Header("Escena del menú principal")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    private bool sequenceFinished;
    private Coroutine sequenceCoroutine;

    private void Awake()
    {
        // Evita que los créditos queden pausados si venimos
        // de una escena que tenía Time.timeScale en 0.
        Time.timeScale = 1f;

        ResetSequence();
    }

    private void Start()
    {
        sequenceCoroutine = StartCoroutine(PlayCreditsSequence());
    }

    private IEnumerator PlayCreditsSequence()
    {
        sequenceFinished = false;

        // PARTE 1:
        // Aparecen individualmente en el centro.
        for (int i = 0; i < credits.Length; i++)
        {
            CanvasGroup centerCredit = credits[i].centerGroup;

            if (centerCredit == null)
            {
                continue;
            }

            // Fade in.
            yield return FadeCanvasGroup(
                centerCredit,
                0f,
                1f,
                centerFadeInDuration
            );

            // Permanece visible.
            yield return new WaitForSecondsRealtime(
                centerVisibleDuration
            );

            // Fade out.
            yield return FadeCanvasGroup(
                centerCredit,
                1f,
                0f,
                centerFadeOutDuration
            );

            // Espera antes del siguiente.
            if (delayBetweenCenterCredits > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    delayBetweenCenterCredits
                );
            }
        }

        // Pausa antes de mostrar la lista completa.
        if (delayBeforeFinalList > 0f)
        {
            yield return new WaitForSecondsRealtime(
                delayBeforeFinalList
            );
        }

        // PARTE 2:
        // Aparecen de arriba hacia abajo y quedan visibles.
        for (int i = 0; i < credits.Length; i++)
        {
            CanvasGroup finalCredit = credits[i].finalGroup;

            if (finalCredit == null)
            {
                continue;
            }

            yield return FadeCanvasGroup(
                finalCredit,
                0f,
                1f,
                finalFadeInDuration
            );

            if (delayBetweenFinalCredits > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    delayBetweenFinalCredits
                );
            }
        }

        // Espera antes de mostrar el botón.
        if (delayBeforeBackButton > 0f)
        {
            yield return new WaitForSecondsRealtime(
                delayBeforeBackButton
            );
        }

        // El botón aparece recién cuando toda la lista está visible.
        if (backButtonGroup != null)
        {
            yield return FadeCanvasGroup(
                backButtonGroup,
                0f,
                1f,
                backButtonFadeDuration
            );

            backButtonGroup.interactable = true;
            backButtonGroup.blocksRaycasts = true;
        }

        sequenceFinished = true;
        sequenceCoroutine = null;
    }

    public void GoToMainMenu()
    {
        // Impide volver antes de que termine toda la secuencia.
        if (!sequenceFinished)
        {
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ReplayCredits()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
        }

        ResetSequence();
        sequenceCoroutine = StartCoroutine(PlayCreditsSequence());
    }

    private void ResetSequence()
    {
        sequenceFinished = false;

        for (int i = 0; i < credits.Length; i++)
        {
            if (credits[i].centerGroup != null)
            {
                SetGroupHidden(credits[i].centerGroup);
            }

            if (credits[i].finalGroup != null)
            {
                SetGroupHidden(credits[i].finalGroup);
            }
        }

        if (backButtonGroup != null)
        {
            SetGroupHidden(backButtonGroup);
        }
    }

    private void SetGroupHidden(CanvasGroup group)
    {
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private IEnumerator FadeCanvasGroup(
        CanvasGroup group,
        float startAlpha,
        float endAlpha,
        float duration
    )
    {
        if (group == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            group.alpha = endAlpha;
            yield break;
        }

        float elapsedTime = 0f;
        group.alpha = startAlpha;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / duration
            );

            float smoothProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            group.alpha = Mathf.Lerp(
                startAlpha,
                endAlpha,
                smoothProgress
            );

            yield return null;
        }

        group.alpha = endAlpha;
    }
}