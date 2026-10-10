using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LaserLoop : MonoBehaviour
{
    public GameObject LaserVisual;

    [Header("Ciclo")]
    public float TiempoEncendido = 2f;
    public float TiempoApagado = 1f;

    [Header("Aviso previo")]
    [Min(0.05f)] public float TiempoAdvertencia = 0.45f;
    [Range(0f, 1f)] public float OpacidadAdvertencia = 0.38f;
    [Min(1f)] public float VelocidadParpadeo = 12f;

    [Header("Aspecto")]
    public Color ColorLaser = new Color(1f, 0.04f, 0.08f, 1f);
    public Color ColorNucleo = new Color(1f, 0.86f, 0.88f, 1f);
    [Range(0.05f, 0.8f)] public float AnchoNucleo = 0.24f;
    [Range(0f, 4f)] public float IntensidadLuz = 1.65f;
    [Range(0f, 1f)] public float PulsacionLuz = 0.25f;
    [Min(0f)] public float VelocidadPulsacion = 9f;
    [Range(1f, 6f)] public float AnchoHalo = 3.2f;
    [Range(0f, 2f)] public float IntensidadHalo = 0.55f;

    private SpriteRenderer laserRenderer;
    private SpriteRenderer warningRenderer;
    private Light2D laserLight;
    private Light2D haloLight;

    private void Awake()
    {
        PrepararAspecto();
    }

    private void Start()
    {
        StartCoroutine(CicloLaser());
    }

    private void PrepararAspecto()
    {
        if (LaserVisual == null)
            return;

        laserRenderer = LaserVisual.GetComponent<SpriteRenderer>();
        if (laserRenderer == null || laserRenderer.sprite == null)
            return;

        laserRenderer.color = ColorLaser;

        Transform nucleo = LaserVisual.transform.Find("NucleoLaser");
        if (nucleo == null)
        {
            GameObject objetoNucleo = new GameObject("NucleoLaser");
            nucleo = objetoNucleo.transform;
            nucleo.SetParent(LaserVisual.transform, false);
        }

        nucleo.localPosition = new Vector3(0f, 0f, -0.01f);
        nucleo.localRotation = Quaternion.identity;
        nucleo.localScale = new Vector3(AnchoNucleo, 1f, 1f);

        SpriteRenderer nucleoRenderer = nucleo.GetComponent<SpriteRenderer>();
        if (nucleoRenderer == null)
            nucleoRenderer = nucleo.gameObject.AddComponent<SpriteRenderer>();

        nucleoRenderer.sprite = laserRenderer.sprite;
        nucleoRenderer.material = laserRenderer.material;
        nucleoRenderer.color = ColorNucleo;
        nucleoRenderer.sortingLayerID = laserRenderer.sortingLayerID;
        nucleoRenderer.sortingOrder = laserRenderer.sortingOrder + 1;

        laserLight = LaserVisual.GetComponent<Light2D>();
        if (laserLight == null)
            laserLight = LaserVisual.AddComponent<Light2D>();

        laserLight.lightType = Light2D.LightType.Sprite;
        laserLight.lightCookieSprite = laserRenderer.sprite;
        laserLight.color = ColorLaser;
        laserLight.intensity = IntensidadLuz;
        laserLight.falloffIntensity = 0.85f;

        Transform halo = LaserVisual.transform.Find("HaloLuzLaser");
        if (halo == null)
        {
            GameObject objetoHalo = new GameObject("HaloLuzLaser");
            halo = objetoHalo.transform;
            halo.SetParent(LaserVisual.transform, false);
        }

        halo.localPosition = new Vector3(0f, 0f, 0.02f);
        halo.localRotation = Quaternion.identity;
        halo.localScale = new Vector3(AnchoHalo, 1.08f, 1f);

        haloLight = halo.GetComponent<Light2D>();
        if (haloLight == null)
            haloLight = halo.gameObject.AddComponent<Light2D>();

        haloLight.lightType = Light2D.LightType.Sprite;
        haloLight.lightCookieSprite = laserRenderer.sprite;
        haloLight.color = new Color(ColorLaser.r, ColorLaser.g * 0.45f, ColorLaser.b * 0.45f, 1f);
        haloLight.intensity = IntensidadHalo;
        haloLight.falloffIntensity = 1f;

        Transform warning = transform.Find("AvisoLaser");
        if (warning == null)
        {
            GameObject objetoAviso = new GameObject("AvisoLaser");
            warning = objetoAviso.transform;
            warning.SetParent(transform, false);
        }

        warning.localPosition = LaserVisual.transform.localPosition;
        warning.localRotation = LaserVisual.transform.localRotation;
        Vector3 escalaAviso = LaserVisual.transform.localScale;
        escalaAviso.x *= 0.3f;
        warning.localScale = escalaAviso;

        warningRenderer = warning.GetComponent<SpriteRenderer>();
        if (warningRenderer == null)
            warningRenderer = warning.gameObject.AddComponent<SpriteRenderer>();

        warningRenderer.sprite = laserRenderer.sprite;
        warningRenderer.material = laserRenderer.material;
        warningRenderer.color = new Color(ColorLaser.r, ColorLaser.g, ColorLaser.b, OpacidadAdvertencia);
        warningRenderer.sortingLayerID = laserRenderer.sortingLayerID;
        warningRenderer.sortingOrder = laserRenderer.sortingOrder;
        warning.gameObject.SetActive(false);
    }

    private IEnumerator CicloLaser()
    {
        while (true)
        {
            if (LaserVisual == null)
                yield break;

            LaserVisual.SetActive(false);

            if (warningRenderer != null)
            {
                warningRenderer.gameObject.SetActive(true);
                float avisoTranscurrido = 0f;
                while (avisoTranscurrido < TiempoAdvertencia)
                {
                    avisoTranscurrido += Time.deltaTime;
                    float pulso = 0.55f + 0.45f * Mathf.Sin(avisoTranscurrido * VelocidadParpadeo);
                    Color colorAviso = ColorLaser;
                    colorAviso.a = OpacidadAdvertencia * pulso;
                    warningRenderer.color = colorAviso;
                    yield return null;
                }
                warningRenderer.gameObject.SetActive(false);
            }

            LaserVisual.SetActive(true);
            float encendidoTranscurrido = 0f;
            while (encendidoTranscurrido < TiempoEncendido)
            {
                encendidoTranscurrido += Time.deltaTime;
                if (laserLight != null)
                    laserLight.intensity = IntensidadLuz + Mathf.Sin(encendidoTranscurrido * VelocidadPulsacion) * PulsacionLuz;
                if (haloLight != null)
                    haloLight.intensity = IntensidadHalo + Mathf.Sin(encendidoTranscurrido * VelocidadPulsacion) * PulsacionLuz * 0.45f;
                yield return null;
            }

            LaserVisual.SetActive(false);
            yield return new WaitForSeconds(TiempoApagado);
        }
    }

}
