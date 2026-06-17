using UnityEngine;

public class Coleccionable : MonoBehaviour
{
    public int puntos = 10;
    public string tagJugador = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(tagJugador))
        {
            ScoreManager.Instance.AddPoints(puntos);
            Destroy(gameObject);
        }
    }
}
