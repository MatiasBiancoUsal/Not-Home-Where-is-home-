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
    public Vector2 tamanoDeteccion = new Vector2(3f, 10f);  // tamaño de la caja (ancho, alto)

    [Header("Shake")]
    public float shakeDuracion = 0.5f;
    public float shakeFuerza = 0.05f;

    private Rigidbody2D rb;
    private Vector3 posicionOriginal;
    private bool activado = false;
    private SpriteRenderer sprite;
    private Collider2D[] misColliders;
    private Collider2D playerCol;   // collider del player, para activar/desactivar el choque
    private HurtBox hurtBox;        // el HurtBox solo daña mientras el bloque CAE

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        posicionOriginal = transform.position;
        sprite = GetComponent<SpriteRenderer>();
        misColliders = GetComponents<Collider2D>();

        // Evitar que los bloques se traben ENTRE SI al caer: cuando estan pegados en fila, sus
        // colliders se tocan y, con la posicion X congelada, el motor de fisica los frena
        // mutuamente. Ignoramos las colisiones entre bloques (siguen chocando con piso/paredes).
        foreach (BloqueAplastante otro in FindObjectsByType<BloqueAplastante>(FindObjectsSortMode.None))
        {
            if (otro == this) continue;
            foreach (Collider2D suyo in otro.GetComponents<Collider2D>())
                foreach (Collider2D mio in misColliders)
                    Physics2D.IgnoreCollision(mio, suyo);
        }

        // Guardar el collider del player. Mientras el bloque NO esta apoyado, ignoramos el choque
        // (con masa enorme te empujaria al infinito). Lo restauramos al aterrizar para que te
        // puedas PARAR ENCIMA del bloque caido.
        GameObject playerObj = GameObject.FindGameObjectWithTag(tagJugador);
        if (playerObj != null) playerCol = playerObj.GetComponent<Collider2D>();
        IgnorarChoqueConPlayer(true);

        // El HurtBox solo debe matar MIENTRAS CAE (no cuando esta quieto arriba ni cuando te
        // parás encima). Arranca apagado y lo prendemos al caer.
        hurtBox = GetComponentInChildren<HurtBox>();
        if (hurtBox != null) hurtBox.enabled = false;
    }

    // Activa o desactiva el choque fisico entre el bloque (collider solido) y el player.
    private void IgnorarChoqueConPlayer(bool ignorar)
    {
        if (playerCol == null) return;
        foreach (Collider2D mio in misColliders)
            if (!mio.isTrigger) // solo el collider solido (el que empuja / sostiene)
                Physics2D.IgnoreCollision(mio, playerCol, ignorar);
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

        // el delay restante nunca puede ser negativo (si el shake dura mas que el delay, no esperamos)
        float restante = delayAntesDeCaer - shakeDuracion;
        if (restante > 0) yield return new WaitForSeconds(restante);

        // Cae
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        rb.mass = 9999f;
        rb.gravityScale = velocidadCaida; // controla que tan rapido cae (parametro del Inspector)
        // al pasar de Kinematic a Dynamic el cuerpo arranca dormido: lo despertamos y empujamos.
        rb.WakeUp();
        rb.linearVelocity = new Vector2(0f, -2f);

        // MIENTRAS CAE: el HurtBox mata al aplastarte (segun el daño que le pusiste a cada bloque).
        if (hurtBox != null) hurtBox.enabled = true;

        yield return new WaitForSeconds(0.5f);
        yield return new WaitUntil(() => Mathf.Abs(rb.linearVelocity.y) < 0.1f);

        // ATERRIZÓ: apagamos el HurtBox (ya no mata) y restauramos el choque -> te podes PARAR ENCIMA.
        if (hurtBox != null) hurtBox.enabled = false;
        IgnorarChoqueConPlayer(false);

        // Espera en el piso
        yield return new WaitForSeconds(tiempoEnElPiso);

        // Fade out
        yield return StartCoroutine(Fade(1f, 0f));

        // Desactivar colliders mientras está invisible
        foreach (Collider2D col in misColliders)
            col.enabled = false;

        // Vuelve arriba
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        transform.position = posicionOriginal;

        yield return new WaitForSeconds(0.3f);

        // Reactivar colliders y volver a ignorar el choque con el player para el proximo ciclo.
        foreach (Collider2D col in misColliders)
            col.enabled = true;
        IgnorarChoqueConPlayer(true);

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
