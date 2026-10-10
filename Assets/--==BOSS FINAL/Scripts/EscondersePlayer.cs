using UnityEngine;
using UnityEngine.InputSystem;

// ============================================================
//  ESCONDERSE (la respuesta a la embestida del boss)
//
//  Manteniendo CLICK DERECHO, la niña se agacha y se hunde en el piso: el boss le
//  pasa por encima sin tocarla. Al soltar, sale.
//
//  Solo se puede mientras la "ventana" esta abierta, que la abre y la cierra el
//  BossFinal durante la embestida.
//
//  NO hay que ponerlo a mano: el BossFinal se lo agrega solo a la niña y le pasa
//  los ajustes que tenga cargados en su Inspector.
// ============================================================
public class EscondersePlayer : MonoBehaviour
{
    // Lo mira el Damageable de la niña: escondida, los golpes no le entran.
    public static bool Escondida { get; private set; }

    // Con Reload Domain desactivado los static se arrastran entre sesiones de Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarStatics()
    {
        Escondida = false;
    }

    // ---- Ajustes (los carga el BossFinal) ----
    public bool ventanaAbierta;
    public float profundidad = 1.4f;
    public float duracion = 0.25f;
    [Tooltip("Valor de 'stateAnim' mientras esta escondida. 0 = no tocar la animacion.")]
    public int animAgachada = 0;
    public Color colorDelPolvo = new Color(0.6f, 0.55f, 0.5f);
    public AudioClip sonidoAlEsconderse;
    public AudioClip sonidoAlSalir;
    [Range(0f, 1f)] public float volumen = 0.8f;

    private enum Estado { Afuera, Metiendose, Adentro, Saliendo }

    private PlayerController pc;
    private SpriteRenderer sr;
    private Collider2D col;
    private Rigidbody2D rb;

    private Estado estado = Estado.Afuera;
    private Vector3 posicionDeSuperficie;
    private float t;
    private RigidbodyType2D tipoNormal;

    private void Awake()
    {
        pc = GetComponent<PlayerController>();
        sr = GetComponentInChildren<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnDisable()
    {
        // Si la escena se recarga o el boss muere mientras estaba escondida, la sacamos.
        if (estado != Estado.Afuera) Restaurar();
    }

    private void Update()
    {
        bool aprieta = ventanaAbierta && Mouse.current != null && Mouse.current.rightButton.isPressed;

        switch (estado)
        {
            case Estado.Afuera:
                if (aprieta && EnElPiso()) Meterse();
                break;

            case Estado.Metiendose:
                Avanzar(1f);
                if (t >= 1f) estado = Estado.Adentro;
                if (!aprieta) estado = Estado.Saliendo;
                break;

            case Estado.Adentro:
                if (!aprieta || !ventanaAbierta) estado = Estado.Saliendo;
                break;

            case Estado.Saliendo:
                Avanzar(-1f);
                if (t <= 0f) Salir();
                else if (aprieta && ventanaAbierta) estado = Estado.Metiendose;
                break;
        }
    }

    private bool EnElPiso()
    {
        return pc != null && pc.jump != null && pc.jump.IsGrounded;
    }

    private void Meterse()
    {
        estado = Estado.Metiendose;
        t = 0f;
        Escondida = true;

        posicionDeSuperficie = transform.position;

        // Le sacamos el control y la fisica: se hunde a mano, sin chocar con el piso.
        if (pc != null)
        {
            pc.enabled = false;
            if (animAgachada != 0 && pc.animPlayer != null) pc.animPlayer.SetInteger("stateAnim", animAgachada);
        }
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            tipoNormal = rb.bodyType;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        if (col != null) col.enabled = false;   // que no la toquen las estalactitas ni el boss

        EfectoPuf.Crear(transform.position, colorDelPolvo, 8, 0.18f, 3f);
        UtilBoss.Sonar(sonidoAlEsconderse, volumen, transform.position);
    }

    // Avanza (o retrocede) el hundido y acompaña con la transparencia.
    private void Avanzar(float direccion)
    {
        t = Mathf.Clamp01(t + direccion * Time.deltaTime / Mathf.Max(0.01f, duracion));

        float suave = Mathf.SmoothStep(0f, 1f, t);
        transform.position = posicionDeSuperficie - new Vector3(0f, profundidad * suave, 0f);

        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(1f, 0.15f, suave);
            sr.color = c;
        }
    }

    private void Salir()
    {
        Restaurar();
        EfectoPuf.Crear(transform.position, colorDelPolvo, 6, 0.16f, 2.5f);
        UtilBoss.Sonar(sonidoAlSalir, volumen, transform.position);
    }

    private void Restaurar()
    {
        estado = Estado.Afuera;
        t = 0f;
        Escondida = false;

        transform.position = posicionDeSuperficie;

        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
        if (col != null) col.enabled = true;
        if (rb != null) rb.bodyType = tipoNormal == 0 ? RigidbodyType2D.Dynamic : tipoNormal;
        if (pc != null) pc.enabled = true;
    }

    // La llama el BossFinal al terminar la pelea, por las dudas.
    public void SacarDelEscondite()
    {
        ventanaAbierta = false;
        if (estado != Estado.Afuera) Salir();
    }
}
