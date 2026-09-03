using System.Collections;
using UnityEngine;

// ============================================================
//  BLOQUEO TEMPORAL (suelo o pared que guia el camino)
//
//  Un pedazo de escenario SOLIDO que tapa un camino, para que la niña no tenga otra
//  opcion que ir por donde queremos (por ejemplo, hacia el orbe y su cinematica).
//  No empuja a nadie ni le saca los controles: el jugador camina libre, simplemente
//  el otro camino todavia no existe. Por eso no se percata de que lo estan guiando.
//
//  Cuando termina la cinematica, el bloqueo se desvanece y deja el paso libre.
//
//  Va en un objeto con SpriteRenderer y un Collider2D SOLIDO (Is Trigger DESTILDADO).
//  Si hace de piso, ademas tiene que estar en el layer que la niña usa como suelo.
//
//  Como se conecta con la cinematica (dos formas, cualquiera sirve):
//    a) Arrastrar la cinematica al campo "Cinematica" de abajo: se engancha solo.
//    b) En el evento "Al Terminar" de la CinematicaFrames, arrastrar este objeto
//       y elegir BloqueoTemporal > AbrirPaso().
//  Si se hacen las dos, no pasa nada: abrir dos veces no tiene efecto.
// ============================================================
public class BloqueoTemporal : MonoBehaviour
{
    [Header("Cuando se abre")]
    [Tooltip("La cinematica que, al terminar, libera el paso. Se puede dejar vacio si preferis " +
             "conectarlo a mano desde el evento 'Al Terminar' de la cinematica.")]
    public CinematicaFrames cinematica;

    [Header("Desvanecido")]
    [Tooltip("Segundos que tarda en desaparecer. 0 = desaparece de golpe.")]
    public float duracionDesvanecido = 0.8f;
    [Tooltip("Activo: el paso se libera APENAS empieza el desvanecido (la niña puede pasar mientras " +
             "todavia se ve medio transparente).\n\n" +
             "Desactivo: el paso se libera recien cuando termino de desaparecer del todo.")]
    public bool liberarElPasoAlEmpezar = false;
    [Tooltip("Segundos de espera antes de arrancar el desvanecido, por si querés que la camara " +
             "vuelva del cartel de la cinematica antes de que el escenario cambie.")]
    public float esperaAntesDeDesvanecer = 0f;

    [Header("Si ya se abrio en esta partida")]
    [Tooltip("Activo: una vez abierto queda abierto para siempre. Al volver a la zona (o al morir) " +
             "el bloqueo ya no esta, asi no se repite el momento guionado.")]
    public bool recordarQueYaSeAbrio = true;
    [Tooltip("Con que nombre se recuerda. Dejalo VACIO y se arma solo con la escena y el nombre del " +
             "objeto. Solo hace falta llenarlo si tenés dos bloqueos con el mismo nombre.")]
    public string claveGuardado = "";

    [Header("Sonido (opcional)")]
    public AudioClip sonidoAlAbrirse;
    [Range(0f, 1f)] public float volumen = 1f;

    private SpriteRenderer[] sprites;
    private Collider2D[] colliders;
    private bool abriendo;
    private bool abierto;

    // ¿El paso ya esta libre?
    public bool PasoLibre => abierto;

    private void Awake()
    {
        // Agarramos tambien los hijos: el bloqueo puede estar armado con varios sprites.
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);

        // Enganche automatico al final de la cinematica.
        if (cinematica != null && cinematica.alTerminar != null)
        {
            cinematica.alTerminar.AddListener(AbrirPaso);
        }
    }

    private void Start()
    {
        // Ya lo abrimos en otra vuelta: el paso arranca libre, sin desvanecido ni sonido.
        if (recordarQueYaSeAbrio && ProgresoJuego.YaMostrado(Clave()))
        {
            AbrirDeUna();
        }
    }

    private void OnDestroy()
    {
        // Si no lo sacamos, el listener queda apuntando a un objeto destruido.
        if (cinematica != null && cinematica.alTerminar != null)
        {
            cinematica.alTerminar.RemoveListener(AbrirPaso);
        }
    }

    // ---------- API ----------

    // Libera el paso con el desvanecido. Es la que se conecta al evento de la cinematica.
    // Llamarla dos veces no hace nada: por eso da igual engancharla por codigo y a mano.
    public void AbrirPaso()
    {
        if (abriendo || abierto) return;

        abriendo = true;

        if (recordarQueYaSeAbrio) ProgresoJuego.MarcarMostrado(Clave());

        if (sonidoAlAbrirse != null)
        {
            AudioSource.PlayClipAtPoint(sonidoAlAbrirse, transform.position, volumen);
        }

        StartCoroutine(RutinaDesvanecido());
    }

    // Saca el bloqueo al instante, sin animacion ni sonido.
    [ContextMenu("Abrir el paso ahora")]
    public void AbrirDeUna()
    {
        StopAllCoroutines();

        PonerColliders(false);
        PonerAlpha(0f);

        abriendo = false;
        abierto = true;

        gameObject.SetActive(false);
    }

    // Vuelve a poner el bloqueo. Sirve para probar en el editor sin recargar la escena.
    [ContextMenu("Volver a cerrar el paso")]
    public void CerrarPaso()
    {
        StopAllCoroutines();

        gameObject.SetActive(true);
        PonerColliders(true);
        PonerAlpha(1f);

        abriendo = false;
        abierto = false;
    }

    // ---------- Interno ----------

    private IEnumerator RutinaDesvanecido()
    {
        if (esperaAntesDeDesvanecer > 0f)
        {
            yield return new WaitForSeconds(esperaAntesDeDesvanecer);
        }

        // Si el paso se libera al principio, la niña ya puede cruzar mientras se desvanece.
        if (liberarElPasoAlEmpezar) PonerColliders(false);

        float t = 0f;
        float dur = Mathf.Max(0f, duracionDesvanecido);

        while (t < dur)
        {
            t += Time.deltaTime;
            PonerAlpha(1f - Mathf.Clamp01(t / dur));
            yield return null;
        }

        PonerAlpha(0f);
        PonerColliders(false);

        abriendo = false;
        abierto = true;

        // Apagado y no destruido: asi "Volver a cerrar el paso" sigue funcionando
        // mientras probas en el editor.
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

    private void PonerColliders(bool activos)
    {
        if (colliders == null) return;

        foreach (Collider2D col in colliders)
        {
            if (col != null) col.enabled = activos;
        }
    }

    // Escena + nombre del objeto. No usamos la posicion (como hacen las monedas) porque
    // un bloqueo se puede mover mientras se diseña el nivel, y no queremos que al moverlo
    // se olvide de que ya lo habian abierto.
    private string Clave()
    {
        if (!string.IsNullOrEmpty(claveGuardado)) return claveGuardado;

        return "Bloqueo@" + gameObject.scene.name + "@" + gameObject.name;
    }
}
