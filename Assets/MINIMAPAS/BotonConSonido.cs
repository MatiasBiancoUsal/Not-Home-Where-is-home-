using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BotonConSonido : MonoBehaviour, IPointerEnterHandler
{
    [Header("Sonidos")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    [Header("Volumen")]
    [Range(0f, 1f)]
    [SerializeField] private float hoverVolume = 0.4f;

    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 0.7f;

    [Header("Delay opcional")]
    [SerializeField] private float extraDelay = 0.05f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHover();
    }

    public void PlayHover()
    {
        if (hoverSound != null)
        {
            AudioManager.Instance?.PlaySFX(hoverSound, hoverVolume);
        }
    }

    public void PlayClick()
    {
        if (clickSound != null)
        {
            AudioManager.Instance?.PlaySFX(clickSound, clickVolume);
        }
    }

    public void PlayClickAndRun(GameObject targetObject)
    {
        StartCoroutine(PlayClickDelay());
    }

    private IEnumerator PlayClickDelay()
    {
        PlayClick();

        if (extraDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(extraDelay);
        }
    }
}