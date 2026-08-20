#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// ============================================================
//  MENU "Not Home" (solo en el editor, NO va al juego compilado)
//  Agrega comandos arriba de todo, en la barra de menus de Unity, para poder
//  probar el juego como si fuera la primera vez que alguien lo abre.
//
//  Este archivo esta dentro de una carpeta llamada "Editor": Unity la excluye sola
//  del build. Igual va con #if UNITY_EDITOR por las dudas.
// ============================================================
public static class MenuProgresoJuego
{
    [MenuItem("Not Home/Borrar progreso guardado", false, 1)]
    private static void BorrarProgreso()
    {
        // Limpia el disco Y la memoria: puntaje, monedas, cinematicas vistas,
        // la zona guardada y los carteles que ya se mostraron (el del tutorial).
        ScoreManager.NuevaPartida();

        Debug.Log("Progreso borrado. El juego arranca como si fuera la primera vez: " +
                  "vuelve el cartel del tutorial, las cinematicas, las monedas, y el boton 'continue' desaparece.");
    }

    [MenuItem("Not Home/Ver progreso guardado", false, 2)]
    private static void VerProgreso()
    {
        if (!ProgresoJuego.HayProgreso())
        {
            Debug.Log("No hay progreso guardado: el juego arranca de cero.");
            return;
        }

        string puntajesPorZona = "";
        for (int numeroZona = 1; numeroZona <= ProgresoJuego.CANTIDAD_ZONAS; numeroZona++)
        {
            string nombreZona = "Zona " + numeroZona;
            puntajesPorZona += "\n    " + nombreZona + ": " + ProgresoJuego.CargarPuntaje(nombreZona);
        }

        Debug.Log("PROGRESO GUARDADO" +
                  "\n  Ultima zona: " + ProgresoJuego.CargarZona() +
                  "\n  Puntajes por zona:" + puntajesPorZona +
                  "\n  Monedas agarradas: " + ProgresoJuego.CargarMonedas().Count +
                  "\n  Cinematicas vistas: " + ProgresoJuego.CargarCinematicas().Count);
    }
}
#endif
