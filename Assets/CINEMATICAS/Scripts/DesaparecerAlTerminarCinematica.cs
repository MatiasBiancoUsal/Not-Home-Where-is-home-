using System.Collections;
using UnityEngine;

// ============================================================
//  DESAPARECER AL TERMINAR LA CINEMATICA
//
//  Va EN un objeto del mundo que tiene que dejar de existir una vez que la cinematica
//  termino. El caso para el que se hizo: el OSITO. Durante la cinematica la niña lo
//  encuentra y se lo lleva, asi que cuando volvemos al juego ya no tiene que estar
//  tirado en el piso.
//
//  Se acuerda de que ya desaparecio: si el jugador muere o vuelve a la zona, el objeto
//  no reaparece (seria raro encontrar el osito dos veces).
//
//  Como se conecta (dos formas, cualquiera sirve):
//    a) Arrastrar la cinematica al campo "Cinematica": se engancha solo.
//    b) En el evento "Al Terminar" de la CinematicaFrames, arrastrar este objeto y
//       elegir DesaparecerAlTerminarCinematica > Desaparecer().
//  Si se hacen las dos, no pasa nada: llamarlo dos veces no tiene efecto.
// ============================================================
public class DesaparecerAlTerminarCinematica : MonoBehaviour
{
    [Header("Que cinematica lo hace desaparecer")]
    [Tooltip("Se puede dejar vacio si preferis conectarlo a mano desde el evento 'Al Terminar' " +
             "de la cinematica.")]
    public CinematicaFrames cinematica;

    [Header("Desvanecido")]
    [Tooltip("Segundos que tarda en desaparecer. 0 = desaparece de golpe.")]
    public float duracionDesvanecido = 0.6f;
    [Tooltip("Segundos de espera antes de arrancar el desvanecido. Sirve para que no se esfume " +
             "en el mismo instante en que la pantalla vuelve del negro.")]
    public float esperaAntesDeDesvanecer = 0.2f;

    [Header("Si ya desaparecio en esta partida")]
    [Tooltip("Activo: una vez que desaparecio no vuelve nunca, ni al morir ni al volver a la zona.")]
    public bool recordarQueYaDesaparecio = true;
    [Tooltip("Con que nombre se recuerda. Dejalo VACIO y se arma solo con la escena y el nombre del " +
             "objeto. Solo hace falta llenarlo si tenés dos objetos con el mismo nombre en la escena.")]
    public string claveGuardado = "";

    [Header("Sonido (opcional)")]
    public AudioClip sonidoAlDesaparecer;
    [Range(0f, 1f)] public float volumen = 1f;

    private SpriteRenderer[] sprites;
    private bool yendose;
    private bool fue;

    private void Awake()
    {
        // Tambien los hijos: el objeto puede estar armado con varios sprites.
        sprites = GetComponentsInChildren<SpriteRenderer>(true);

        if (cinematica != null && cinematica.alTerminar != null)
        {
            cinematica.alTerminar.AddListener(Desaparecer);
        }
    }

    private void Start()
    {
        // Ya lo agarro en otra vuelta: no tiene que estar en la escena.
        if (recordarQueYaDesaparecio && ProgresoJuego.YaMostrado(Clave()))
        {
            DesaparecerDeUna();
        }
    }

    private void OnDestroy()
    {
        // Si no lo sacamos, el listener queda apuntando a un objeto destruido.
        if (cinematica != null && cinematica.alTerminar != null)
        {
            cinematica.alTerminar.RemoveListener(Desaparecer);
        }
    }

    // ---------- API ----------

    // La que se conecta al evento de la cinematica. Llamarla dos veces no hace nada.
    public void Desaparecer()
    {
        if (yendose || fue) return;

        yendose = true;

        if (recordarQueYaDesaparecio) ProgresoJuego.MarcarMostrado(Clave());

        if (sonidoAlDesaparecer != null)
        {
            AudioSource.PlayClipAtPoint(sonidoAlDesaparecer, transform.position, volumen);
        }

        StartCoroutine(RutinaDesvanecido());
    }

    // Lo saca al instante, sin animacion ni sonido.
    [ContextMenu("Hacerlo desaparecer ahora")]
    public void DesaparecerDeUna()
    {
        StopAllCoroutines();

        PonerAlpha(0f);
        yendose = false;
        fue = true;

        gameObject.SetActive(false);
    }

    // Lo vuelve a poner. Sirve para probar en el editor sin recargar la escena.
    [ContextMenu("Volver a ponerlo")]
    public void VolverAAparecer()
    {
        StopAllCoroutines();

        gameObject.SetActive(true);
        PonerAlpha(1f);

        yendose = false;
        fue = false;
    }

    // ---------- Interno ----------

    private IEnumerator RutinaDesvanecido()
    {
        // En tiempo REAL: la cinematica deja el timeScale en 0 hasta que restaura, y no
        // queremos que el desvanecido quede congelado esperando.
        if (esperaAntesDeDesvanecer > 0f)
        {
            yield return new WaitForSecondsRealtime(esperaAntesDeDesvanecer);
        }

        float t = 0f;
        float dur = Mathf.Max(0f, duracionDesvanecido);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            PonerAlpha(1f - Mathf.Clamp01(t / dur));
            yield return null;
        }

        PonerAlpha(0f);

        yendose = false;
        fue = true;

        // Apagado y no destruido, asi "Volver a ponerlo" sigue funcionando mientras probas.
        gameObject.SetActive(false);
    }

    private void PonerAlpha(float a)
    {
        if (sprites == null) return;

        foreach (SpriteRenderer sr in sprites)
        {
            if (sr == null) continue;

            Color c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }

    // Escena + nombre del objeto (no la posicion: el osito se puede mover mientras se
    // diseña el nivel, y no queremos que al moverlo se olvide de que ya lo agarraron).
    private string Clave()
    {
        if (!string.IsNullOrEmpty(claveGuardado)) return claveGuardado;

        return "Desaparecido@" + gameObject.scene.name + "@" + gameObject.name;
    }
}
