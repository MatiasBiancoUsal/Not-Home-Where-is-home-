using UnityEngine;

public class MinimapUI : MonoBehaviour
{
    [Header("Panel del minimapa")]
    [SerializeField] private GameObject minimapPanel;

    [Header("Opcional")]
    [SerializeField] private bool pauseGameWhileMapIsOpen = false;

    private bool isOpen = false;

    // Esto lo usa PauseMenuManager para saber si el mapa esta abierto.
    public bool IsOpen => isOpen;

    private void Start()
    {
        CloseMinimap();
    }

    public void ToggleMinimap()
    {
        if (isOpen)
        {
            CloseMinimap();
        }
        else
        {
            OpenMinimap();
        }
    }

    public void OpenMinimap()
    {
        isOpen = true;

        if (minimapPanel != null)
        {
            minimapPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("MinimapUI: falta asignar el Panel_Minimapa en el Inspector.");
        }

        if (pauseGameWhileMapIsOpen)
        {
            Time.timeScale = 0f;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseMinimap()
    {
        isOpen = false;

        if (minimapPanel != null)
        {
            minimapPanel.SetActive(false);
        }

        if (pauseGameWhileMapIsOpen)
        {
            Time.timeScale = 1f;
        }
    }
}