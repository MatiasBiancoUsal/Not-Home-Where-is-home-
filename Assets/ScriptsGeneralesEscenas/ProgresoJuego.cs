using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
//  PROGRESO DEL JUEGO (guardado en disco)
//  Guarda lo que el jugador ya logro, para que siga estando cuando CIERRA el juego
//  y lo vuelve a abrir:
//    - el puntaje
//    - las monedas que ya agarro
//    - las cinematicas que ya vio
//
//  Usa PlayerPrefs, igual que los volumenes de Opciones. Anda en PC y en WebGL/itch.io.
//  No es un MonoBehaviour: no va en ningun objeto de la escena, se llama directo
//  desde los scripts (ProgresoJuego.GuardarPuntaje(...), etc).
// ============================================================
public static class ProgresoJuego
{
    private const string KEY_PUNTAJE     = "Progreso_Puntaje";
    private const string KEY_MONEDAS     = "Progreso_Monedas";
    private const string KEY_CINEMATICAS = "Progreso_Cinematicas";
    private const string KEY_ZONA        = "Progreso_Zona";
    private const string KEY_MARCAS      = "Progreso_Marcas";

    // Con que zona arranca una partida nueva.
    public const string ZONA_INICIAL = "Zona 1";

    // Con esto separamos los ids dentro de un mismo texto guardado.
    // No puede aparecer en un id (los ids son "escena@x,y" o el nombre del objeto).
    private const char SEPARADOR = '|';

    // ---------- Puntaje ----------

    public static int CargarPuntaje()
    {
        return PlayerPrefs.GetInt(KEY_PUNTAJE, 0);
    }

    public static void GuardarPuntaje(int puntaje)
    {
        PlayerPrefs.SetInt(KEY_PUNTAJE, puntaje);
        PlayerPrefs.Save();
    }

    // ---------- Ultima zona jugada ----------

    // La zona se anota SOLA: al arrancar el juego nos enganchamos al aviso de "se cargo una
    // escena" y cada vez que carga una que se llama "Zona ...", la guardamos.
    // Gracias a esto no hay que poner ningun objeto ni script adentro de las zonas.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void EngancharGuardadoDeZona()
    {
        SceneManager.sceneLoaded -= AlCargarEscena; // por las dudas, para no engancharlo dos veces
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (EsZona(escena.name))
        {
            GuardarZona(escena.name);
        }
    }

    private static bool EsZona(string nombreEscena)
    {
        return !string.IsNullOrEmpty(nombreEscena) && nombreEscena.StartsWith("Zona");
    }

    // Guardamos el NOMBRE de la escena y no su numero, asi sigue funcionando aunque
    // algun dia cambie el orden de las escenas en el Build Settings.
    public static void GuardarZona(string nombreEscena)
    {
        PlayerPrefs.SetString(KEY_ZONA, nombreEscena);
        PlayerPrefs.Save();
    }

    // La zona donde quedo el jugador. Si nunca jugo, la primera.
    public static string CargarZona()
    {
        return PlayerPrefs.GetString(KEY_ZONA, ZONA_INICIAL);
    }

    // ---------- Monedas ----------

    public static HashSet<string> CargarMonedas()
    {
        return CargarLista(KEY_MONEDAS);
    }

    public static void GuardarMonedas(HashSet<string> ids)
    {
        GuardarLista(KEY_MONEDAS, ids);
    }

    // ---------- Cinematicas ----------

    public static HashSet<string> CargarCinematicas()
    {
        return CargarLista(KEY_CINEMATICAS);
    }

    public static void GuardarCinematicas(HashSet<string> ids)
    {
        GuardarLista(KEY_CINEMATICAS, ids);
    }

    // ---------- Marcas de "esto ya se mostro una vez" ----------

    // Para las cosas que se muestran UNA sola vez en toda la partida: el cartel del
    // tutorial, un aviso, lo que sea. Cada una con su propia clave (un texto cualquiera,
    // por ejemplo "TutorialMovimiento").

    public static bool YaMostrado(string clave)
    {
        if (string.IsNullOrEmpty(clave)) return false;
        return CargarLista(KEY_MARCAS).Contains(clave);
    }

    public static void MarcarMostrado(string clave)
    {
        if (string.IsNullOrEmpty(clave)) return;

        HashSet<string> marcas = CargarLista(KEY_MARCAS);
        if (marcas.Add(clave)) // si ya estaba, no hace falta volver a guardar
        {
            GuardarLista(KEY_MARCAS, marcas);
        }
    }

    // ---------- Borrar todo ----------

    // Partida NUEVA: se olvida el puntaje, las monedas y las cinematicas.
    // Conectarlo a un boton de "Nueva partida" o de "Borrar progreso".
    public static void BorrarTodo()
    {
        PlayerPrefs.DeleteKey(KEY_PUNTAJE);
        PlayerPrefs.DeleteKey(KEY_MONEDAS);
        PlayerPrefs.DeleteKey(KEY_CINEMATICAS);
        PlayerPrefs.DeleteKey(KEY_ZONA);
        PlayerPrefs.DeleteKey(KEY_MARCAS);
        PlayerPrefs.Save();
    }

    // ¿Hay una partida empezada? La usa el boton "continue" para saber si mostrarse o no.
    public static bool HayProgreso()
    {
        return PlayerPrefs.HasKey(KEY_ZONA) || PlayerPrefs.HasKey(KEY_PUNTAJE) || PlayerPrefs.HasKey(KEY_MONEDAS);
    }

    // ---------- Auxiliares ----------

    // Los ids se guardan como un solo texto: "id1|id2|id3".
    private static HashSet<string> CargarLista(string key)
    {
        HashSet<string> set = new HashSet<string>();

        string guardado = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(guardado)) return set;

        string[] partes = guardado.Split(SEPARADOR);
        foreach (string p in partes)
        {
            if (!string.IsNullOrEmpty(p)) set.Add(p);
        }

        return set;
    }

    private static void GuardarLista(string key, HashSet<string> ids)
    {
        if (ids == null || ids.Count == 0)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            return;
        }

        string[] array = new string[ids.Count];
        ids.CopyTo(array);

        PlayerPrefs.SetString(key, string.Join(SEPARADOR.ToString(), array));
        PlayerPrefs.Save();
    }
}
