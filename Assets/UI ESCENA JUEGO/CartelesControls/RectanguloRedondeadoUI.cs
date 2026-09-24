using UnityEngine;
using UnityEngine.UI;

// Rectangulo UI redondeado sin necesitar una imagen externa.
public class RectanguloRedondeadoUI : MaskableGraphic
{
    [Min(0f)] public float radio = 28f;
    [Range(2, 12)] public int suavidad = 6;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        float rad = Mathf.Min(radio, Mathf.Min(r.width, r.height) * 0.5f);
        Vector2 centro = r.center;
        vh.AddVert(centro, color, new Vector2(0.5f, 0.5f));

        int pasos = Mathf.Max(2, suavidad);
        for (int esquina = 0; esquina < 4; esquina++)
        {
            Vector2 c = esquina switch
            {
                0 => new Vector2(r.xMax - rad, r.yMax - rad),
                1 => new Vector2(r.xMin + rad, r.yMax - rad),
                2 => new Vector2(r.xMin + rad, r.yMin + rad),
                _ => new Vector2(r.xMax - rad, r.yMin + rad)
            };

            float inicio = 0.5f * Mathf.PI * esquina;
            for (int i = 0; i <= pasos; i++)
            {
                float a = inicio + Mathf.PI * 0.5f * i / pasos;
                Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad;
                vh.AddVert(p, color, new Vector2(Mathf.InverseLerp(r.xMin, r.xMax, p.x), Mathf.InverseLerp(r.yMin, r.yMax, p.y)));
            }
        }

        int borde = 4 * (pasos + 1);
        for (int i = 0; i < borde; i++)
        {
            int actual = i + 1;
            int siguiente = ((i + 1) % borde) + 1;
            vh.AddTriangle(0, actual, siguiente);
        }
    }
}
