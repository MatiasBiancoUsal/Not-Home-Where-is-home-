using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Sonidos")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    [Header("Volumen")]
    [Range(0f, 1f)]
    [SerializeField] private float hoverVolume = 0.4f;

    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 0.7f;

    // Para cargarle los sonidos desde codigo (lo usan los botones que se crean solos,
    // como el cartel de dificultad del boss final).
    public void Configurar(AudioClip hover, AudioClip click, float volumenHover = 0.4f, float volumenClick = 0.7f)
    {
        hoverSound = hover;
        clickSound = click;
        hoverVolume = volumenHover;
        clickVolume = volumenClick;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHoverSound();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClickSound();
    }

    private void PlayHoverSound()
    {
        // Ahora suena por el AudioManager (grupo SFX), no por un AudioSource suelto.
        if (hoverSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(hoverSound, hoverVolume);
        }
    }

    private void PlayClickSound()
    {
        if (clickSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clickSound, clickVolume);
        }
    }
}
