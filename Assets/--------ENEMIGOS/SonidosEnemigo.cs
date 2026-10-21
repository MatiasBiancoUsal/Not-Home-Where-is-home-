using UnityEngine;

// ============================================================
//  SONIDOS DE UN ENEMIGO
//  Se agrega a cualquier monstruo que tenga HealthHandler (la vida) y le da:
//    - IDLE:   un sonido cada tantos segundos mientras esta vivo, solo si la niña
//              esta cerca (asi no suenan todos los monstruos del mapa a la vez).
//    - DAÑO:   cuando le pegan y no se muere.
//    - MUERTE: cuando se muere.
//  Todos los sonidos son opcionales: el que quede vacio, no suena.
//  Suenan por el canal de EFECTOS, asi los regula el slider de Sonidos de Opciones.
// ============================================================
public class SonidosEnemigo : MonoBehaviour
{
    [Header("IDLE (mientras esta vivo)")]
    [Tooltip("Uno o varios sonidos. Si pones varios, elige uno distinto al azar cada vez.")]
    public AudioClip[] sonidosIdle;
    [Tooltip("Segundos minimos entre un sonido de idle y el siguiente.")]
    public float esperaMinimaIdle = 3f;
    [Tooltip("Segundos maximos entre un sonido de idle y el siguiente.")]
    public float esperaMaximaIdle = 7f;
    [Tooltip("Solo suena si la niña esta a menos de esta distancia. Mas cerca = mas fuerte.")]
    public float distanciaParaEscuchar = 12f;
    [Range(0f, 1f)] public float volumenIdle = 0.6f;

    [Header("DAÑO (cuando le pegan)")]
    public AudioClip[] sonidosDanio;
    [Range(0f, 1f)] public float volumenDanio = 0.8f;

    [Header("MUERTE")]
    public AudioClip sonidoMuerte;
    [Range(0f, 1f)] public float volumenMuerte = 0.9f;

    [Header("Variacion")]
    [Tooltip("Cambia un poquito el tono cada vez, asi no suena siempre identico.")]
    public bool variarTono = true;

    private AudioSource fuente;
    private HealthHandler vida;
    private Transform nina;
    private int vidaAnterior;
    private float proximoIdle;
    private bool muerto;

    private void Awake()
    {
        vida = GetComponent<HealthHandler>();
        if (vida == null) vida = GetComponentInParent<HealthHandler>();

        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        if (vida == null) return;
        vida.OnHealthChanged += AlCambiarLaVida;
        vida.OnDeath += AlMorir;
    }

    private void OnDisable()
    {
        if (vida == null) return;
        vida.OnHealthChanged -= AlCambiarLaVida;
        vida.OnDeath -= AlMorir;
    }

    private void Start()
    {
        if (vida == null)
        {
            Debug.LogWarning("SonidosEnemigo: '" + name + "' no tiene HealthHandler. Solo va a sonar el idle.", this);
        }
        else
        {
            vidaAnterior = vida.CurrentHealth;
        }

        // Mismo canal que los efectos del juego (slider de Sonidos).
        if (AudioManager.Instance != null && AudioManager.Instance.sfxSource != null)
        {
            fuente.outputAudioMixerGroup = AudioManager.Instance.sfxSource.outputAudioMixerGroup;
        }

        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null) nina = pc.transform;

        ProgramarIdle();
    }

    private void Update()
    {
        if (muerto || sonidosIdle == null || sonidosIdle.Length == 0) return;
        if (Time.time < proximoIdle) return;

        ProgramarIdle();

        if (nina == null) return;
        float distancia = Vector2.Distance(nina.position, transform.position);
        if (distancia > distanciaParaEscuchar) return;

        // Mas cerca, mas fuerte.
        float cercania = 1f - distancia / Mathf.Max(0.01f, distanciaParaEscuchar);
        Sonar(Elegir(sonidosIdle), volumenIdle * Mathf.Lerp(0.3f, 1f, cercania));
    }

    private void ProgramarIdle()
    {
        proximoIdle = Time.time + Random.Range(esperaMinimaIdle, Mathf.Max(esperaMinimaIdle, esperaMaximaIdle));
    }

    private void AlCambiarLaVida(int vidaActual)
    {
        // Solo si bajo y sigue vivo: el golpe mortal usa el sonido de muerte.
        if (vidaActual < vidaAnterior && vidaActual > 0)
        {
            Sonar(Elegir(sonidosDanio), volumenDanio);
        }
        vidaAnterior = vidaActual;
    }

    private void AlMorir()
    {
        if (muerto) return;
        muerto = true;
        if (sonidoMuerte == null) return;

        // Por el AudioManager y no por la fuente propia: el enemigo se destruye enseguida
        // y cortaria el sonido a la mitad.
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(sonidoMuerte, volumenMuerte);
        else AudioSource.PlayClipAtPoint(sonidoMuerte, transform.position, volumenMuerte);
    }

    private void Sonar(AudioClip clip, float volumen)
    {
        if (clip == null) return;
        fuente.pitch = variarTono ? Random.Range(0.94f, 1.06f) : 1f;
        fuente.PlayOneShot(clip, volumen);
    }

    private static AudioClip Elegir(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }
}
