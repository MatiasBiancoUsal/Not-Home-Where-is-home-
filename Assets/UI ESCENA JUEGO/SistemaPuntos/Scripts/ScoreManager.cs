using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // El puntaje pertenece solamente a la zona activa. Cada vez que se entra a una
    // escena se carga el valor guardado para esa zona, sin mezclarlo con las demas.
    private int currentScore = 0;
    private string zonaActual;

    // Las monedas pueden mantenerse en una sola lista porque su ID ya incluye el
    // nombre de la escena (por ejemplo "Zona 3@12.0,5.0").
    private static HashSet<string> recolectadas = new HashSet<string>();

    // La lista de monedas se lee del disco una sola vez por partida.
    private static bool monedasCargadas = false;

    // El proyecto tiene "Enter Play Mode Options" con Reload Domain DESACTIVADO
    // (Edit > Project Settings > Editor): eso hace que el Play arranque rapido, pero deja
    // las variables static con el valor de la sesion anterior. Por eso las reiniciamos
    // a mano al empezar cada partida: asi se vuelven a leer del disco de cero.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarStatics()
    {
        Instance = null;
        recolectadas = new HashSet<string>();
        monedasCargadas = false;
    }

    public int CurrentScore => currentScore;

    // Se calcula en Awake sumando el valor real de todas las monedas de la zona. De
    // esta manera contempla automaticamente monedas de 1, 10, 50 o cualquier otro
    // valor que se configure en el Inspector.
    [Header("Puntaje Total de Monedas del Nivel (automatico)")]
    [Tooltip("Se calcula solo al cargar la zona, antes de que desaparezcan las monedas ya recolectadas.")]
    public int puntajeTotal = 0;

    public int PuntajeTotal => puntajeTotal;

    public event Action<int> OnScoreChanged;

    [Header("Testing (solo para probar en el editor)")]
    [Tooltip("Apretá esta tecla en Play para REGENERAR las monedas: limpia las recolectadas, resetea el puntaje y recarga la zona.")]
    public Key teclaRegenerar = Key.F5;

    private void Awake()
    {
        // Cada zona tiene su propio ScoreManager dentro del CANVATODO.
        Instance = this;
        zonaActual = SceneManager.GetActiveScene().name;

        // Todos los Awake corren antes que los Start. Las monedas guardadas todavia
        // existen en este momento, asi que el total incluye tambien las ya recogidas.
        CalcularPuntajeTotalDeMonedas();

        // El puntaje se carga siempre para la zona que acaba de entrar.
        currentScore = ProgresoJuego.CargarPuntaje(zonaActual);

        if (!monedasCargadas)
        {
            recolectadas = ProgresoJuego.CargarMonedas();
            monedasCargadas = true;
        }
    }

    private void CalcularPuntajeTotalDeMonedas()
    {
        int total = 0;
        Coleccionable[] monedas = UnityEngine.Object.FindObjectsByType<Coleccionable>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Coleccionable moneda in monedas)
        {
            if (moneda == null || moneda.gameObject.scene.name != zonaActual) continue;
            total += Mathf.Max(0, moneda.puntos);
        }

        puntajeTotal = total;
    }

    private void Update()
    {
        // Atajo de testing: regenerar las monedas para volver a probarlas.
        if (Keyboard.current != null && Keyboard.current[teclaRegenerar].wasPressedThisFrame)
        {
            RegenerarPuntos();
        }
    }

    // Si el valor cambia en el Inspector durante Play mode, esto avisa a la UI.
    // Al recargar la zona vuelve a calcularse automaticamente.
    private void OnValidate()
    {
        OnScoreChanged?.Invoke(currentScore);
    }

    // Suma puntos SIN control de duplicados (enemigos: reaparecen y se pueden volver a matar).
    public void AddPoints(int points)
    {
        currentScore += points;
        OnScoreChanged?.Invoke(currentScore);

        ProgresoJuego.GuardarPuntaje(zonaActual, currentScore);
    }

    // Suma puntos de una MONEDA identificada: si ya se recolecto antes, NO vuelve a sumar.
    public void AddPoints(int points, string id)
    {
        if (recolectadas.Contains(id)) return; // ya la teniamos
        recolectadas.Add(id);
        currentScore += points;
        OnScoreChanged?.Invoke(currentScore);

        ProgresoJuego.GuardarPuntaje(zonaActual, currentScore);
        ProgresoJuego.GuardarMonedas(recolectadas);
    }

    // ¿Esta moneda ya fue recolectada? (la usa la moneda para no reaparecer al recargar la zona).
    public bool YaRecolectada(string id)
    {
        return recolectadas.Contains(id);
    }

    // PARTIDA NUEVA: borra el progreso de la memoria Y del disco (puntaje, monedas,
    // cinematicas vistas y la zona guardada).
    //
    // Es ESTATICO a proposito: lo llama el boton "new game" del Main Menu, donde no hay
    // ningun ScoreManager en la escena. Si no limpiaramos tambien la memoria, al volver
    // al menu despues de jugar y apretar "new game" el puntaje viejo seguiria ahi.
    public static void NuevaPartida()
    {
        recolectadas = new HashSet<string>();
        monedasCargadas = false;

        ProgresoJuego.BorrarTodo();
        TriggerCinematica.OlvidarVistas();

        if (Instance != null)
        {
            Instance.currentScore = 0;
            Instance.OnScoreChanged?.Invoke(0);
        }
    }

    // Version para llamar desde un boton con el ScoreManager de la escena.
    public void ResetScore()
    {
        NuevaPartida();
    }

    // TESTING: borra solamente el puntaje y las monedas de la zona actual. El
    // progreso de las otras zonas y las cinematicas se conservan.
    // (Tecla del inspector o boton del menu contextual del componente.)
    [ContextMenu("Regenerar Puntos (reaparecer monedas)")]
    public void RegenerarPuntos()
    {
        ProgresoJuego.BorrarPuntaje(zonaActual);
        ProgresoJuego.BorrarMonedasDeZona(zonaActual);

        string prefijoZona = zonaActual + "@";
        recolectadas.RemoveWhere(id => id.StartsWith(prefijoZona));
        currentScore = 0;
        OnScoreChanged?.Invoke(currentScore);

        if (Application.isPlaying)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
