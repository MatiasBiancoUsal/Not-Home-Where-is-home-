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
    // corre el juego). Para arrancar de cero en una partida nueva: ResetScore().
    private static int currentScore = 0;
    private static HashSet<string> recolectadas = new HashSet<string>();

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
    }

    // Suma puntos de una MONEDA identificada: si ya se recolecto antes, NO vuelve a sumar.
    public void AddPoints(int points, string id)
    {
        if (recolectadas.Contains(id)) return; // ya la teniamos
        recolectadas.Add(id);
        currentScore += points;
        OnScoreChanged?.Invoke(currentScore);
    }

    // ¿Esta moneda ya fue recolectada? (la usa la moneda para no reaparecer al recargar la zona).
    public bool YaRecolectada(string id)
    {
        return recolectadas.Contains(id);
    }

    // Llamar al empezar una partida NUEVA (desde el menu) para arrancar el puntaje de cero.
    public void ResetScore()
    {
        currentScore = 0;
        recolectadas.Clear();
        OnScoreChanged?.Invoke(currentScore);
    }

    // TESTING: olvida las monedas recolectadas, resetea el puntaje y recarga la zona,
    // asi todas las monedas vuelven a aparecer para probarlas. (Tecla o boton del inspector.)
    [ContextMenu("Regenerar Puntos (reaparecer monedas)")]
    public void RegenerarPuntos()
    {
        recolectadas.Clear();
        currentScore = 0;
        OnScoreChanged?.Invoke(currentScore);

        if (Application.isPlaying)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
