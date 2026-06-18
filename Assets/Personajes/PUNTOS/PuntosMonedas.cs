using UnityEngine;

public class Coleccionable : MonoBehaviour
{
    public int puntos = 10;
    public string tagJugador = "Player";

    private string id;

    private void Start()
    {
        // ID unico de esta moneda: escena + su posicion (no debe haber dos monedas exactamente
        // en el mismo lugar). Sirve para recordar si ya la recolectamos.
        id = gameObject.scene.name + "@" + transform.position.x.ToString("F1") + "," + transform.position.y.ToString("F1");

        // Si ya la habiamos recolectado (otra vida / recarga de la zona), NO debe reaparecer: la sacamos.
        if (ScoreManager.Instance != null && ScoreManager.Instance.YaRecolectada(id))
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(tagJugador))
        {
            // suma con ID: el puntaje queda guardado y esta moneda no volvera a aparecer.
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddPoints(puntos, id);

            Destroy(gameObject);
        }
    }
}
