using System.Collections;
using UnityEngine;

public class Damageable : MonoBehaviour
{
    private Rigidbody2D rb;
    private HealthHandler healthHandler;

    // Knockback

    [Header("KnockBack")]
    public bool activeKnockBack = true; // esta variable determina si el efecto de knockback aplica a este gameobject
    public float knockBackDuration = 0.2f; // cuanto dura el efecto de knockback
    public bool IsKnockedBack { get; private set; }

    // Flash
    [Header("Flash")]
    public bool activeFlash = true; //esta variable determina si el efecto flash aplica a este gameobject
    public float flashDuration = 0.1f; //duracion del efecto flash
    public Material flashMaterial; // material que pinta de blanco nuestro game object
    private Material originalMaterial; // material original del game object
    private SpriteRenderer spriteRenderer;

    // Freeze Time
    [Header("Freeze Time")]
    public bool activeFreezeTime = true; //esta variable determina si el efecto freeze time aplica a este gameobject
    public float freezeDuration = 0.05f; //tiempo que congelamos el juego cuando atacamos

    // Invulnerability
    [Header("Invulnerability")]
    public bool activeInvulnerability = false; //esta variable determina si el efecto de invulnerabilidad aplica a este gameobject
    private bool isInvulnerability;
    public float timeInvulnerability = 1f; // si estamos o no invulnerables

    // Particles


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        healthHandler = GetComponent<HealthHandler>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.material;
        }
    }

    public void ApplyDamage(int damageAmount, Vector2 sourcePosition, float sourceKnockBackForce)
    {
        if (isInvulnerability)
        {
            return;
        }

        // Si este golpe es MORTAL, salteamos TODOS los efectos (knockback, flash, freeze,
        // invulnerabilidad) para que la muerte sea limpia: solo muere y reaparece.
        bool esGolpeMortal = healthHandler != null && healthHandler.CurrentHealth - damageAmount <= 0;

        if (!esGolpeMortal)
        {
            // efecto knockback
            if (activeKnockBack)
            {
                KnockBackApply(sourcePosition, sourceKnockBackForce);
            }

            //efecto flash
            if (activeFlash)
            {
                StartCoroutine(FlashEffect());
            }

            // efecto freeze time
            if (activeFreezeTime)
            {
                StartCoroutine(FreezeTimeEffect());
            }

            // efecto invulnerabilidad
            if (activeInvulnerability)
            {
                StartCoroutine(InvulnerabilityEffect());
            }
        }

        // acceder a health y quitar damageAmount
        healthHandler.TakeDamage(damageAmount);

    }

    private void KnockBackApply(Vector2 sourcePosition, float sourceKnockBackForce)
    {
        // solo el eje X: el enemigo es kinematic y no tiene gravedad propia
        float dirX = Mathf.Sign(transform.position.x - sourcePosition.x);
        StartCoroutine(KnockBackRoutine(dirX, sourceKnockBackForce, knockBackDuration));
    }

    IEnumerator KnockBackRoutine(float dirX, float force, float duration)
    {
        IsKnockedBack = true;

        // kinematic no responde a AddForce, seteamos velocidad directamente
        rb.linearVelocity = new Vector2(dirX * force, rb.linearVelocity.y);

        yield return new WaitForSeconds(duration);

        rb.linearVelocity = Vector2.zero;
        IsKnockedBack = false;
    }

    IEnumerator FlashEffect()
    {
        if (spriteRenderer == null || flashMaterial == null)
        {
            yield break;
        }

        spriteRenderer.material = flashMaterial;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.material = originalMaterial;
    }

    IEnumerator FreezeTimeEffect()
    {

        if (Time.timeScale == 0)
        {
            yield break;
        }

        float originalTime = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(freezeDuration);

        if (Time.timeScale == 0)
        {
            Time.timeScale = originalTime;
        }
    }

    IEnumerator InvulnerabilityEffect()
    {
        isInvulnerability = true;
        yield return new WaitForSeconds(timeInvulnerability);
        isInvulnerability = false;
    }
}
