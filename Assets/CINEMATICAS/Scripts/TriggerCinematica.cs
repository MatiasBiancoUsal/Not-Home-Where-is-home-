using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  TRIGGER DE CINEMATICA
//  Va EN el objeto que marca la zona (por ejemplo el sprite de
//  "la niña encuentra el osito para luchar"), con un Collider2D en Is Trigger.
//  Cuando la niña entra, dispara la cinematica UNA sola vez.
// ============================================================
[RequireComponent(typeof(Collider2D))]
public class TriggerCinematica : MonoBehaviour
{
    [Header("Que cinematica dispara")]
    [Tooltip("Arrastrar aca el objeto que tiene el script CinematicaFrames.")]
    public CinematicaFrames cinematica;

    [Header("Quien la dispara")]
    public string tagJugador = "Player";

    [Header("Repeticion")]
    [Tooltip("Si esta activo, la cinematica se ve UNA sola vez: no se repite al cambiar de zona, ni al morir, ni al cerrar y volver a abrir el juego (queda guardada).")]
    public bool unaSolaVez = true;

    [Tooltip("Identificador de esta cinematica. Si lo dejas vacio, se arma solo con la escena + el nombre del objeto. Si le cambias el nombre al objeto, el jugador la vuelve a ver una vez.")]
    public string id = "";

    [Tooltip("Si esta activo, el objeto se apaga despues de disparar (util si el sprite era solo la marca de la zona).")]
    public bool apagarObjetoDespues = false;

    // Las cinematicas ya vistas. Es ESTATICO: sobrevive al cambio de zona y a la recarga
    // de la escena (asi no se repite al morir), y ademas se GUARDA EN DISCO, asi tampoco
    // se repite cuando el jugador cierra el juego y lo vuelve a abrir.
    private static HashSet<string> yaVistas = new HashSet<string>();

    // Al empezar la partida, leemos del disco cuales ya vio.
    //
    // Se hace a mano porque el proyecto tiene "Enter Play Mode Options" con Reload Domain
    // DESACTIVADO (Edit > Project Settings > Editor): eso acelera el Play, pero deja las
    // variables static con el valor de la sesion anterior en vez de arrancar limpias.
    //
    // SubsystemRegistration es lo primero que corre al arrancar, antes de cargar la escena,
    // tanto en el editor como en el juego compilado.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void CargarProgreso()
    {
        yaVistas = ProgresoJuego.CargarCinematicas();
    }

    private void Reset()
    {
        // Al agregar el script, dejamos el collider listo como trigger.
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(tagJugador)) return;

        if (cinematica == null)
        {
            Debug.LogWarning("TriggerCinematica: falta asignar la cinematica en el Inspector (" + name + ").");
            return;
        }

        // No arrancamos una cinematica arriba de otra.
        if (CinematicaFrames.EnCurso) return;

        string clave = Clave();
        if (unaSolaVez)
        {
            if (yaVistas.Contains(clave)) return;

            yaVistas.Add(clave);
            ProgresoJuego.GuardarCinematicas(yaVistas);
        }

        cinematica.Reproducir();

        if (apagarObjetoDespues)
        {
            gameObject.SetActive(false);
        }
    }

    // La clave con la que se recuerda esta cinematica: escena + nombre del objeto,
    // asi dos triggers con el mismo nombre en zonas distintas no se pisan.
    private string Clave()
    {
        if (!string.IsNullOrEmpty(id)) return id;
        return gameObject.scene.name + "@" + name;
    }

    // Para volver a verlas desde cero: olvida las vistas y borra el guardado.
    // La usa la tecla de testing del ScoreManager (F5).
    public static void OlvidarVistas()
    {
        yaVistas.Clear();
        ProgresoJuego.GuardarCinematicas(yaVistas);
    }
}
