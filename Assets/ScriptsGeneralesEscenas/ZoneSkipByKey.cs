using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// ============================================================
//  ATAJO PARA PASAR DE ZONA (para mostrarle el juego al profesor)
//
//  SHIFT + N -> pasa a la siguiente escena. Despues de la ultima, vuelve a la primera.
//
//  Antes era la N sola, pero esta pegada a la M (el mapa) y en las pruebas los
//  jugadores se salteaban zonas sin querer: aparecian en la Zona 6 y sin las
//  habilidades de las flores que se habian salteado.
//
//  Al saltar a una zona, la niña recibe (y queda guardado) todo lo que tendria si
//  hubiera jugado las zonas anteriores: el ataque y las habilidades de sus flores.
// ============================================================
public class ZoneSkipByKey : MonoBehaviour
{
    private void Update()
    {
        Keyboard k = Keyboard.current;
        if (k == null) return;

        bool shift = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
        if (shift && k.nKey.wasPressedThisFrame)
        {
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;

        // Si nos pasamos de la última escena, volvemos a la primera.
        if (next >= SceneManager.sceneCountInBuildSettings)
        {
            next = 0;
        }

        string nombre = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(next));

        // Se guarda ANTES de cargar: la niña de la zona nueva lee su progreso en el Awake.
        DarLoDeLasZonasAnteriores(nombre);

        SceneManager.LoadScene(next);

        // Aunque sea un atajo, que aparezca el nombre de la zona igual que al cruzar una puerta.
        // TransicionZonas sobrevive al cambio de escena, asi que el cartel sigue andando.
        if (TransicionZonas.Instancia != null)
        {
            TransicionZonas.Instancia.MostrarNombreDeLaZona(nombre);
        }
    }

    // En la Zona N ya se tendrian que haber agarrado las flores de las zonas 1 a N-1
    // y haber visto la cinematica del osito (que da el ataque).
    private void DarLoDeLasZonasAnteriores(string nombreEscena)
    {
        int zona = DesbloquearHabilidadesPorZona.NumeroDeZona(nombreEscena);
        if (zona <= 1) return;

        PlayerController.Habilidad[] orden = DesbloquearHabilidadesPorZona.ORDEN_POR_DEFECTO;
        int cantidad = Mathf.Min(zona - 1, orden.Length);

        for (int i = 0; i < cantidad; i++)
        {
            ProgresoJuego.MarcarMostrado("Habilidad_" + orden[i]);
        }

        PlayerAttacks ataque = Object.FindFirstObjectByType<PlayerAttacks>();
        ProgresoJuego.MarcarMostrado(ataque != null ? ataque.claveDesbloqueo : "HabilidadAtaque");
    }
}
