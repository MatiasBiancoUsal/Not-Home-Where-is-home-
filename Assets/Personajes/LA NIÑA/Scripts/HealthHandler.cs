using UnityEngine;

public class HealthHandler : MonoBehaviour
{
    public int maxHealth; // cantidad MAX vida del objeto
   [SerializeField] private int currentHealth; // cantidad ACTUAL vida del objeto

    private void Awake()
    {
        currentHealth = maxHealth; // al iniciar, la vida actual es igual a la vida máxima
    }
    
    public void TakeDamage(int damage)
    {
        currentHealth -= damage; // disminuye la vida actual en la cantidad de danio recibido

        if (currentHealth <= 0) // si la vida actual es menor o igual a 0 (muert)
        {
           Die();
        }
    }

    private void Die()
    {
        // activar particulas
        // generar sonido
        Destroy(gameObject);
    }
}
