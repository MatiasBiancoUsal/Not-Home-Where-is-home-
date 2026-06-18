using System;
using System.Collections.Generic;
using UnityEngine;

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

    private void Awake()
    {
        // Cada escena tiene su ScoreManager (dentro del CANVATODO), pero el puntaje es
        // compartido (estatico), asi que no importa cual instancia sea la "actual".
        Instance = this;
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
}
