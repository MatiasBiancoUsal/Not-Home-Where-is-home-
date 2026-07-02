using UnityEngine;

// La pared sombra ya NO te mata al toque: te va quitando vida de a poco
// y te empuja hacia adelante (para que avances y no te quedes atrapado).
// El que te mata es el sistema de vidas normal cuando se te acaban.
public class ParedSombraKill : MonoBehaviour
{
    [Header("Daño")]
    [Tooltip("Cuanta vida te quita cada vez.")]
    public int danio = 1;
    [Tooltip("Cada cuantos segundos te quita vida mientras te toca.")]
    public float intervaloDanio = 0.6f;

    [Header("Empuje")]
    [Tooltip("Que tan fuerte te empuja para avanzar. Si corrés más rápido que esto, podés escapar.")]
    public float fuerzaEmpuje = 7f;

    private float timerDanio = 0f;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // EMPUJE: te barre hacia adelante (lejos de la pared). Si ya te alejás más rápido
        // que el empuje, no te frenamos -> podés escapar corriendo.
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb != null)
        {
            float dirX = Mathf.Sign(other.transform.position.x - transform.position.x);
            float velAlejandose = rb.linearVelocity.x * dirX;
            if (velAlejandose < fuerzaEmpuje)
                rb.linearVelocity = new Vector2(dirX * fuerzaEmpuje, rb.linearVelocity.y);
        }

        // DAÑO de a poco: una vez cada "intervaloDanio", no cada frame.
        timerDanio -= Time.deltaTime;
        if (timerDanio <= 0f)
        {
            HealthHandler hh = other.GetComponent<HealthHandler>();
            if (hh != null) hh.TakeDamage(danio);
            timerDanio = intervaloDanio;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) timerDanio = 0f; // reinicia para el proximo contacto
    }
}
