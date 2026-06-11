using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class FallingPlatform2D : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float topDetectionTolerance = 0.12f;

    [Header("Warning")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float shakeDuration = 0.8f;
    [SerializeField] private float shakeStrength = 0.08f;
    [SerializeField] private float shakeSpeed = 35f;

    [Header("Fall")]
    [SerializeField] private float fallDelay = 0.15f;
    [SerializeField] private float fallGravityScale = 4f;

    [Header("Cleanup")]
    [SerializeField] private bool destroyAfterFall = true;
    [SerializeField] private float destroyDelay = 3f;

    private Rigidbody2D rb;
    private Collider2D platformCollider;
    private Vector3 visualStartLocalPosition;
    private bool activated;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        platformCollider = GetComponent<Collider2D>();

        if (visualRoot == null)
            visualRoot = transform;

        visualStartLocalPosition = visualRoot.localPosition;

        platformCollider.isTrigger = false;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = Vector2.zero;
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
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;
        if (!IsStandingOnTop(other)) return;

        activated = true;
        StartCoroutine(ShakeThenFall());
    }

    private bool IsStandingOnTop(Collider2D other)
    {
        float playerBottom = other.bounds.min.y;
        float platformTop = platformCollider.bounds.max.y;

        return playerBottom >= platformTop - topDetectionTolerance;
    }

    private IEnumerator ShakeThenFall()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float shakeX = Mathf.Sin(elapsed * shakeSpeed) * shakeStrength;
            float shakeY = Mathf.Sin(elapsed * shakeSpeed * 1.7f) * shakeStrength * 0.35f;

            visualRoot.localPosition = visualStartLocalPosition + new Vector3(shakeX, shakeY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        visualRoot.localPosition = visualStartLocalPosition;

        yield return new WaitForSeconds(fallDelay);

        Fall();

        if (destroyAfterFall)
            Destroy(gameObject, destroyDelay);
    }

    private void Fall()
    {
        platformCollider.enabled = false;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallGravityScale;
        rb.linearVelocity = Vector2.zero;
    }
}