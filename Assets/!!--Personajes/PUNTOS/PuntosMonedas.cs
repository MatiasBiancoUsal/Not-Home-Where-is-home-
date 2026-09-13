using System.Collections;
using UnityEngine;

public class Coleccionable : MonoBehaviour
{
    public int puntos = 10;
    public string tagJugador = "Player";

    [Header("Animacion de recolectado")]
    [Tooltip("Nombre EXACTO del trigger que creaste en el Animator para la animacion de recolectado.")]
    public string triggerRecolectado = "Obtained";
    [Tooltip("Cuanto dura la animacion de recolectado antes de que la moneda desaparezca.")]
    public float duracionAnimRecolectado = 0.52f;

    [Header("Sonido de recolectado")]
    [Tooltip("Suena al agarrar la moneda. Cada prefab (1PUNTO, 10PUNTOS) tiene el suyo. Se puede dejar vacio.")]
    public AudioClip sonidoRecolectado;
    [Range(0f, 1f)] public float volumenSonido = 1f;

    private string id;
    private bool recolectada = false;

    private void Start()
    {
        // ID unico de esta moneda: escena + su posicion (no debe haber dos monedas en el mismo lugar).
        id = gameObject.scene.name + "@" + transform.position.x.ToString("F1") + "," + transform.position.y.ToString("F1");

        // Si ya la habiamos recolectado (otra vida / recarga de la zona), NO debe reaparecer.
        if (ScoreManager.Instance != null && ScoreManager.Instance.YaRecolectada(id))
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (recolectada) return; // ya la agarramos, no procesar de nuevo

        if (other.CompareTag(tagJugador))
        {
            recolectada = true;

            // suma con ID: el puntaje queda guardado y esta moneda no volvera a aparecer.
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddPoints(puntos, id);

            // Por el AudioManager (grupo SFX): lo regula el slider de Sonidos y no se
            // corta cuando la moneda se destruye.
            if (sonidoRecolectado != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(sonidoRecolectado, volumenSonido);

            StartCoroutine(SecuenciaRecolectado());
        }
    }

    private IEnumerator SecuenciaRecolectado()
    {
        // ya no se puede volver a recolectar ni chocar
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        // reproducir la animacion de recolectado (el Animator puede estar en la moneda o en un hijo)
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.SetTrigger(triggerRecolectado);

        // esperar a que se vea la animacion y recien ahi desaparecer
        yield return new WaitForSeconds(duracionAnimRecolectado);
        Destroy(gameObject);
    }
}
