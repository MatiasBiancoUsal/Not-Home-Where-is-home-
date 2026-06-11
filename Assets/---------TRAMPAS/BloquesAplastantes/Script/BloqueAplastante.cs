using System.Collections;
using UnityEngine;

public class BloqueAplastante : MonoBehaviour
{
    [Header("Configuración")]
    public float delayAntesDeCaer = 0.8f;
    public float velocidadCaida = 3f; // mas alto = cae mas rapido (multiplica la gravedad)
    public float tiempoEnElPiso = 2f;
    public float tiempoFade = 0.8f;
    public string tagJugador = "Player";

    [Header("Detección (caja DEBAJO del bloque)")]
    public Vector2 offsetDeteccion = new Vector2(0f, -5f); // centro de la caja relativo al bloque (abajo = Y negativo)
    public Vector2 tamanoDeteccion = new Vector2(3f, 10f); // tamaño de la caja (ancho, alto)

    [Header("Shake")]
    public float shakeDuracion = 0.5f;
    public float shakeFuerza = 0.05f;

    private Rigidbody2D rb;
    private Vector3 posicionOriginal;
    private bool activado = false;
    private SpriteRenderer sprite;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        posicionOriginal = transform.position;
        sprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (activado) return;

        // Caja de deteccion DEBAJO del bloque: solo se activa si el player entra ahi (no si esta arriba).
        Vector2 centro = (Vector2)transform.position + offsetDeteccion;
        Collider2D[] hits = Physics2D.OverlapBoxAll(centro, tamanoDeteccion, 0f);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag(tagJugador))
            {
                activado = true;
                StartCoroutine(SecuenciaCaida());
                break;
            }
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
        rb.gravityScale = velocidadCaida; // controla que tan rapido cae (parametro del Inspector)

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

    // Dibuja la caja de deteccion en el editor (para verla y acomodarla).
    private void OnDrawGizmos()
    {
        Vector2 centro = (Vector2)transform.position + offsetDeteccion;

        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawCube(centro, tamanoDeteccion);     // relleno semi-transparente

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(centro, tamanoDeteccion); // contorno
    }
}
