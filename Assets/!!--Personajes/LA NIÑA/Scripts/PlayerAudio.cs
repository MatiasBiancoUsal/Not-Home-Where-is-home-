using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PlayerAudio : MonoBehaviour
{
    [Header("Salida")]
    [Tooltip("Canal SFX del MainMixer. Ya queda conectado en el prefab.")]
    [SerializeField] private AudioMixerGroup grupoSFX;
    [Range(0f, 1f)] [SerializeField] private float volumenGeneral = 1f;

    [Header("Pasos")]
    [Tooltip("Podes agregar varias variantes; se elige una distinta al azar.")]
    [SerializeField] private AudioClip[] pasos;
    [Tooltip("Dejalo apagado para usar los pasos sincronizados de RUN_animation.")]
    [SerializeField] private bool pasosAutomaticos = false;
    [Min(0.08f)] [SerializeField] private float intervaloPasos = 0.32f;
    [Min(0f)] [SerializeField] private float velocidadMinimaPasos = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float volumenPasos = 0.55f;

    [Header("Saltos y movimiento")]
    [Tooltip("Suena cuando la niña despega y empieza a subir.")]
    [FormerlySerializedAs("salto")]
    [SerializeField] private AudioClip subidaSalto;
    [Tooltip("Suena una vez al llegar al punto mas alto y comenzar a caer.")]
    [SerializeField] private AudioClip caidaSalto;
    [SerializeField] private AudioClip dobleSalto;
    [SerializeField] private AudioClip dash;
    [SerializeField] private AudioClip aterrizajeSuave;
    [SerializeField] private AudioClip aterrizajeFuerte;
    [Min(0f)] [SerializeField] private float velocidadAterrizajeFuerte = 12f;

    [Header("Escudo")]
    [SerializeField] private AudioClip escudoActivar;
    [SerializeField] private AudioClip escudoGolpe;
    [SerializeField] private AudioClip escudoRomper;

    [Header("Otros sonidos del personaje")]
    [SerializeField] private AudioClip saltoPared;
    [SerializeField] private AudioClip trepar;
    [SerializeField] private AudioClip pisoton;
    [SerializeField] private AudioClip ataque;
    [Tooltip("Segundos del principio del sonido de ataque que se saltean. El archivo actual (atack) " +
             "arranca muy bajito y el golpe fuerte llega tarde, cuando la animacion ya termino. " +
             "Subilo o bajalo de a poco con el juego en Play hasta que el golpe suene justo.")]
    [Min(0f)] [SerializeField] private float saltearInicioDelAtaque = 0.12f;
    [SerializeField] private AudioClip recibirDanio;
    [SerializeField] private AudioClip muerte;

    private AudioSource fuente;
    private AudioSource fuenteAtaque;
    private PlayerController playerController;
    private float timerPaso;
    private bool estabaEnSuelo;
    private float mayorVelocidadDeCaida;
    private bool sonidoCaidaReproducido;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        fuente = GetComponent<AudioSource>();

        if (fuente == null) fuente = gameObject.AddComponent<AudioSource>();

        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 0f;
        fuente.outputAudioMixerGroup = grupoSFX;
    }

    private void Start()
    {
        estabaEnSuelo = EstaEnSuelo();
    }

    private void Update()
    {
        if (playerController == null || playerController.rb == null) return;

        ActualizarAterrizaje();
        ActualizarCaidaDelSalto();
        if (pasosAutomaticos) ActualizarPasos();
    }

    private void ActualizarPasos()
    {
        bool puedeCaminar = EstaEnSuelo()
            && !EstaTrepanado()
            && !EstaEnDash()
            && Mathf.Abs(playerController.rb.linearVelocity.x) >= velocidadMinimaPasos;

        if (!puedeCaminar)
        {
            timerPaso = 0f;
            return;
        }

        timerPaso -= Time.deltaTime;
        if (timerPaso <= 0f)
        {
            ReproducirPaso();
            timerPaso = intervaloPasos;
        }
    }

    private void ActualizarAterrizaje()
    {
        bool enSuelo = EstaEnSuelo();

        if (!enSuelo)
        {
            mayorVelocidadDeCaida = Mathf.Max(mayorVelocidadDeCaida, -playerController.rb.linearVelocity.y);
        }
        else if (!estabaEnSuelo)
        {
            AudioClip clip = mayorVelocidadDeCaida >= velocidadAterrizajeFuerte
                ? aterrizajeFuerte
                : aterrizajeSuave;
            Reproducir(clip, 0.8f);
            mayorVelocidadDeCaida = 0f;
        }

        estabaEnSuelo = enSuelo;
    }

    private void ActualizarCaidaDelSalto()
    {
        bool enSuelo = EstaEnSuelo();

        // Detecta cualquier caida: despues de saltar, al salir caminando de una
        // plataforma o al caer desde otra mecanica, sin depender de la tecla.
        if (!enSuelo && playerController.rb.linearVelocity.y < -0.1f && !sonidoCaidaReproducido)
        {
            Reproducir(caidaSalto, 0.8f);
            sonidoCaidaReproducido = true;
        }
        else if (enSuelo)
        {
            sonidoCaidaReproducido = false;
        }
    }

    private bool EstaEnSuelo()
    {
        return playerController != null && playerController.jump != null && playerController.jump.IsGrounded;
    }

    private bool EstaTrepanado()
    {
        return playerController.climb != null && playerController.climb.IsClimbing;
    }

    private bool EstaEnDash()
    {
        return playerController.dash != null && playerController.dash.IsDash;
    }

    public void ReproducirPaso()
    {
        if (!EstaEnSuelo() || EstaTrepanado() || EstaEnDash()) return;
        if (pasos == null || pasos.Length == 0) return;
        Reproducir(pasos[Random.Range(0, pasos.Length)], volumenPasos, true);
    }

    public void ReproducirSalto()
    {
        sonidoCaidaReproducido = false;
        Reproducir(subidaSalto, 0.85f, true);
    }

    public void ReproducirDobleSalto()
    {
        sonidoCaidaReproducido = false;
        Reproducir(dobleSalto, 0.9f, true);
    }
    public void ReproducirDash() => Reproducir(dash, 0.9f);
    public void ReproducirEscudoActivar() => Reproducir(escudoActivar);
    public void ReproducirEscudoGolpe() => Reproducir(escudoGolpe);
    public void ReproducirEscudoRomper() => Reproducir(escudoRomper);
    public void ReproducirSaltoPared() => Reproducir(saltoPared, 0.85f, true);
    public void ReproducirTrepar() => Reproducir(trepar, 0.6f, true);
    public void ReproducirPisoton() => Reproducir(pisoton);
    // El ataque va por su propia fuente: PlayOneShot siempre arranca desde el principio del
    // archivo, y aca necesitamos arrancar un poco mas adelante para que no suene con delay.
    public void ReproducirAtaque()
    {
        if (ataque == null || fuente == null) return;

        if (fuenteAtaque == null)
        {
            fuenteAtaque = gameObject.AddComponent<AudioSource>();
            fuenteAtaque.playOnAwake = false;
            fuenteAtaque.loop = false;
            fuenteAtaque.spatialBlend = 0f;
            fuenteAtaque.outputAudioMixerGroup = grupoSFX;
        }

        fuenteAtaque.clip = ataque;
        fuenteAtaque.pitch = Random.Range(0.96f, 1.04f);
        fuenteAtaque.volume = 0.9f * volumenGeneral;
        fuenteAtaque.time = Mathf.Clamp(saltearInicioDelAtaque, 0f, Mathf.Max(0f, ataque.length - 0.05f));
        fuenteAtaque.Play();
    }
    public void ReproducirDanio() => Reproducir(recibirDanio);
    public void ReproducirMuerte() => Reproducir(muerte);

    private void Reproducir(AudioClip clip, float volumen = 1f, bool variarTono = false)
    {
        if (clip == null || fuente == null) return;

        fuente.pitch = variarTono ? Random.Range(0.96f, 1.04f) : 1f;
        fuente.PlayOneShot(clip, volumen * volumenGeneral);
    }
}
