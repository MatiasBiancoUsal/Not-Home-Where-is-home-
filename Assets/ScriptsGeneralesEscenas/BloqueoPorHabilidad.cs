using System.Collections;
using UnityEngine;

// ============================================================
//  BLOQUEO POR HABILIDAD
//
//  Un pedazo de escenario SOLIDO que tapa una salida hasta que la niña consigue una
//  habilidad. El caso para el que se hizo: en la Zona 2 la niña se cae a un pozo del
//  que solo se sale TREPANDO. Si todavia no tiene esa habilidad, queda encerrada y hay
//  que reiniciar. Con este bloqueo, el pozo esta tapado hasta que consigue trepar.
//
//  Cuando la consigue, el bloqueo se desvanece y deja el paso libre. Al volver a la
//  zona (o al morir) ya arranca abierto, sin repetir la animacion.
//
//  COMO SE USA
//    1. Un objeto con SpriteRenderer y un Collider2D SOLIDO (Is Trigger DESTILDADO).
//    2. Si hace de piso o de pared, ponelo en el layer "Ground", si no la niña lo
//       atraviesa.
//    3. Agregale este script y elegi la habilidad en "Habilidad Que Lo Abre".
//
//  No hay que conectarlo a nada mas: el solo se fija si la niña ya tiene la habilidad.
// ============================================================
public class BloqueoPorHabilidad : MonoBehaviour
{
    [Header("Que lo abre")]
    [Tooltip("Cuando la niña consigue ESTA habilidad, el bloqueo desaparece y libera el paso.")]
    public PlayerController.Habilidad habilidadQueLoAbre = PlayerController.Habilidad.Escalar;

    [Header("Desvanecido")]
    [Tooltip("Segundos que tarda en desaparecer. 0 = desaparece de golpe.")]
    [Min(0f)] public float duracionDesvanecido = 0.8f;
    [Tooltip("Activo: el paso se libera APENAS empieza el desvanecido (la niña puede pasar mientras " +
             "todavia se ve medio transparente).\n\n" +
             "Desactivo: se libera recien cuando termino de desaparecer del todo.")]
    public bool liberarElPasoAlEmpezar = false;
    [Tooltip("Segundos de espera antes de arrancar el desvanecido. Sirve para que no se esfume en " +
             "el mismo instante en que se cierra el cartel de la habilidad.")]
    [Min(0f)] public float esperaAntesDeDesvanecer = 0.4f;

    [Header("Aviso en pantalla (opcional)")]
    [Tooltip("Cartelito que aparece abajo cuando se abre el paso. Vacio = no muestra nada.")]
    public string mensajeAlAbrirse = "se abrio un camino nuevo";

    [Header("Sonido (opcional)")]
    public AudioClip sonidoAlAbrirse;
    [Range(0f, 1f)] public float volumen = 1f;

    private SpriteRenderer[] sprites;
    private Collider2D[] colliders;
    private PlayerController jugador;

    private bool abriendo;
    private bool abierto;

    // ¿El paso ya esta libre?
    public bool PasoLibre => abierto;

    private void Awake()
    {
        // Tambien los hijos: el bloqueo puede estar armado con varios sprites.
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void Start()
    {
        jugador = FindFirstObjectByType<PlayerController>();

        if (jugador == null)
        {
            Debug.LogWarning("BloqueoPorHabilidad: no encontre a la niña en la escena. El bloqueo " +
                             "se queda cerrado.", this);
            return;
        }

        // Ya tenia la habilidad al entrar a la zona: el paso arranca libre, sin animacion
        // ni sonido (el momento ya paso en otra vuelta).
        if (jugador.TieneHabilidad(habilidadQueLoAbre))
        {
            AbrirDeUna();
        }
    }

    // Se fija si la niña consiguio la habilidad. Es una comparacion de un bool por frame:
    // no cuesta nada, y asi el bloqueo se abre en el momento exacto en que la agarra, sin
    // tener que conectar nada en el Inspector.
    private void Update()
    {
        if (abierto || abriendo || jugador == null) return;

        if (jugador.TieneHabilidad(habilidadQueLoAbre))
        {
            AbrirPaso();
        }
    }

    // ---------- API ----------

    // Libera el paso con el desvanecido. Llamarla dos veces no hace nada.
    [ContextMenu("Abrir el paso ahora")]
    public void AbrirPaso()
    {
        if (abriendo || abierto) return;

        abriendo = true;
        StartCoroutine(RutinaDeApertura());
    }

    // Lo saca al instante, sin animacion ni sonido.
    public void AbrirDeUna()
    {
        StopAllCoroutines();

        PonerAlpha(0f);
        LiberarElPaso();

        abriendo = false;
        abierto = true;

        gameObject.SetActive(false);
    }

    // Lo vuelve a poner. Sirve para probar en el editor sin recargar la escena.
    [ContextMenu("Volver a cerrarlo")]
    public void VolverACerrar()
    {
        StopAllCoroutines();

        gameObject.SetActive(true);
        PonerAlpha(1f);

        foreach (Collider2D col in colliders)
        {
            if (col != null) col.enabled = true;
        }

        abriendo = false;
        abierto = false;
    }

    // ---------- Interno ----------

    private IEnumerator RutinaDeApertura()
    {
        if (esperaAntesDeDesvanecer > 0f)
        {
            yield return new WaitForSeconds(esperaAntesDeDesvanecer);
        }

        if (sonidoAlAbrirse != null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(sonidoAlAbrirse, volumen);
            else AudioSource.PlayClipAtPoint(sonidoAlAbrirse, transform.position, volumen);
        }

        AvisoEnPantalla.Mostrar(mensajeAlAbrirse);

        if (liberarElPasoAlEmpezar) LiberarElPaso();

        float t = 0f;
        float dur = Mathf.Max(0f, duracionDesvanecido);

        while (t < dur)
        {
            t += Time.deltaTime;
            PonerAlpha(1f - Mathf.Clamp01(t / dur));
            yield return null;
        }

        PonerAlpha(0f);
        LiberarElPaso();

        abriendo = false;
        abierto = true;

        // Apagado y no destruido, asi "Volver a cerrarlo" sigue funcionando mientras probas.
        gameObject.SetActive(false);
    }

    private void LiberarElPaso()
    {
        foreach (Collider2D col in colliders)
        {
            if (col != null) col.enabled = false;
        }
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
}
