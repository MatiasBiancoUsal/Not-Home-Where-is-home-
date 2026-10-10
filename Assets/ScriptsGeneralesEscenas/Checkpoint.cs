using System.Collections;
using UnityEngine;

// ============================================================
//  CHECKPOINT
//  Cuando la niña lo toca, pasa a ser el lugar donde reaparece si muere.
//
//  Se agrega SOLO a los objetos que se llaman "CHECKPOINT ..." (ver PuntoDeReaparicion).
//  Tambien se puede poner a mano en cualquier objeto.
//
//  Ya no bloquea el paso: sus colliders pasan a ser trigger y se mueve al layer
//  "Ignore Raycast". Antes eran bloques solidos (y alguno con el tag Wall), y la niña
//  se quedaba trabada contra ellos o se pegaba para trepar.
// ============================================================
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Sonido al activarlo (opcional). Hay uno en Assets/Sonidos/Player/HintsStarsLite/Checkpoint 2.")]
    public AudioClip sonidoAlActivar;
    [Range(0f, 1f)] public float volumen = 0.7f;
    [Tooltip("Distancia maxima para buscar el piso debajo del checkpoint.")]
    public float distanciaAlPiso = 15f;

    [Header("Espacio durante la animacion")]
    [Tooltip("Collider fisico que impide atravesar la fuente mientras se activa.")]
    [SerializeField] private Collider2D bloqueoDuranteAnimacion;
    [Tooltip("Debe coincidir con la duracion del clip Inicio Fuente.")]
    [Min(0f)] [SerializeField] private float duracionBloqueo = 1.1f;
    [Tooltip("Pequeño impulso que saca al player del centro de la fuente.")]
    [Min(0f)] [SerializeField] private float fuerzaSeparacion = 3f;

    private Animator animator;
    private ParticleSystem particulasActivacion;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        DesactivarBloqueo();
        PrepararParticulasExistentes();

        Collider2D[] colliders = GetComponents<Collider2D>();
        if (colliders.Length == 0)
        {
            colliders = new Collider2D[] { gameObject.AddComponent<BoxCollider2D>() };
        }

        foreach (Collider2D c in colliders)
        {
            c.isTrigger = true;
        }

        // Si quedara en el layer del piso, la niña "pisaria" el trigger y podria saltar en el aire.
        int ignorar = LayerMask.NameToLayer("Ignore Raycast");
        gameObject.layer = ignorar >= 0 ? ignorar : 2;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) return;

        Vector3 donde = LugarDeReaparicion(pc);
        string escena = gameObject.scene.name;

        if (PuntoDeReaparicion.EsElActual(escena, donde)) return;

        PuntoDeReaparicion.Guardar(escena, donde);

        ActivarBloqueo(pc);

        if (animator != null)
        {
            animator.SetTrigger("Activar");
        }

        if (particulasActivacion != null)
        {
            particulasActivacion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particulasActivacion.Play();
        }

        if (sonidoAlActivar != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(sonidoAlActivar, volumen);
        }

        StartCoroutine(DesactivarBloqueoDespues());
    }

    private void ActivarBloqueo(PlayerController pc)
    {
        if (bloqueoDuranteAnimacion != null)
        {
            bloqueoDuranteAnimacion.enabled = true;
        }

        if (pc == null || pc.rb == null || fuerzaSeparacion <= 0f) return;

        float direccion = Mathf.Sign(pc.transform.position.x - transform.position.x);
        if (Mathf.Approximately(direccion, 0f))
        {
            direccion = pc.movement != null && pc.movement.IsFacingRight ? 1f : -1f;
        }

        Vector2 velocidad = pc.rb.linearVelocity;
        velocidad.x = direccion * fuerzaSeparacion;
        pc.rb.linearVelocity = velocidad;
    }

    private IEnumerator DesactivarBloqueoDespues()
    {
        yield return new WaitForSecondsRealtime(duracionBloqueo);
        DesactivarBloqueo();
    }

    private void DesactivarBloqueo()
    {
        if (bloqueoDuranteAnimacion != null)
        {
            bloqueoDuranteAnimacion.enabled = false;
        }
    }

    private void PrepararParticulasExistentes()
    {
        particulasActivacion = GetComponentInChildren<ParticleSystem>(true);
        if (particulasActivacion == null) return;

        particulasActivacion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particulasActivacion.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.8f;
        main.startDelay = 0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.12f);
        main.startColor = Color.white;
        main.gravityModifier = -0.06f;
        main.simulationSpeed = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.maxParticles = 40;

        ParticleSystem.EmissionModule emission = particulasActivacion.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 18)
        });

        ParticleSystem.ShapeModule shape = particulasActivacion.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.65f;
        shape.radiusThickness = 1f;

        ParticleSystem.ColorOverLifetimeModule colorVida = particulasActivacion.colorOverLifetime;
        colorVida.enabled = true;
        Gradient brillo = new Gradient();
        brillo.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(0f, 1f)
            });
        colorVida.color = brillo;

        ParticleSystem.SizeOverLifetimeModule tamanoVida = particulasActivacion.sizeOverLifetime;
        tamanoVida.enabled = true;
        tamanoVida.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.15f),
                new Keyframe(0.18f, 1f),
                new Keyframe(1f, 0f)));

        ParticleSystemRenderer rendererParticulas = particulasActivacion.GetComponent<ParticleSystemRenderer>();
        SpriteRenderer rendererFuente = GetComponent<SpriteRenderer>();
        if (rendererParticulas != null && rendererFuente != null)
        {
            rendererParticulas.sortingLayerID = rendererFuente.sortingLayerID;
            rendererParticulas.sortingOrder = rendererFuente.sortingOrder + 1;
        }
    }

    private void OnDisable()
    {
        DesactivarBloqueo();
    }

    // Parada en el piso, debajo del checkpoint. Si no hay piso, donde esta la niña ahora.
    private Vector3 LugarDeReaparicion(PlayerController pc)
    {
        Collider2D propio = GetComponent<Collider2D>();
        Vector2 origen = propio != null ? (Vector2)propio.bounds.center : (Vector2)transform.position;
        return PuntoDeReaparicion.EnElPiso(origen, pc, distanciaAlPiso);
    }
}
