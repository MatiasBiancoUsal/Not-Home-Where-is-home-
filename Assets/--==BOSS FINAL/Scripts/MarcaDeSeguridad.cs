using UnityEngine;
using UnityEngine.Rendering.Universal;

// ============================================================
//  MARCA DE SEGURIDAD (el lugar del piso donde NO cae nada)
//  Se enciende mientras las estalactitas tiemblan avisando, late para llamar la
//  atencion y se apaga sola cuando caen.
//
//  La crea el boss durante el derrumbe. Si queres tu propio dibujo, armá un prefab
//  con un SpriteRenderer (y una Light 2D adentro si querés que ilumine) y arrastralo
//  al campo "Prefab Marca De Hueco" del BOSS.
// ============================================================
public class MarcaDeSeguridad : MonoBehaviour
{
    [Tooltip("Cuanto dura antes de apagarse sola.")]
    public float duracion = 1f;
    [Tooltip("Que tan rapido late.")]
    public float velocidadDelLatido = 6f;
    [Tooltip("Cuanto se agranda al latir. 0 = no cambia de tamaño.")]
    public float latido = 0.12f;
    [Tooltip("Segundos que tarda en aparecer.")]
    public float aparicion = 0.15f;

    private SpriteRenderer dibujo;
    private Light2D luz;
    private Color colorBase;
    private float intensidadBase;
    private Vector3 escalaBase;
    private float t;

    private void Start()
    {
        dibujo = GetComponentInChildren<SpriteRenderer>();
        if (dibujo != null) colorBase = dibujo.color;

        // Si tu prefab tiene una Light 2D adentro, tambien late y se apaga con la marca.
        luz = GetComponentInChildren<Light2D>();
        if (luz != null) intensidadBase = luz.intensity;

        escalaBase = transform.localScale;
    }

    public void Iniciar(float segundos)
    {
        duracion = Mathf.Max(0.1f, segundos);
    }

    private void Update()
    {
        t += Time.deltaTime;

        float pulso = Mathf.Sin(t * velocidadDelLatido) * 0.5f + 0.5f;   // 0 a 1
        float entrada = Mathf.Clamp01(t / Mathf.Max(0.01f, aparicion));

        // Los ultimos instantes se desvanece, asi no desaparece de golpe.
        float salida = Mathf.Clamp01((duracion - t) / 0.25f);

        if (dibujo != null)
        {
            Color c = colorBase;
            c.a = colorBase.a * Mathf.Lerp(0.5f, 1f, pulso) * entrada * salida;
            dibujo.color = c;
        }

        if (luz != null)
        {
            luz.intensity = intensidadBase * Mathf.Lerp(0.5f, 1f, pulso) * entrada * salida;
        }

        transform.localScale = escalaBase * (1f + latido * pulso);

        if (t >= duracion) Destroy(gameObject);
    }
}
