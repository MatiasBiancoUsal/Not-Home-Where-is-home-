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
        scoreText.text = score.ToString();
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= UpdateScore;
    }
}