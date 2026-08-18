using UnityEngine;

// Va EN el player. Cuando su vida BAJA (recibió daño), sacude la camara.
// No toca HealthHandler: solo escucha su evento OnHealthChanged.
[RequireComponent(typeof(HealthHandler))]
public class PlayerCameraShake : MonoBehaviour
{
    private HealthHandler health;
    private int lastHealth;

    private void Awake()
    {
        health = GetComponent<HealthHandler>();
    }

    private void OnEnable()
    {
        if (health != null) health.OnHealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if (health != null) health.OnHealthChanged -= OnHealthChanged;
    }

    private void Start()
    {
        if (health != null) lastHealth = health.CurrentHealth;
    }

    private void OnHealthChanged(int newHealth)
    {
        // solo sacudimos si la vida BAJÓ (recibió daño), no si se cura.
        if (newHealth < lastHealth)
            CameraShaker.Instance?.ShakeDamage();

        lastHealth = newHealth;
    }
}
