using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // ESTATICOS: el puntaje y las monedas ya recolectadas persisten entre escenas.
    // No se reinician al cambiar de zona ni al morir/recargar (siguen vivos mientras
    // corre el juego). Ademas se GUARDAN EN DISCO (ProgresoJuego), asi siguen estando
    // cuando el jugador cierra el juego y lo vuelve a abrir.
    // Para arrancar de cero en una partida nueva: ResetScore().
    private static int currentScore = 0;
    private static HashSet<string> recolectadas = new HashSet<string>();

    // Para leer el guardado una sola vez por partida (no en cada cambio de zona).
    private static bool progresoCargado = false;

    // El proyecto tiene "Enter Play Mode Options" con Reload Domain DESACTIVADO
    // (Edit > Project Settings > Editor): eso hace que el Play arranque rapido, pero deja
    // las variables static con el valor de la sesion anterior. Por eso las reiniciamos
    // a mano al empezar cada partida: asi se vuelven a leer del disco de cero.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarStatics()
    {
        currentScore = 0;
        recolectadas = new HashSet<string>();
        progresoCargado = false;
    }

    public int CurrentScore => currentScore;

    public event Action<int> OnScoreChanged;

    [Header("Testing (solo para probar en el editor)")]
    [Tooltip("Apretá esta tecla en Play para REGENERAR las monedas: limpia las recolectadas, resetea el puntaje y recarga la zona.")]
    public Key teclaRegenerar = Key.F5;

    private void Awake()
    {
        // Cada escena tiene su ScoreManager (dentro del CANVATODO), pero el puntaje es
        // compartido (estatico), asi que no importa cual instancia sea la "actual".
        Instance = this;

        // La primera zona que se carga en la partida lee el progreso guardado.
        // Corre en Awake para que el ScoreUI ya lo encuentre cargado en su Start.
        if (!progresoCargado)
        {
            currentScore = ProgresoJuego.CargarPuntaje();
            recolectadas = ProgresoJuego.CargarMonedas();
            progresoCargado = true;
        }
    }

    private void Update()
    {
        // Atajo de testing: regenerar las monedas para volver a probarlas.
        if (Keyboard.current != null && Keyboard.current[teclaRegenerar].wasPressedThisFrame)
        {
            RegenerarPuntos();
        }
    }

    // Suma puntos SIN control de duplicados (enemigos: reaparecen y se pueden volver a matar).
    public void AddPoints(int points)
    {
        currentScore += points;
        OnScoreChanged?.Invoke(currentScore);

        ProgresoJuego.GuardarPuntaje(currentScore);
    }

    // Suma puntos de una MONEDA identificada: si ya se recolecto antes, NO vuelve a sumar.
    public void AddPoints(int points, string id)
    {
        if (recolectadas.Contains(id)) return; // ya la teniamos
        recolectadas.Add(id);
        currentScore += points;
        OnScoreChanged?.Invoke(currentScore);

        ProgresoJuego.GuardarPuntaje(currentScore);
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
        currentScore = 0;
        recolectadas = new HashSet<string>();
        progresoCargado = false; // que la proxima zona lo vuelva a leer (ya vacio)

        ProgresoJuego.BorrarTodo();
        TriggerCinematica.OlvidarVistas();

        if (Instance != null)
        {
            Instance.OnScoreChanged?.Invoke(currentScore);
        }
    }

    // Version para llamar desde un boton con el ScoreManager de la escena.
    public void ResetScore()
    {
        NuevaPartida();
    }

    // TESTING: borra TODO el progreso guardado (monedas, puntaje y cinematicas vistas)
    // y recarga la zona, asi todo vuelve a aparecer para probarlo.
    // (Tecla del inspector o boton del menu contextual del componente.)
    [ContextMenu("Regenerar Puntos (reaparecer monedas)")]
    public void RegenerarPuntos()
    {
        NuevaPartida();

        if (Application.isPlaying)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
