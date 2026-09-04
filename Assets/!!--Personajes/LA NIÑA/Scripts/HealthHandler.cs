using System;
using UnityEngine;

public class HealthHandler : MonoBehaviour
{
    public int maxHealth;

    [SerializeField] private int currentHealth;

    [Tooltip("Si está activo, el objeto se DESTRUYE al morir (enemigos). El player lo deja desactivado para hacer respawn.")]
    public bool destroyOnDeath = true;

    public event Action OnDeath;
    public event Action<int> OnHealthChanged;

    public int CurrentHealth => currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Cura al personaje sin pasarse de maxHealth.
    //
    // Devuelve CUANTA vida se recupero de verdad: 0 si ya estaba llena, si esta muerto,
    // o si le pasamos un valor invalido. El orbe de vida usa ese numero para decidir si
    // se deja agarrar o si se queda en el mapa para mas adelante.
    public int Heal(int amount)
    {
        if (amount <= 0) return 0;
        if (currentHealth <= 0) return 0;          // ya murio: no se revive curando
        if (currentHealth >= maxHealth) return 0;  // ya esta al maximo

        int antes = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

        int curado = currentHealth - antes;
        if (curado > 0) OnHealthChanged?.Invoke(currentHealth);

        return curado;
    }

    // Cuanta vida le falta para llegar al maximo.
    public int VidaQueFalta => Mathf.Max(0, maxHealth - currentHealth);

    private void Die()
    {
        if (Time.timeScale == 0)
        {
            Time.timeScale = 1;
        }

        // activar particulas
        // generar sonido

        OnDeath?.Invoke();

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}