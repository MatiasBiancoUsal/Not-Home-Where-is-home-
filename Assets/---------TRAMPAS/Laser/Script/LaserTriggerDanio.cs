using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class LaserTriggerDanio : MonoBehaviour
{
    public HurtBox ConfiguracionDanio;
    public LaserLoop Laser;

    [Header("Empuje al jugador")]
    [Min(0f)] public float FuerzaEmpuje = 8f;
    [Min(0.05f)] public float DuracionEmpuje = 0.2f;

    private float proximoGolpe;

    private void Awake()
    {
        BoxCollider2D caja = GetComponent<BoxCollider2D>();
        caja.isTrigger = true;

        if (ConfiguracionDanio == null)
            ConfiguracionDanio = GetComponent<HurtBox>();
        if (Laser == null)
            Laser = GetComponentInParent<LaserLoop>();
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        Damageable jugador = ObtenerJugador(otro);
        if (jugador == null) return;

        Laser?.ReproducirSonidoContacto();
        AplicarDanio(jugador);
    }

    private void OnTriggerStay2D(Collider2D otro)
    {
        Damageable jugador = ObtenerJugador(otro);
        if (jugador != null)
            AplicarDanio(jugador);
    }

    private Damageable ObtenerJugador(Collider2D otro)
    {
        Damageable damageable = otro.GetComponentInParent<Damageable>();
        return damageable != null && damageable.CompareTag("Player") ? damageable : null;
    }

    private void AplicarDanio(Damageable jugador)
    {
        if (Time.time < proximoGolpe) return;

        int cantidad = ConfiguracionDanio != null ? ConfiguracionDanio.damageHurtBox : 1;
        float espera = ConfiguracionDanio != null ? ConfiguracionDanio.damageCoolDown : 0.5f;

        // El origen se coloca delante de la niña para empujarla hacia atrás,
        // en dirección contraria a donde está mirando.
        Vector2 origenEmpuje = transform.position;
        PlayerMovement movimiento = jugador.GetComponent<PlayerMovement>();
        if (movimiento != null)
        {
            float ladoFrontal = movimiento.IsFacingRight ? 1f : -1f;
            origenEmpuje = (Vector2)jugador.transform.position + Vector2.right * ladoFrontal;
        }

        if (jugador.ApplyDamage(cantidad, origenEmpuje, FuerzaEmpuje))
        {
            proximoGolpe = Time.time + Mathf.Max(0.05f, espera);

            if (movimiento != null)
                StartCoroutine(BloquearMovimiento(movimiento));
        }
    }

    private IEnumerator BloquearMovimiento(PlayerMovement movimiento)
    {
        movimiento.onKnockBack = true;
        yield return new WaitForSeconds(DuracionEmpuje);
        if (movimiento != null)
        {
            movimiento.onKnockBack = false;
            // Vuelve a leer el control actual. Si no hay ninguna tecla presionada,
            // detiene al personaje y también corrige la animación de caminar.
            movimiento.Move();
        }
    }
}
