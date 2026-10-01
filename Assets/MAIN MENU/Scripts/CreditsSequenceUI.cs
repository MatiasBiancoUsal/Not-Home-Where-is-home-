using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("Pantalla de fuentes de audio")]
    [SerializeField] private string audioCreditsTitle = "MÚSICA Y EFECTOS DE SONIDO";
    [TextArea(4, 9)]
    [SerializeField] private string audioCreditsText =
        "Música original creada con Suno\n" +
        "Efectos de sonido: Pixabay y Unity Asset Store\n\n" +
        "Fuentes y enlaces:\n" +
        "[Agregar enlaces aquí]";
    [SerializeField] private float audioCreditsFadeInDuration = 0.7f;
    [SerializeField] private float audioCreditsVisibleDuration = 4f;
    [SerializeField] private float audioCreditsFadeOutDuration = 0.7f;

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
    private CanvasGroup audioCreditsGroup;

    private void Awake()
    {
        // Evita que los créditos queden pausados si venimos
        // de una escena que tenía Time.timeScale en 0.
        Time.timeScale = 1f;

        CreateAudioCreditsScreen();
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

        // PARTE INTERMEDIA:
        // Fuentes de musica y sonidos, antes de reunir todos los nombres.
        if (audioCreditsGroup != null)
        {
            yield return FadeCanvasGroup(
                audioCreditsGroup,
                0f,
                1f,
                audioCreditsFadeInDuration
            );

            yield return new WaitForSecondsRealtime(
                audioCreditsVisibleDuration
            );

            yield return FadeCanvasGroup(
                audioCreditsGroup,
                1f,
                0f,
                audioCreditsFadeOutDuration
            );
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

        if (audioCreditsGroup != null)
        {
            SetGroupHidden(audioCreditsGroup);
        }
    }

    private void CreateAudioCreditsScreen()
    {
        if (audioCreditsGroup != null || credits == null || credits.Length == 0) return;

        CanvasGroup referenceGroup = null;
        for (int i = 0; i < credits.Length && referenceGroup == null; i++)
            referenceGroup = credits[i].centerGroup;

        if (referenceGroup == null) return;

        Canvas canvas = referenceGroup.GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : referenceGroup.transform.parent;
        GameObject screen = new GameObject("Fuentes de audio", typeof(RectTransform), typeof(CanvasGroup));
        screen.layer = referenceGroup.gameObject.layer;

        RectTransform screenRect = screen.GetComponent<RectTransform>();
        screenRect.SetParent(parent, false);
        screenRect.anchorMin = Vector2.zero;
        screenRect.anchorMax = Vector2.one;
        screenRect.offsetMin = Vector2.zero;
        screenRect.offsetMax = Vector2.zero;
        audioCreditsGroup = screen.GetComponent<CanvasGroup>();

        TMP_Text referenceText = referenceGroup.GetComponentInChildren<TMP_Text>(true);

        TMP_Text title = CreateText(
            screen.transform,
            "Titulo",
            audioCreditsTitle,
            new Vector2(0f, 170f),
            new Vector2(1450f, 120f),
            58f,
            referenceText
        );
        title.fontStyle = FontStyles.Bold;

        TMP_Text body = CreateText(
            screen.transform,
            "Contenido editable",
            audioCreditsText,
            new Vector2(0f, -45f),
            new Vector2(1450f, 360f),
            31f,
            referenceText
        );
        body.lineSpacing = 12f;
    }

    private TMP_Text CreateText(
        Transform parent,
        string objectName,
        string content,
        Vector2 position,
        Vector2 size,
        float fontSize,
        TMP_Text referenceText)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (referenceText != null)
        {
            text.font = referenceText.font;
            text.color = referenceText.color;
        }
        else
        {
            text.color = Color.white;
        }

        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
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
