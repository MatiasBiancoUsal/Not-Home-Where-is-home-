using System.Collections;
using UnityEngine;

public class BloqueAplastante : MonoBehaviour
{
    [Header("Configuración")]
    public float distanciaDeteccion = 30f;
    public float delayAntesDeCaer = 0.8f;
    public float tiempoEnElPiso = 2f;
    public float tiempoFade = 0.8f;
    public string tagJugador = "Player";

    [Header("Shake")]
    public float shakeDuracion = 0.5f;
    public float shakeFuerza = 0.05f;

    private Rigidbody2D rb;
    private Vector3 posicionOriginal;
    private bool activado = false;
    private Transform jugador;
    private SpriteRenderer sprite;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        posicionOriginal = transform.position;
        sprite = GetComponent<SpriteRenderer>();

        GameObject obj = GameObject.FindWithTag(tagJugador);
        if (obj != null)
            jugador = obj.transform;
        else
            Debug.LogError("BLOQUE: no encontró tag: " + tagJugador);
    }

    void Update()
    {
        if (activado || jugador == null) return;

        float distancia = Vector2.Distance(transform.position, jugador.position);
        if (distancia <= distanciaDeteccion)
        {
            activado = true;
            StartCoroutine(SecuenciaCaida());
        }
    }

    IEnumerator SecuenciaCaida()
    {
        // Shake
        float timer = 0f;
        while (timer < shakeDuracion)
        {
            transform.position = posicionOriginal + (Vector3)Random.insideUnitCircle * shakeFuerza;
            timer += Time.deltaTime;
            yield return null;
        }
        transform.position = posicionOriginal;

        yield return new WaitForSeconds(delayAntesDeCaer - shakeDuracion);

        // Cae
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        rb.mass = 9999f;

        yield return new WaitForSeconds(0.5f);
        yield return new WaitUntil(() => Mathf.Abs(rb.linearVelocity.y) < 0.1f);

        // Espera en el piso
        yield return new WaitForSeconds(tiempoEnElPiso);

        // Fade out
        yield return StartCoroutine(Fade(1f, 0f));

        // Desactivar colliders mientras está invisible
        foreach (Collider2D col in GetComponents<Collider2D>())
            col.enabled = false;

        // Vuelve arriba
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        transform.position = posicionOriginal;

        yield return new WaitForSeconds(0.3f);

        // Reactivar colliders
        foreach (Collider2D col in GetComponents<Collider2D>())
            col.enabled = true;

        // Fade in
        yield return StartCoroutine(Fade(0f, 1f));

        activado = false;
    }

    IEnumerator Fade(float desde, float hasta)
    {
        float timer = 0f;
        Color color = sprite.color;

        while (timer < tiempoFade)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(desde, hasta, timer / tiempoFade);
            sprite.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        sprite.color = new Color(color.r, color.g, color.b, hasta);
    }
}