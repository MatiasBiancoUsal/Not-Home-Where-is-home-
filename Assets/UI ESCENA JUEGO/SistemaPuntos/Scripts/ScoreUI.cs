using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    private void Start()
    {
        ScoreManager.Instance.OnScoreChanged += UpdateScore;
        UpdateScore(ScoreManager.Instance.CurrentScore);
    }

    private void UpdateScore(int score)
    {
        // Un solo texto con puntaje actual + total del nivel, ej: "30 / 100"
        scoreText.text = score + " / " + ScoreManager.Instance.PuntajeTotal;
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= UpdateScore;
    }
}