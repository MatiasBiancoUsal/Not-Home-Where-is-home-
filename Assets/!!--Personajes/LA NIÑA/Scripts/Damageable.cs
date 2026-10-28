using System.Collections;
using UnityEngine;

public class Damageable : MonoBehaviour
{
    private Rigidbody2D rb;
    private HealthHandler healthHandler;
    private PlayerShield shield; // solo el player lo tiene; en los enemigos queda null
    private PlayerAudio playerAudio; // solo el player lo tiene; en los enemigos queda null

    // Knockback

    [Header("KnockBack")]
    public bool activeKnockBack = true; // esta variable determina si el efecto de knockback aplica a este gameobject
    public float knockBackDuration = 0.2f; // cuanto dura el efecto de knockback

    [Tooltip("Capas que FRENAN el knockback (el piso y las paredes). Sin esto los enemigos " +
             "kinematicos atraviesan las paredes al salir despedidos.")]
    public LayerMask capasQueFrenanElKnockback = 1 << 3; // layer 3 = Ground
    [Tooltip("Cuanto antes de la pared se frena, para que no quede la mitad del cuerpo adentro.")]
    [Min(0f)] public float margenContraLaPared = 0.1f;
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
        shield = GetComponent<PlayerShield>();
        playerAudio = GetComponent<PlayerAudio>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.material;
        }
    }

    // Devuelve TRUE si el golpe entro de verdad (se descontó vida).
    // Devuelve FALSE si lo frenó el escudo o la invulnerabilidad: el que golpea puede
    // usar esto para no empujar ni frenar al player.
    public bool ApplyDamage(int damageAmount, Vector2 sourcePosition, float sourceKnockBackForce)
    {
        // Cruzando una puerta entre zonas la niña no puede recibir daño: seria injusto
        // morirse mientras camina sola y no responde a los controles. (Ver TransicionZonas.)
        if (TransicionZonas.InvulnerablePorTransicion && CompareTag("Player"))
        {
            return false;
        }

        // Escondida bajo el piso durante la embestida del boss final: no le entra nada.
        if (EscondersePlayer.Escondida && CompareTag("Player"))
        {
            return false;
        }

        if (isInvulnerability)
        {
            return false;
        }

        // ESCUDO: si esta en pie, se come el golpe entero y el player no pierde vida
        // ni sufre ninguno de los efectos de abajo.
        if (shield != null && shield.AbsorberGolpe(damageAmount, sourcePosition))
        {
            return false;
        }

        // Si este golpe es MORTAL, salteamos TODOS los efectos (knockback, flash, freeze,
        // invulnerabilidad) para que la muerte sea limpia: solo muere y reaparece.
        bool esGolpeMortal = healthHandler != null && healthHandler.CurrentHealth - damageAmount <= 0;

        if (!esGolpeMortal)
        {
            // efecto knockback
            // Algunos enemigos fijos (por ejemplo EnemyWall) no usan Rigidbody2D.
            // El golpe igual debe descontar vida aunque no haya cuerpo para empujar.
            if (activeKnockBack && rb != null)
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

        // El sonido solo se reproduce si el golpe realmente atraveso escudo e
        // invulnerabilidad. Los golpes mortales usan el sonido de muerte.
        if (!esGolpeMortal) playerAudio?.ReproducirDanio();

        // acceder a health y quitar damageAmount
        healthHandler.TakeDamage(damageAmount);

        return true;
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

        // Los enemigos son KINEMATIC: la fisica no los frena contra las paredes, asi que
        // salen despedidos y las atraviesan. Vamos mirando si hay pared en el camino y,
        // si la hay, los frenamos nosotros.
        float restante = duration;
        while (restante > 0f)
        {
            if (HayParedHacia(dirX))
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;
            }

            restante -= Time.deltaTime;
            yield return null;
        }

        if (restante > 0f) yield return new WaitForSeconds(restante);

        rb.linearVelocity = Vector2.zero;
        IsKnockedBack = false;
    }

    // Mira si hay pared pegada del lado hacia el que sale despedido.
    private bool HayParedHacia(float dirX)
    {
        if (capasQueFrenanElKnockback == 0) return false;

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return false;

        Bounds caja = col.bounds;
        Vector2 desde = caja.center;
        float alcance = caja.extents.x + margenContraLaPared;

        // Tres rayos (arriba, centro, abajo) para que no se cuele por una esquina.
        for (int i = -1; i <= 1; i++)
        {
            Vector2 origen = desde + new Vector2(0f, i * caja.extents.y * 0.6f);
            if (Physics2D.Raycast(origen, new Vector2(dirX, 0f), alcance, capasQueFrenanElKnockback))
            {
                return true;
            }
        }

        return false;
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
