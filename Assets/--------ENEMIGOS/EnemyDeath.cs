using System.Collections;
using UnityEngine;

[RequireComponent(typeof(HealthHandler))]
public class EnemyDeath : MonoBehaviour
{
    [Header("Animacion de muerte")]
    [Tooltip("Nombre EXACTO del trigger que creaste en el Animator para la muerte.")]
    public string triggerMuerte = "Die";
    [Tooltip("Cuanto dura la animacion de muerte antes de que el enemigo desaparezca.")]
    public float duracionAnimMuerte = 1f;

    [Header("Puntos")]
    public int puntosPorMatar = 10;

    private Animator animator;
    private HealthHandler healthHandler;
    private Rigidbody2D rb;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        healthHandler = GetComponent<HealthHandler>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (healthHandler != null) healthHandler.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (healthHandler != null) healthHandler.OnDeath -= HandleDeath;
    }

    private void Start()
    {
        if (healthHandler != null) healthHandler.destroyOnDeath = false;
    }

    private void HandleDeath()
    {
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // 1) Frenar al enemigo: que no patrulle, no dañe ni se mueva mientras "muere".
        EnemyPatrol patrol = GetComponent<EnemyPatrol>();
        if (patrol != null) patrol.enabled = false;

        HurtBox hurtBox = GetComponentInChildren<HurtBox>();
        if (hurtBox != null) hurtBox.enabled = false;

        if (rb != null) rb.linearVelocity = Vector2.zero;

        // 2) Reproducir la animacion de muerte.
        if (animator != null) animator.SetTrigger(triggerMuerte);

        // 3) Esperar a que se vea y desaparecer.
        yield return new WaitForSeconds(duracionAnimMuerte);

        // 4) Sumar puntos.
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddPoints(puntosPorMatar);

        Destroy(gameObject);
    }
}