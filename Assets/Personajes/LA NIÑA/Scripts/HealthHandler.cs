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

    private void Die()
    {
        if (Time.timeScale == 0)
        {
            Time.timeScale = 1;
        }
        // activar particulas
        // generar sonido
        OnDeath?.Invoke();
        if (destroyOnDeath) Destroy(gameObject);
    }
}