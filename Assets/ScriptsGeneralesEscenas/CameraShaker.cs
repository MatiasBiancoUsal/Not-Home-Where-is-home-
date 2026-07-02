using UnityEngine;
using Unity.Cinemachine;

// ============================================================
//  Sacude la camara con Cinemachine Impulse. Tiene DOS shakes
//  distintos y ajustables: uno para cuando el player recibe daño
//  y otro (mas fuerte) para cuando derrota a un enemigo.
//
//  Setup: va en un GameObject con un CinemachineImpulseSource.
//  La Cinemachine Camera necesita un CinemachineImpulseListener.
// ============================================================
[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance { get; private set; }

    [Header("Shake al RECIBIR daño")]
    [Tooltip("Fuerza del sacudon cuando el player es golpeado.")]
    public float damageForce = 0.4f;
    [Tooltip("Duracion del sacudon de daño.")]
    public float damageDuration = 0.2f;

    [Header("Shake al DERROTAR enemigo")]
    [Tooltip("Fuerza del sacudon cuando matás un enemigo (suele ser mas fuerte).")]
    public float killForce = 0.9f;
    [Tooltip("Duracion del sacudon de derrota.")]
    public float killDuration = 0.4f;

    [Header("Shake al SUPER SALTO")]
    [Tooltip("Fuerza del sacudon al ejecutar un super salto (se escala con la carga).")]
    public float superJumpForce = 1.2f;
    [Tooltip("Duracion del sacudon del super salto.")]
    public float superJumpDuration = 0.35f;

    [Header("Shake al IMPACTO del STOMP")]
    [Tooltip("Fuerza del sacudon cuando el stomp golpea el piso.")]
    public float stompImpactForce = 1.5f;
    [Tooltip("Duracion del sacudon del impacto del stomp.")]
    public float stompImpactDuration = 0.3f;

    private CinemachineImpulseSource impulse;

    private void Awake()
    {
        Instance = this;
        impulse = GetComponent<CinemachineImpulseSource>();
    }

    // El player recibio daño.
    public void ShakeDamage()
    {
        DoShake(damageForce, damageDuration);
    }

    // Se derroto a un enemigo.
    public void ShakeEnemyKill()
    {
        DoShake(killForce, killDuration);
    }

    // Super salto ejecutado. 'intensity' (0 a 1) escala la fuerza segun la carga.
    public void ShakeSuperJump(float intensity = 1f)
    {
        DoShake(superJumpForce * intensity, superJumpDuration);
    }

    // El stomp golpeo el piso.
    public void ShakeStompImpact()
    {
        DoShake(stompImpactForce, stompImpactDuration);
    }

    private void DoShake(float force, float duration)
    {
        if (impulse == null) return;
        impulse.ImpulseDefinition.ImpulseDuration = duration;
        // direccion aleatoria, asi el sacudon no es siempre igual.
        Vector2 dir = Random.insideUnitCircle.normalized;
        impulse.GenerateImpulseWithVelocity(new Vector3(dir.x, dir.y, 0f) * force);
    }
}
