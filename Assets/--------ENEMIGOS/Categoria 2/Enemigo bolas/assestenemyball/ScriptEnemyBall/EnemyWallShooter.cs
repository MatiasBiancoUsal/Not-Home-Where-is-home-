using System.Collections;
using UnityEngine;

public class EnemyWallShooter : MonoBehaviour
{
    public Transform player;
    public GameObject ballPrefab;
    public Transform firePoint;

    [Header("Detección")]
    public float detectionRange = 5f;       // radio del rango de detección
    public Vector2 detectionOffset;         // corre el centro del rango respecto al enemigo
    public Color gizmoColor = Color.cyan;    // color del gizmo (editable desde el inspector)

    [Header("Disparo")]
    public float fireRate = 2f;

    private float nextFireTime;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null) return;

        Vector2 center = (Vector2)transform.position + detectionOffset;
        float distance = Vector2.Distance(center, player.position);

        if (distance <= detectionRange)
        {
            animator.SetBool("Shoot", true);

            if (Time.time >= nextFireTime)
            {
                StartCoroutine(ShootWithDelay());
                nextFireTime = Time.time + fireRate;
            }
        }
        else
        {
            animator.SetBool("Shoot", false);
        }
    }

    IEnumerator ShootWithDelay()
    {
        yield return new WaitForSeconds(0.4f);
        Shoot();
    }

    void Shoot()
    {
        GameObject ball = Instantiate(ballPrefab, firePoint.position, Quaternion.identity);

        EnemyBall enemyBall = ball.GetComponent<EnemyBall>();

        if (enemyBall != null)
        {
            Vector2 direction = player.position - firePoint.position;
            enemyBall.SetDirection(direction);
        }
    }

    // Dibuja el rango de detección en el editor (siempre visible) para verlo y ajustarlo.
    private void OnDrawGizmos()
    {
        Vector2 center = (Vector2)transform.position + detectionOffset;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(center, detectionRange);

        // si el player está asignado y dentro del rango, lo marca con una línea
        if (player != null && Vector2.Distance(center, player.position) <= detectionRange)
        {
            Gizmos.DrawLine(center, player.position);
        }
    }
}
