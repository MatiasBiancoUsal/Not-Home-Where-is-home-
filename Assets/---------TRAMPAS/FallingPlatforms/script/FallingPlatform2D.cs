using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class FallingPlatform2D : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float topDetectionTolerance = 0.12f;

    [Header("Warning")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float shakeDuration = 0.8f;
    [SerializeField] private float shakeStrength = 0.08f;
    [SerializeField] private float shakeSpeed = 35f;
    [Tooltip("Si está activo, el player parado encima tiembla junto con la plataforma durante el aviso.")]
    [SerializeField] private bool sacudirAlPlayer = true;

    [Header("Fall")]
    [SerializeField] private float fallDelay = 0.15f;
    [SerializeField] private float fallGravityScale = 4f;

    [Header("Reaparición")]
    [Tooltip("Segundos desde que cae hasta que la plataforma vuelve a aparecer en su lugar.")]
    [SerializeField] private float tiempoReaparicion = 3f;
    [Tooltip("Cuanto tarda el fade in (aparecer de a poco) cuando reaparece.")]
    [SerializeField] private float fadeInDuration = 0.5f;

    private Rigidbody2D rb;
    private Collider2D platformCollider;
    private SpriteRenderer[] sprites;
    private Vector3 visualStartLocalPosition;
    private Vector3 startPosition;       // posicion original, para reaparecer ahi
    private bool activated;
    private Rigidbody2D playerRb;        // rigidbody del player parado encima (para sacudirlo)

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        platformCollider = GetComponent<Collider2D>();
        sprites = GetComponentsInChildren<SpriteRenderer>();

        if (visualRoot == null)
            visualRoot = transform;

        visualStartLocalPosition = visualRoot.localPosition;
        startPosition = transform.position;

        platformCollider.isTrigger = false;

        ResetFisica();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryActivate(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryActivate(collision.collider);
    }

    private void TryActivate(Collider2D other)
    {
        if (activated) return;
        if (!other.CompareTag(playerTag)) return;
        if (!IsStandingOnTop(other)) return;

        playerRb = other.attachedRigidbody; // el rigidbody del player (no el transform)
        activated = true;
        StartCoroutine(ShakeThenFallThenRespawn());
    }

    private bool IsStandingOnTop(Collider2D other)
    {
        float playerBottom = other.bounds.min.y;
        float platformTop = platformCollider.bounds.max.y;

        return playerBottom >= platformTop - topDetectionTolerance;
    }

    private IEnumerator ShakeThenFallThenRespawn()
    {
        // 1) AVISO: la plataforma (y el player encima) tiemblan.
        float elapsed = 0f;
        Vector2 prevShake = Vector2.zero;

        while (elapsed < shakeDuration)
        {
            float shakeX = Mathf.Sin(elapsed * shakeSpeed) * shakeStrength;
            float shakeY = Mathf.Sin(elapsed * shakeSpeed * 1.7f) * shakeStrength * 0.35f;
            Vector2 shakeOffset = new Vector2(shakeX, shakeY);

            visualRoot.localPosition = visualStartLocalPosition + (Vector3)shakeOffset;

            // al player le movemos el RIGIDBODY (su transform lo pisa la fisica) con el delta del shake.
            if (sacudirAlPlayer && playerRb != null)
                playerRb.position += shakeOffset - prevShake;
            prevShake = shakeOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // devolver el ultimo desplazamiento al player y dejar el visual en su lugar.
        if (sacudirAlPlayer && playerRb != null)
            playerRb.position -= prevShake;
        visualRoot.localPosition = visualStartLocalPosition;

        yield return new WaitForSeconds(fallDelay);

        // 2) CAE.
        Fall();

        // 3) Espera y REAPARECE con fade in.
        yield return new WaitForSeconds(tiempoReaparicion);
        yield return StartCoroutine(Respawn());
    }

    private void Fall()
    {
        platformCollider.enabled = false;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallGravityScale;
        rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator Respawn()
    {
        // volver a su lugar, quieta y firme.
        transform.position = startPosition;
        visualRoot.localPosition = visualStartLocalPosition;
        ResetFisica();
        playerRb = null;

        // aparecer de a poco. Hasta que no termine el fade, no es solida (no se puede pisar).
        platformCollider.enabled = false;
        SetAlpha(0f);

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(t / fadeInDuration));
            yield return null;
        }
        SetAlpha(1f);

        platformCollider.enabled = true; // ya aparecio completa: vuelve a ser solida
        activated = false;
    }

    private void SetAlpha(float a)
    {
        foreach (SpriteRenderer sr in sprites)
        {
            Color c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }

    // Deja el rigidbody quieto y kinematico (estado de "plataforma firme").
    private void ResetFisica()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = Vector2.zero;
    }
}
