using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Poner este script en la "casita" para volver al menu principal.
// Funciona de 3 formas (con cualquiera alcanza):
//   - Si la casita es UI (Image con "Raycast Target") -> al clickearla.
//   - Si la casita es un sprite del mundo (con un Collider2D) -> al clickearla.
//   - O conectando un boton: OnClick -> GoToMainMenu.
public class BackToMenu : MonoBehaviour, IPointerClickHandler
{
    // click sobre una Image de UI
    public void OnPointerClick(PointerEventData eventData)
    {
        GoToMainMenu();
    }

    // click sobre un sprite del mundo que tenga un Collider2D
    private void OnMouseDown()
    {
        GoToMainMenu();
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
}
