using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsManager : MonoBehaviour
{
    // La cruz (X) de la escena Options usa este metodo para volver al menu principal.
    public void VolverAlMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
}
