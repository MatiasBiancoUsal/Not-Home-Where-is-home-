using UnityEngine;

public class PlataformaMovil : MonoBehaviour
{
    [Header("Movimiento")]
    public Transform puntoA;
    public Transform puntoB;
    public float velocidad = 3f;
    public float esperaEnExtremos = 0.5f;

    private Vector3 destino;
    private float timerEspera = 0f;
    private bool esperando = false;

    void Start()
    {
        if (puntoA == null || puntoB == null)
        {
            Debug.LogError("PlataformaMovil: asigná puntoA y puntoB en el Inspector");
            return;
        }
        destino = puntoB.position;
    }

    void Update()
    {
        if (puntoA == null || puntoB == null) return;

        if (esperando)
        {
            timerEspera -= Time.deltaTime;
            if (timerEspera <= 0f)
                esperando = false;
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);

        if (Vector3.Distance(transform.position, destino) < 0.05f)
        {
            destino = destino == puntoA.position ? puntoB.position : puntoA.position;
            timerEspera = esperaEnExtremos;
            esperando = true;
        }
    }

    // Esto hace que el jugador se mueva con la plataforma
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
            col.transform.SetParent(transform);
    }

    void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
            col.transform.SetParent(null);
    }
}