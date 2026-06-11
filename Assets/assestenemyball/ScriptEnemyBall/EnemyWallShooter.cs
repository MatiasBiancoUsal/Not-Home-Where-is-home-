using System.Collections;
using UnityEngine;

public class EnemyWallShooter : MonoBehaviour
{
    public Transform player;
    public GameObject ballPrefab;
    public Transform firePoint;

    public float detectionRange = 5f;
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

        float distance = Vector2.Distance(transform.position, player.position);

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
}