using System;
using UnityEngine;

public class HealthHandler : MonoBehaviour
{
    public int maxHealth; // cantidad MAX vida del objeto
    [SerializeField] private int currentHealth; // cantidad ACTUAL vida del objeto

    [Tooltip("Si está activo, el objeto se DESTRUYE al morir (enemigos). El player lo deja desactivado para hacer respawn.")]
    public bool destroyOnDeath = true;

    // se dispara cuando la vida llega a 0. Lo escucha la secuencia de muerte del player.
    public event Action OnDeath;

    public int CurrentHealth => currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth; // al iniciar, la vida actual es igual a la vida maxima
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return; // ya esta muerto, no disparamos la muerte dos veces

        currentHealth -= damage; // disminuye la vida actual en la cantidad de danio recibido

        if (currentHealth <= 0) // si la vida actual es menor o igual a 0 (muerte)
        {
            Die();
        }
    }

    private void Die()
    {
        if (Time.timeScale == 0)
        {
            Time.timeScale = 1; // por si moriste durante un freeze del combate
        }

        // activar particulas
        // generar sonido
        OnDeath?.Invoke();                       // avisa a quien escuche (ej: la secuencia de muerte del player)
        if (destroyOnDeath) Destroy(gameObject); // enemigos: se destruyen. player: no (hace respawn).
    }
}
