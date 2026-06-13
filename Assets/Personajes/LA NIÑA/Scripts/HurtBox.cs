using System.Collections;
using UnityEngine;

public class HurtBox : MonoBehaviour
{
    [Header("HurtBox")]
    public int damageHurtBox; //cantidad del hurtbox con cada golpe
    public float knockBackHurtBox; // fuerza de retroceso que aplica el hurt box
    public float timeKnockBack = 0.15f; // duracion del knock back
    public Vector2 hurtBoxSize; // tamaño del hurt box
    public Vector2 hurtBoxOffset; // posicion del hurt box
    public LayerMask targetLayer; // layers de los game objects afectados por el hurt box

    // cooldown
    public float damageCoolDown;
    private float coolDownTimer;

    void Update()
    {
        if (coolDownTimer > 0)
        {
            coolDownTimer -= Time.deltaTime;
            return;
        }

        CheckHurtBox();
    }

    private void CheckHurtBox()
    {
        Vector2 center = (Vector2)transform.position + hurtBoxOffset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, hurtBoxSize, 0f, targetLayer);

        foreach (Collider2D hit in hits)
        {
            Damageable damage = hit.GetComponent<Damageable>();
            if (damage != null)
            {
                // punto de contacto entre el hurt box y el collider del hit
                Vector2 contactPoint = hit.ClosestPoint(center);

                // si el centro del hurtbox es muy cercano al contact point, etonces forzar direccion desde el centro
                if ((contactPoint - center).sqrMagnitude < 0.0001f)
                {
                    contactPoint = center;
                }
                damage.ApplyDamage(damageHurtBox, contactPoint, knockBackHurtBox);
            }

            // en caso de que el hit detectado sea Player Movement
            PlayerMovement playerMovement = hit.GetComponent<PlayerMovement>(); // se obtiene el player movement
            if (playerMovement != null)
            {
                playerMovement.onKnockBack = true;
                // llamar corutina
                StartCoroutine(ResetKnockBack(playerMovement, timeKnockBack));
            }
            coolDownTimer = damageCoolDown;
        }
    }

    IEnumerator ResetKnockBack(PlayerMovement playerMovement, float timeKnockBack)
    {
        yield return new WaitForSeconds(timeKnockBack);
        if (playerMovement != null)
        {
            playerMovement.onKnockBack = false; // se resetea el knock back del player movement
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red; // color del hurt box
        Vector2 center = (Vector2)transform.position + hurtBoxOffset;
        Gizmos.DrawWireCube(center, hurtBoxSize);
    }
}
