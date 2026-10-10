using UnityEngine;

// ============================================================
//  EFECTO PUF
//  Unos cuadraditos que saltan para los costados y se desvanecen. Lo usan el boss,
//  los bichos y las estalactitas para romperse / morir sin necesitar una animacion.
//  No va en ningun objeto: se crea solo con EfectoPuf.Crear(...).
// ============================================================
public class EfectoPuf : MonoBehaviour
{
    private const float DURACION = 0.6f;
    private const float GRAVEDAD = 14f;

    private Transform[] particulas;
    private SpriteRenderer[] dibujos;
    private Vector2[] velocidades;
    private float tamanio;
    private float t;

    public static void Crear(Vector3 posicion, Color color, int cantidad = 8, float tamanio = 0.2f, float fuerza = 4f)
    {
        GameObject go = new GameObject("Puf");
        go.transform.position = posicion;
        go.AddComponent<EfectoPuf>().Armar(color, Mathf.Max(1, cantidad), tamanio, fuerza);
    }

    private void Armar(Color color, int cantidad, float tam, float fuerza)
    {
        tamanio = tam;
        particulas = new Transform[cantidad];
        dibujos = new SpriteRenderer[cantidad];
        velocidades = new Vector2[cantidad];

        for (int i = 0; i < cantidad; i++)
        {
            GameObject p = new GameObject("particula");
            p.transform.SetParent(transform, false);
            p.transform.localScale = Vector3.one * tamanio;

            SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = UtilBoss.Cuadrado();
            sr.color = color;
            sr.sortingOrder = 60;

            // Hacia los costados y un poco para arriba.
            float angulo = Random.Range(15f, 165f) * Mathf.Deg2Rad;
            velocidades[i] = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * fuerza * Random.Range(0.5f, 1f);

            particulas[i] = p.transform;
            dibujos[i] = sr;
        }
    }

    private void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / DURACION);

        for (int i = 0; i < particulas.Length; i++)
        {
            velocidades[i].y -= GRAVEDAD * Time.deltaTime;
            particulas[i].position += (Vector3)(velocidades[i] * Time.deltaTime);
            particulas[i].localScale = Vector3.one * tamanio * (1f - k * 0.7f);

            Color c = dibujos[i].color;
            c.a = 1f - k;
            dibujos[i].color = c;
        }

        if (t >= DURACION) Destroy(gameObject);
    }
}
