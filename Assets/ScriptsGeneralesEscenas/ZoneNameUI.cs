using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ZoneNameUI : MonoBehaviour
{
    public static ZoneNameUI Instance { get; private set; }

    [SerializeField] private Image zoneImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Sprite spriteDeEstaZona; // el PNG de la zona actual
    [SerializeField] private float fadeInTime = 0.4f;
    [SerializeField] private float holdTime = 1.5f;
    [SerializeField] private float fadeOutTime = 0.6f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        Instance = this;
        canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        // Al arrancar la escena, muestra automáticamente el cartel de esta zona
        if (spriteDeEstaZona != null)
        {
            ShowZoneImage(spriteDeEstaZona);
        }
    }

    public void ShowZoneImage(Sprite sprite)
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(ShowRoutine(sprite));
    }

    private IEnumerator ShowRoutine(Sprite sprite)
    {
        zoneImage.sprite = sprite;

        yield return Fade(0f, 1f, fadeInTime);
        yield return new WaitForSeconds(holdTime);
        yield return Fade(1f, 0f, fadeOutTime);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}