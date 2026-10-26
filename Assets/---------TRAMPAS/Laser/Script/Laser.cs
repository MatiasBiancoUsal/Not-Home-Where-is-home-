using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LaserLoop : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject LaserVisual;
    public SpriteRenderer ImagenLaser;
    [Tooltip("Arrastra aqui el sprite que queres usar para este laser.")]
    public Sprite SpriteDelLaser;
    public HurtBox ZonaDanio;
    public LaserTriggerDanio ZonaColision;

    [Header("Tamaño")]
    [Tooltip("Cambia solamente el tamaño del laser en el eje X, sin modificar Y.")]
    [Min(0.01f)] public float EscalaX = 1f;
    [Tooltip("Cambia solamente el tamaño del laser en el eje Y, sin modificar X.")]
    [Min(0.01f)] public float EscalaY = 1f;

    [Header("Ciclo rapido")]
    [Min(0.05f)] public float TiempoAparicion = 0.2f;
    [Min(0.05f)] public float TiempoEncendido = 1.25f;
    [Min(0.05f)] public float TiempoDesvanecido = 0.2f;
    [Min(0.05f)] public float TiempoApagado = 0.45f;

    [Header("Sonido al tocar al jugador")]
    public AudioClip SonidoAlTocar;
    [Range(0f, 1f)] public float VolumenSonido = 0.8f;

    [Header("Efecto de luz")]
    public Color ColorLuz = new Color(1f, 0.08f, 0.08f, 1f);
    [Min(0f)] public float IntensidadLuz = 1.5f;
    public Light2D LuzLaser;

    private AudioSource fuenteAudio;
    private Color colorOriginal;

    private void Awake()
    {
        if (LaserVisual != null)
        {
            if (ImagenLaser == null)
                ImagenLaser = LaserVisual.GetComponent<SpriteRenderer>();
            if (ZonaDanio == null)
                ZonaDanio = LaserVisual.GetComponent<HurtBox>();
        }

        // El detector pertenece al Empty padre "Laser". La imagen hija es solo visual.
        if (ZonaColision == null)
            ZonaColision = GetComponent<LaserTriggerDanio>();

        AplicarEscalaXY();

        if (ImagenLaser != null)
        {
            if (SpriteDelLaser != null)
                ImagenLaser.sprite = SpriteDelLaser;

            colorOriginal = ImagenLaser.color;
            PrepararLuz();
        }

        AjustarColliderAImagen();

        fuenteAudio = GetComponent<AudioSource>();
        if (fuenteAudio == null)
            fuenteAudio = gameObject.AddComponent<AudioSource>();

        fuenteAudio.playOnAwake = false;
        fuenteAudio.loop = false;
        fuenteAudio.spatialBlend = 0f;
    }

    private void OnValidate()
    {
        EscalaX = Mathf.Max(0.01f, EscalaX);
        EscalaY = Mathf.Max(0.01f, EscalaY);

        if (LaserVisual != null && ImagenLaser == null)
            ImagenLaser = LaserVisual.GetComponent<SpriteRenderer>();
        if (ZonaColision == null)
            ZonaColision = GetComponent<LaserTriggerDanio>();

        AplicarEscalaXY();
        AjustarColliderAImagen();
    }

    private void AplicarEscalaXY()
    {
        if (LaserVisual == null)
            return;

        Vector3 escala = LaserVisual.transform.localScale;
        escala.x = EscalaX;
        escala.y = EscalaY;
        LaserVisual.transform.localScale = escala;
    }

    private void Start()
    {
        StartCoroutine(CicloLaser());
    }

    private void PrepararLuz()
    {
        if (LaserVisual == null || ImagenLaser == null)
            return;

        if (LuzLaser == null)
            LuzLaser = LaserVisual.GetComponent<Light2D>();
        if (LuzLaser == null)
            LuzLaser = LaserVisual.AddComponent<Light2D>();

        LuzLaser.lightType = Light2D.LightType.Sprite;
        LuzLaser.lightCookieSprite = ImagenLaser.sprite;
        LuzLaser.color = ColorLuz;
        LuzLaser.intensity = 0f;
        LuzLaser.falloffIntensity = 0.8f;
    }

    private IEnumerator CicloLaser()
    {
        while (true)
        {
            PrepararEncendido();
            yield return AparecerProgresivamente();
            ActivarDanio();
            yield return new WaitForSeconds(TiempoEncendido);

            yield return DesaparecerProgresivamente();

            OcultarLaser();
            yield return new WaitForSeconds(TiempoApagado);
        }
    }

    private void PrepararEncendido()
    {
        if (LaserVisual != null)
            LaserVisual.SetActive(true);

        if (ImagenLaser != null)
        {
            Color invisible = colorOriginal;
            invisible.a = 0f;
            ImagenLaser.color = invisible;
        }

        if (LuzLaser != null)
            LuzLaser.intensity = 0f;

        DesactivarDanio();
    }

    private IEnumerator AparecerProgresivamente()
    {
        if (ImagenLaser == null)
            yield break;

        float transcurrido = 0f;
        while (transcurrido < TiempoAparicion)
        {
            transcurrido += Time.deltaTime;
            float progreso = Mathf.Clamp01(transcurrido / TiempoAparicion);
            Color color = colorOriginal;
            color.a = Mathf.Lerp(0f, colorOriginal.a, progreso);
            ImagenLaser.color = color;
            if (LuzLaser != null)
                LuzLaser.intensity = Mathf.Lerp(0f, IntensidadLuz, progreso);
            yield return null;
        }
    }

    private void ActivarDanio()
    {
        // El daño comienza solamente cuando terminó de aparecer.
        if (ZonaDanio != null)
            ZonaDanio.enabled = false;
        if (ZonaColision != null)
        {
            ZonaColision.enabled = true;
            ZonaColision.GetComponent<BoxCollider2D>().enabled = true;
        }
    }

    private void DesactivarDanio()
    {
        if (ZonaDanio != null)
            ZonaDanio.enabled = false;
        if (ZonaColision != null)
        {
            ZonaColision.enabled = false;
            ZonaColision.GetComponent<BoxCollider2D>().enabled = false;
        }
    }

    private IEnumerator DesaparecerProgresivamente()
    {
        if (ImagenLaser == null)
            yield break;

        float transcurrido = 0f;
        while (transcurrido < TiempoDesvanecido)
        {
            transcurrido += Time.deltaTime;
            float progreso = Mathf.Clamp01(transcurrido / TiempoDesvanecido);
            Color color = colorOriginal;
            color.a = Mathf.Lerp(colorOriginal.a, 0f, progreso);
            ImagenLaser.color = color;
            if (LuzLaser != null)
                LuzLaser.intensity = Mathf.Lerp(IntensidadLuz, 0f, progreso);
            yield return null;
        }
    }

    private void OcultarLaser()
    {
        // Al quedar invisible también deja de hacer daño.
        DesactivarDanio();

        if (LaserVisual != null)
            LaserVisual.SetActive(false);

        if (LuzLaser != null)
            LuzLaser.intensity = 0f;
    }

    public void ReproducirSonidoContacto()
    {
        if (SonidoAlTocar != null && fuenteAudio != null)
            fuenteAudio.PlayOneShot(SonidoAlTocar, VolumenSonido);
    }

    private void AjustarColliderAImagen()
    {
        if (ImagenLaser == null || ZonaColision == null)
            return;

        BoxCollider2D caja = ZonaColision.GetComponent<BoxCollider2D>();
        if (caja == null)
            return;

        Bounds limites = ImagenLaser.localBounds;
        Vector3[] esquinas =
        {
            new Vector3(limites.min.x, limites.min.y),
            new Vector3(limites.min.x, limites.max.y),
            new Vector3(limites.max.x, limites.min.y),
            new Vector3(limites.max.x, limites.max.y)
        };

        Vector2 minimo = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 maximo = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        foreach (Vector3 esquina in esquinas)
        {
            Vector3 mundo = ImagenLaser.transform.TransformPoint(esquina);
            Vector3 local = transform.InverseTransformPoint(mundo);
            minimo = Vector2.Min(minimo, local);
            maximo = Vector2.Max(maximo, local);
        }

        caja.offset = (minimo + maximo) * 0.5f;
        caja.size = maximo - minimo;
        caja.isTrigger = true;
    }
}
