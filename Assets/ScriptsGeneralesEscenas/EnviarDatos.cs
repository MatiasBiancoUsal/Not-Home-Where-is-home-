using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnviarDatos : MonoBehaviour
{
    public float tiempoTotal;

    public void EnviarDatosZona(int vidas, int municion)
    {
        Scene escenaActual = SceneManager.GetActiveScene();

        CustomEvent entrarZona = new CustomEvent("EntrarZona")
        {
            { "NombreNivel", escenaActual.buildIndex },
            { "VidasJugador", vidas },
            { "MunicionJugador", municion }
        };

        AnalyticsService.Instance.RecordEvent(entrarZona);
    }

    public void TerminarJuegoAnalytics(string nombrePersonaje)
    {
        CustomEvent terminarJuego = new CustomEvent("JuegoTerminado")
        {
            { "NombrePersonaje", nombrePersonaje },
            { "TiempoTotal", tiempoTotal }
        };

        AnalyticsService.Instance.RecordEvent(terminarJuego);
    }
}
