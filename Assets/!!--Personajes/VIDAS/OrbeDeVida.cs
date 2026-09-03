using System.Collections;
using UnityEngine;

// ============================================================
//  ORBE DE VIDA
//  Un objeto del mundo que devuelve vida al tocarlo. Por defecto vale UN CORAZON
//  (2 puntos de vida, porque cada corazon del HUD son 2).
//
//  Funciona igual que las monedas de puntos: tiene su animacion propia mientras
//  espera, y al agarrarlo reproduce la animacion de recolectado antes de desaparecer.
//
//  Va en el objeto que tiene el Collider2D marcado como Is Trigger.
//  Necesita un Animator con un trigger de recolectado (el mismo "Obtained" que usan
//  las monedas).
// ============================================================
public class OrbeDeVida : MonoBehaviour
{
    [Header("Cuanto cura")]
    [Tooltip("Puntos de vida que devuelve. Cada corazon del HUD son 2 puntos, asi que 2 = un corazon entero.")]
    public int curacion = 2;
    [Tooltip("Tag del jugador. Solo el objeto con este tag puede levantarlo.")]
    public string tagJugador = "Player";

    [Header("Cuando la vida ya esta llena")]
    [Tooltip("Activo (recomendado): si la niña esta con la vida al maximo NO lo levanta, y el orbe " +
             "queda en el mapa para cuando de verdad lo necesite.\n\n" +
             "Desactivo: lo levanta igual y la curacion se desperdicia.")]
    public bool soloSiFaltaVida = true;

    [Header("Animacion de recolectado")]
    [Tooltip("Nombre EXACTO del trigger que creaste en el Animator para la animacion de recolectado. " +
             "Es el mismo que usan las monedas de puntos.")]
    public string triggerRecolectado = "Obtained";
    [Tooltip("Cuanto dura esa animacion antes de que el orbe desaparezca.")]
    public float duracionAnimRecolectado = 0.52f;

    [Header("Si vuelve a aparecer")]
    [Tooltip("Desactivo (como las monedas): una vez agarrado no vuelve a aparecer nunca, ni al morir " +
             "ni al volver a entrar a la zona.\n\n" +
             "Activo: reaparece cada vez que se carga la zona. Sirve para hacer un punto de curacion " +
             "al que el jugador pueda volver.")]
    public bool reapareceAlVolverALaZona = false;

    [Header("Sonido (opcional)")]
    [Tooltip("Se puede dejar vacio. Suena en la posicion del orbe, asi no se corta al desaparecer.")]
    public AudioClip sonidoRecolectado;
    [Range(0f, 1f)] public float volumen = 1f;

    private string id;
    private bool recolectado;

    private void Start()
    {
        // ID unico de ESTE orbe: escena + posicion. Lleva el prefijo "Vida" para no
        // pisarse con el id de una moneda que estuviera en el mismo lugar.
        id = "Vida@" + gameObject.scene.name + "@" +
             transform.position.x.ToString("F1") + "," + transform.position.y.ToString("F1");

        // Ya lo agarramos en otra vuelta: no debe reaparecer.
        if (!reapareceAlVolverALaZona && ProgresoJuego.YaMostrado(id))
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (recolectado) return;
        if (!other.CompareTag(tagJugador)) return;

        // El collider que entra puede ser un hijo de la niña, por eso miramos tambien
        // hacia arriba en la jerarquia.
        HealthHandler vida = other.GetComponent<HealthHandler>();
        if (vida == null) vida = other.GetComponentInParent<HealthHandler>();

        if (vida == null)
        {
            Debug.LogWarning("OrbeDeVida: el objeto con el tag '" + tagJugador + "' no tiene HealthHandler. " +
                             "El orbe no puede curar.", other);
            return;
        }

        // Vida llena: lo dejamos en el mapa en vez de desperdiciarlo.
        if (soloSiFaltaVida && vida.VidaQueFalta <= 0) return;

        int curado = vida.Heal(curacion);

        // Heal devuelve 0 si no habia nada que curar (o si esta muerta). En ese caso
        // tampoco lo consumimos.
        if (curado <= 0) return;

        recolectado = true;

        if (!reapareceAlVolverALaZona) ProgresoJuego.MarcarMostrado(id);

        if (sonidoRecolectado != null)
        {
            // En la posicion del orbe y no con un AudioSource propio: el objeto se
            // destruye enseguida y cortaria el sonido a la mitad.
            AudioSource.PlayClipAtPoint(sonidoRecolectado, transform.position, volumen);
        }

        StartCoroutine(SecuenciaRecolectado());
    }

    private IEnumerator SecuenciaRecolectado()
    {
        // Que no se pueda volver a tocar mientras corre la animacion.
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            col.enabled = false;
        }

        // El Animator puede estar en este objeto o en un hijo.
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null && !string.IsNullOrEmpty(triggerRecolectado))
        {
            anim.SetTrigger(triggerRecolectado);
        }

        yield return new WaitForSeconds(duracionAnimRecolectado);

        Destroy(gameObject);
    }
}
