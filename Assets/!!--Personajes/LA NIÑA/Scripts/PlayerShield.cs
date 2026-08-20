using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// ============================================================
//  ESCUDO DEL PLAYER
//  Va EN el player, al lado de las otras mecanicas (PlayerDash, PlayerStomp, etc).
//  Se desbloquea tocando una Flor, igual que las demas habilidades.
//
//  Como funciona:
//    - Se activa con una tecla y da INMORTALIDAD mientras esta en pie.
//    - Aguanta una cantidad de golpes (por defecto 6). Cada 2 golpes el sprite
//      cambia: limpio -> roto 1 -> roto 2. En el ultimo golpe se rompe y se
//      reproduce la animacion de rotura.
//    - Tambien puede caerse solo por tiempo, si se le pone una duracion.
//
//  La parte visual esta en ShieldVisual, que vive en un objeto HIJO del player.
//  Asi el escudo se dibuja SOBRE la niña sin tocar sus animaciones.
// ============================================================
public class PlayerShield : MonoBehaviour
{
    [Header("Tecla")]
    [Tooltip("Tecla que invoca el escudo. Se puede cambiar en cualquier momento desde aca.")]
    public Key teclaActivar = Key.E;
    [Tooltip("Si esta activo, volver a apretar la tecla baja el escudo antes de tiempo (y arranca el cooldown).")]
    public bool sePuedeBajarAMano = false;

    [Header("Resistencia")]
    [Tooltip("Cuantos golpes aguanta antes de romperse.")]
    public int golpesQueAguanta = 6;
    [Tooltip("Cada cuantos golpes cambia el sprite (limpio -> roto 1 -> roto 2).")]
    public int golpesPorEtapa = 2;

    [Header("Duracion y cooldown")]
    [Tooltip("Segundos que dura el escudo si no lo rompen. Poné 0 para que dure hasta que se rompa a golpes.")]
    public float duracion = 10f;
    [Tooltip("Segundos que hay que esperar para volver a invocarlo.")]
    public float cooldown = 8f;
    [Tooltip("Activo: el cooldown empieza cuando el escudo se cae. Desactivo: empieza en el momento de invocarlo.")]
    public bool cooldownEmpiezaAlCaerse = true;

    [Header("Al frenar un golpe")]
    [Tooltip("El golpe no empuja ni frena al player. Desactivalo si querés que el golpe se sienta igual.")]
    public bool bloquearKnockback = true;
    [Tooltip("Congela el juego un instante en cada golpe frenado, para que se sienta el impacto.")]
    public bool freezeFrame = true;
    [Tooltip("Cuanto dura ese congelamiento, en segundos.")]
    public float freezeDuracion = 0.05f;
    [Tooltip("Sacude la CAMARA cada vez que el escudo frena un golpe. Va apagado a proposito: el escudo protege, asi que el golpe se siente en el escudo (que vibra y destella) pero no zamarrea la pantalla. Prendelo solo si querés que los golpes frenados tambien muevan la camara.")]
    public bool sacudirCamara = false;

    [Header("Al romperse")]
    [Tooltip("Segundos de invulnerabilidad despues de que el escudo se rompe, para no comerse otro golpe al instante.")]
    public float invulnerabilidadAlRomper = 1f;
    [Tooltip("Sacude la camara (mas fuerte) cuando el escudo se rompe.")]
    public bool sacudirCamaraAlRomper = true;

    [Header("Aviso de que se esta por caer")]
    [Tooltip("El escudo parpadea cuando le queda esta cantidad de golpes (o menos). 0 = nunca.")]
    public int parpadearConGolpesRestantes = 1;
    [Tooltip("Si tiene duracion, tambien parpadea durante estos ultimos segundos. 0 = nunca.")]
    public float parpadearUltimosSegundos = 3f;

    [Header("Sonidos")]
    [Tooltip("Se puede dejar vacio. Si no hay AudioSource en el player, se crea uno solo.")]
    public AudioSource audioSource;
    public AudioClip sonidoActivar;
    public AudioClip sonidoGolpeFrenado;
    public AudioClip sonidoRotura;
    [Range(0f, 1f)] public float volumen = 1f;

    [Header("Testing")]
    [Tooltip("Ignora el cooldown, para poder probar el escudo sin esperar. Acordate de apagarlo.")]
    public bool ignorarCooldown = false;
    [Tooltip("Escribe en la consola cada golpe frenado y cada cambio de etapa.")]
    public bool mostrarLogs = false;

    // ---------- Estado ----------

    private bool activo;
    private int golpesRecibidos;
    private float timerDuracion;
    private float timerCooldown;
    private float timerInvulnerable;
    private bool rompiendose; // corriendo la animacion de rotura

    private PlayerController playerController;
    private ShieldVisual visual;

    // ---------- Getters ----------

    // El escudo esta en pie y frenando golpes.
    public bool EstaActivo => activo;
    // Golpes que todavia aguanta.
    public int GolpesRestantes => Mathf.Max(0, golpesQueAguanta - golpesRecibidos);
    // Golpes que ya freno.
    public int GolpesRecibidos => golpesRecibidos;
    // Todavia no se puede volver a invocar.
    public bool EnCooldown => timerCooldown > 0f;
    // Segundos que faltan para poder volver a invocarlo.
    public float TiempoRestanteCooldown => Mathf.Max(0f, timerCooldown);
    // Segundos que le quedan de vida al escudo (solo si tiene duracion).
    public float TiempoRestante => duracion > 0f ? Mathf.Max(0f, timerDuracion) : 0f;
    // El escudo esta bancando el golpe: ni el escudo activo ni los i-frames post-rotura dejan pasar daño.
    public bool EstaProtegido => activo || timerInvulnerable > 0f;
    // Lo consulta el HurtBox para saber si tiene que empujar al player o no.
    public bool BloqueaKnockbackAhora => EstaProtegido && bloquearKnockback;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        // El visual vive en un hijo. Lo buscamos incluso si esta desactivado.
        visual = GetComponentInChildren<ShieldVisual>(true);

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (sonidoActivar != null || sonidoGolpeFrenado != null || sonidoRotura != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        ActualizarTimers();
        LeerInput();
        ActualizarParpadeo();
    }

    private void ActualizarTimers()
    {
        if (timerCooldown > 0f) timerCooldown -= Time.deltaTime;
        if (timerInvulnerable > 0f) timerInvulnerable -= Time.deltaTime;

        // Duracion: solo corre si le pusimos un limite de tiempo.
        if (activo && duracion > 0f)
        {
            timerDuracion -= Time.deltaTime;
            if (timerDuracion <= 0f) Bajar();
        }
    }

    private void LeerInput()
    {
        if (Keyboard.current == null || teclaActivar == Key.None) return;

        KeyControl tecla = Keyboard.current[teclaActivar];
        if (tecla == null || !tecla.wasPressedThisFrame) return;

        if (activo)
        {
            if (sePuedeBajarAMano) Bajar();
            return;
        }

        Activar();
    }

    private void ActualizarParpadeo()
    {
        if (visual == null || !activo) return;

        bool porGolpes = parpadearConGolpesRestantes > 0 && GolpesRestantes <= parpadearConGolpesRestantes;
        bool porTiempo = duracion > 0f && parpadearUltimosSegundos > 0f && timerDuracion <= parpadearUltimosSegundos;

        visual.SetParpadeo(porGolpes || porTiempo);
    }

    // ---------- Activar / bajar ----------

    // Invoca el escudo. Devuelve false si no se pudo (sin habilidad, en cooldown o ya activo).
    public bool Activar()
    {
        if (playerController != null && !playerController.TieneHabilidad(PlayerController.Habilidad.Escudo)) return false;
        if (activo || rompiendose) return false;
        if (!ignorarCooldown && timerCooldown > 0f) return false;

        activo = true;
        golpesRecibidos = 0;
        timerDuracion = duracion;

        if (!cooldownEmpiezaAlCaerse) timerCooldown = cooldown;

        if (visual != null)
        {
            visual.Mostrar();
            visual.CambiarEtapa(ShieldVisual.Etapa.Limpio);
        }

        Sonar(sonidoActivar);

        if (mostrarLogs) Debug.Log("[Escudo] Activado. Aguanta " + golpesQueAguanta + " golpes.");
        return true;
    }

    // Se cae sin romperse (se le acabo el tiempo, o lo bajamos a mano).
    public void Bajar()
    {
        if (!activo) return;

        activo = false;
        if (visual != null) visual.Desvanecer();
        if (cooldownEmpiezaAlCaerse) timerCooldown = cooldown;

        if (mostrarLogs) Debug.Log("[Escudo] Se cayo sin romperse.");
    }

    // ---------- Absorber daño ----------

    // Lo llama el Damageable del player ANTES de aplicar el daño.
    // Devuelve true si el golpe fue frenado (entonces el player no pierde vida).
    public bool AbsorberGolpe(int danio, Vector2 origen)
    {
        // Invulnerabilidad de cortesia despues de que se rompio el escudo.
        if (timerInvulnerable > 0f) return true;

        if (!activo) return false;

        golpesRecibidos++;

        if (mostrarLogs) Debug.Log("[Escudo] Golpe frenado " + golpesRecibidos + "/" + golpesQueAguanta);

        Sonar(sonidoGolpeFrenado);

        // Se rompio: este era el ultimo golpe que aguantaba.
        if (golpesRecibidos >= golpesQueAguanta)
        {
            Romper();
            return true;
        }

        // Sigue en pie: vibra, destella y cambia de sprite si toca.
        if (visual != null)
        {
            visual.Golpe();
            visual.CambiarEtapa(EtapaSegunGolpes());
        }

        if (sacudirCamara) CameraShaker.Instance?.ShakeShieldHit();
        if (freezeFrame) StartCoroutine(FreezeFrame());

        return true;
    }

    // Con golpesPorEtapa = 2: golpes 0-1 limpio, 2-3 roto 1, 4-5 roto 2.
    private ShieldVisual.Etapa EtapaSegunGolpes()
    {
        if (golpesPorEtapa <= 0) return ShieldVisual.Etapa.Limpio;

        int etapa = golpesRecibidos / golpesPorEtapa;

        if (etapa <= 0) return ShieldVisual.Etapa.Limpio;
        if (etapa == 1) return ShieldVisual.Etapa.Roto1;
        return ShieldVisual.Etapa.Roto2;
    }

    private void Romper()
    {
        activo = false;
        rompiendose = true;
        timerInvulnerable = invulnerabilidadAlRomper;
        timerCooldown = cooldown; // el cooldown siempre arranca al romperse

        if (visual != null) visual.Romper();

        Sonar(sonidoRotura);

        if (sacudirCamaraAlRomper) CameraShaker.Instance?.ShakeShieldBreak();
        if (freezeFrame) StartCoroutine(FreezeFrame());

        StartCoroutine(FinDeRotura());

        if (mostrarLogs) Debug.Log("[Escudo] Roto.");
    }

    // Marca de que ya termino la rotura, asi se puede volver a invocar cuando baje el cooldown.
    private IEnumerator FinDeRotura()
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, invulnerabilidadAlRomper));
        rompiendose = false;
    }

    // Mismo criterio que usa Damageable: no pisamos un freeze que ya este corriendo.
    private IEnumerator FreezeFrame()
    {
        if (Time.timeScale == 0f) yield break;

        float original = Time.timeScale;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(freezeDuracion);

        if (Time.timeScale == 0f) Time.timeScale = original;
    }

    private void Sonar(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, volumen);
    }

    // Para el menu de pausa / cambio de escena: deja todo limpio.
    private void OnDisable()
    {
        if (activo && visual != null) visual.Ocultar();
        activo = false;
        rompiendose = false;
    }
}
