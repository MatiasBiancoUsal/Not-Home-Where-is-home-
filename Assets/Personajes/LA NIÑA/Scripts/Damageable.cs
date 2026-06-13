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

    // Freeze Time

    // Invulnerability

    // Particles


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        healthHandler = GetComponent<HealthHandler>();
    }

    public void ApplyDamage(int damageAmount, Vector2 sourcePosition, float sourceKnockBackForce)
    {
        if (activeKnockBack)
        {
            KnockBackApply(sourcePosition, sourceKnockBackForce);
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
}
