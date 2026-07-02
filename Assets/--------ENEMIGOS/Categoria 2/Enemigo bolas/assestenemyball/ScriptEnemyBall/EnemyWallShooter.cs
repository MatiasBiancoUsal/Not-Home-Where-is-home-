using System.Collections;
using UnityEngine;

public class EnemyWallShooter : MonoBehaviour
{
    public GameObject ballPrefab;
    public Transform firePoint;

    [Header("Detección")]
    public float detectionRange = 5f;       // radio del rango de detección
    public Vector2 detectionOffset;         // corre el centro del rango respecto al enemigo
    [Tooltip("Tag del player. El enemigo lo busca SOLO en la escena, no hace falta asignarlo a mano.")]
    public string playerTag = "Player";
    public Color gizmoColor = Color.cyan;    // color del gizmo (editable desde el inspector)

    [Header("Disparo")]
    public float fireRate = 2f;

    private Transform player;                // se busca solo por tag (no se asigna en el inspector)
    private float nextFireTime;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        BuscarPlayer();
    }

    void Update()
    {
        // si no tenemos al player (o se recreo al reaparecer), lo volvemos a buscar por tag.
        if (player == null)
        {
            BuscarPlayer();
            if (player == null) return;
        }

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

    // Busca al player en la escena por su tag.
    private void BuscarPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null) player = playerObj.transform;
    }

    IEnumerator ShootWithDelay()
    {
        yield return new WaitForSeconds(0.4f);
        Shoot();
    }

    void Shoot()
    {
        if (player == null) return; // por si el player desaparecio justo en el delay

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

        // si el player ya esta referenciado (en Play) y dentro del rango, lo marca con una línea
        if (player != null && Vector2.Distance(center, player.position) <= detectionRange)
        {
            Gizmos.DrawLine(center, player.position);
        }
    }
}
