using UnityEngine;

public class PlayerAttacks : MonoBehaviour
{
    private PlayerController playerController;

    [Header("Ataque Player")]
    public int hitPower = 1; // cantidad de daño que generan los ataques del jugador
    public int knockBackForce = 6; // fuerza con la que se empujan los enemigos
    public float attackRange = 0.6f; //radio del hitbox
    public Vector2 hitBoxOffset; // distancia u offset del hitbox
    public float pogoForce = 10f;
    public LayerMask enemyLayer;

    public float attackCooldown = 0.05f;

    [Header("Combo")]
    public float comboWindowDuration = 0.25f;

    //cooldowns
    private float timerAttackCoolDown = 0;
    private bool isAttack;
    private Vector2 attackDirection;
    private int comboStep = 0;
    private bool comboBuffered = false;
    private float comboWindowTimer = 0f;

    public bool IsAttack => isAttack;
    public Vector2 AttackDirection => attackDirection;
    public int ComboStep => comboStep;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    public void OnUpdate()
    {
        if (timerAttackCoolDown > 0)
            timerAttackCoolDown -= Time.fixedDeltaTime;

        if (comboWindowTimer > 0)
        {
            comboWindowTimer -= Time.fixedDeltaTime;
            if (comboWindowTimer <= 0)
                comboStep = 0; // se cerró la ventana, resetear combo
        }
    }

    public void AttackHold()
    {
        if (isAttack)
        {
            // solo buffear si es ataque de pie (los ataques up/down no tienen combo)
            if (attackDirection != Vector2.up && attackDirection != Vector2.down)
                comboBuffered = true;
            return;
        }

        // la ventana de combo va ANTES del cooldown: continuar combo no requiere cooldown
        if (comboWindowTimer > 0)
        {
            comboStep++;
            comboWindowTimer = 0;
            isAttack = true;
            return;
        }

        if (timerAttackCoolDown > 0) return;

        // ataque fresco
        timerAttackCoolDown = attackCooldown;
        isAttack = true;
        comboStep = 0;

        Vector2 move = playerController.controles.Player.Move.ReadValue<Vector2>();

        if (move.y < 0.2f && !playerController.jump.IsGrounded)
            attackDirection = Vector2.down;
        else if (move.y > 0.2f)
            attackDirection = Vector2.up;
        else
            attackDirection = playerController.movement.IsFacingRight ? Vector2.right : Vector2.left;
    }

    public void ActiveHitbox()
    {
        Vector2 attackPos = (Vector2)transform.position + attackDirection * attackRange + Vector2.Scale(hitBoxOffset, attackDirection);

        Collider2D[] hurtEnemies = Physics2D.OverlapCircleAll(attackPos, attackRange, enemyLayer);

        foreach (Collider2D enemy in hurtEnemies)
        {
            // aplicar el dano al enemigo
            Damageable damageable = enemy.GetComponent<Damageable>();
            if (damageable != null)
            {
                damageable.ApplyDamage(hitPower, transform.position, knockBackForce);
            }
            /*
            // Health Handler
            HealthHandler healthEnemy = enemy.GetComponent<HealthHandler>();
            if (healthEnemy != null)
            {
                healthEnemy.TakeDamage(hitPower);
            }*/

            if (attackDirection == Vector2.down) // si la direccion de ataque es hacia abajo
            {
                Pogo();
            }
        }
    }

    private void Pogo()
    {
        // cancelar las fuerzas y velociades del rb
        playerController.rb.linearVelocity = new Vector2(playerController.rb.linearVelocity.x, 0);
        // aplicamos un impulso hacia arriba de nuestro jugador
        playerController.rb.AddForce(Vector2.up * pogoForce, ForceMode2D.Impulse);
    }

    public void EndAttack()
    {
        if (comboBuffered && comboStep < 2)
        {
            // presionaron durante la animacion: avanzar combo directamente
            comboStep++;
            comboBuffered = false;
            // isAttack se mantiene en true para el siguiente golpe
        }
        else
        {
            isAttack = false;
            comboBuffered = false;

            bool esAtaquePie = attackDirection != Vector2.up && attackDirection != Vector2.down;
            if (comboStep < 2 && esAtaquePie)
                comboWindowTimer = comboWindowDuration; // abrir ventana para el proximo golpe
            else
            {
                comboWindowTimer = 0;
                comboStep = 0; // combo completo o ataque direccional: resetear
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector2 gizmosPos = (Vector2)transform.position + attackDirection * attackRange + Vector2.Scale(hitBoxOffset, attackDirection);
        Gizmos.DrawWireSphere(gizmosPos, attackRange);
    }
}
