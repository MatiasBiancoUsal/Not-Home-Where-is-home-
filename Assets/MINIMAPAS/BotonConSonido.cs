using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(AudioSource))]
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

    private AudioSource audioSource;
    private Button button;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        button = GetComponent<Button>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHover();
    }

    public void PlayHover()
    {
        if (hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound, hoverVolume);
        }
    }

    public void PlayClick()
    {
        if (clickSound != null)
        {
            audioSource.PlayOneShot(clickSound, clickVolume);
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