using System.Collections;
using UnityEngine;

public class Damageable : MonoBehaviour
{
    private Rigidbody2D rb;

    // Knockback

    [Header("KnockBack")]
    public bool activeKnockBack = true; // esta variable determina si el efecto de knockback aplica a este gameobject
    public float knockBackDuration = 0.15f; // cuanto dura el efecto de knockback

    // Flash

    // Freeze Time

    // Invulnerability

    // Particles


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void ApplyDamage(int damageAmount, Vector2 sourcePosition, float sourceKnockBackForce)
    {
        if (activeKnockBack)
        {
            KnockBackApply(sourcePosition, sourceKnockBackForce);
        }
    }

    // acceder a health y quitar damageAmount

    private void KnockBackApply(Vector2 sourcePosition, float sourceKnockBackForce)
    {
        // calcular direccion
        Vector2 direction = ((Vector2)transform.position - sourcePosition).normalized;

        // resetear valores y fuerzas
        rb.linearVelocity = Vector2.zero;

        // ejecutar la rutina del knockback
        StartCoroutine(KnockBackRoutine(direction, sourceKnockBackForce, knockBackDuration));

    }

    IEnumerator KnockBackRoutine(Vector2 direction, float force, float duration)
    {
        // agregar impulso
        rb.AddForce(direction * force, ForceMode2D.Impulse);

        // esperar un tiempo minimo
        yield return new WaitForSeconds(duration);

        // resetear valores y fuerzas
        rb.linearVelocity = Vector2.zero;
    }
}
