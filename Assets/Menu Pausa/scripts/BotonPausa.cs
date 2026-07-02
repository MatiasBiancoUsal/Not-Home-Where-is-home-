using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Va EN la imagen "BotonMenuPausa" (canvas CANVATODO). Al pasar el mouse SUENA el hover, y al
// clickear SUENA el click y se ABRE el menu de pausa. ESC sigue abriendolo igual (PauseMenuManager).
// Usa los MISMOS 2 sonidos que el menu de inicio (asignalos en hoverSound / clickSound).
//
// Requisitos en el editor:
//  - La imagen con "Raycast Target" activado (viene asi por defecto en una Image).
//  - Un EventSystem en la escena (se crea solo junto con cualquier Canvas/UI).
public class BotonPausa : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Menu de pausa")]
    [Tooltip("El PauseMenuManager de la escena. Si lo dejas vacio, lo busca solo al iniciar.")]
    [SerializeField] private PauseMenuManager pauseMenu;

    [Header("Sonidos (los mismos del menu de inicio)")]
    [SerializeField] private AudioClip hoverSound;
    [Range(0f, 1f)] [SerializeField] private float hoverVolume = 0.4f;
    [SerializeField] private AudioClip clickSound;
    [Range(0f, 1f)] [SerializeField] private float clickVolume = 0.7f;

    [Header("Delay despues del click")]
    [Tooltip("Segundos (en tiempo REAL) que espera antes de abrir la pausa, para que se alcance a escuchar el click (como en el menu). Pausar NO corta el sonido, asi que igual se escucha completo; 0 = abre al instante.")]
    [SerializeField] private float extraDelay = 0.05f;

    private void Awake()
    {
        // Si no se asigno a mano (util cuando CANVATODO es un prefab), lo buscamos en la escena.
        if (pauseMenu == null) pauseMenu = FindFirstObjectByType<PauseMenuManager>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // sonido al pasar el mouse por encima (por el AudioManager, igual que el menu)
        if (hoverSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(hoverSound, hoverVolume);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // sonido del click
        if (clickSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clickSound, clickVolume);
        }

        // abrir la pausa (con o sin delay)
        if (extraDelay > 0f)
        {
            StartCoroutine(AbrirPausaConDelay());
        }
        else
        {
            AbrirPausa();
        }
    }

    private IEnumerator AbrirPausaConDelay()
    {
        // WaitForSecondsRealtime: independiente del timeScale (la pausa lo pone en 0).
        yield return new WaitForSecondsRealtime(extraDelay);
        AbrirPausa();
    }

    private void AbrirPausa()
    {
        if (pauseMenu != null) pauseMenu.PauseGame();
    }
}
