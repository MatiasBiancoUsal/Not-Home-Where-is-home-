using System.Collections;
using TMPro;
using UnityEngine;

// ============================================================
//  PUNTOS FLOTANTES (+1 / +10)
//
//  Cuando la niña agarra una moneda, sale un "+10" en el lugar de la moneda que sube
//  un poco, se agranda de golpe y se desvanece. Es el feedback de que el punto entro.
//
//  NO hay que ponerlo en ninguna escena: lo llama la moneda y el cartelito se crea solo.
//
//  Desde cualquier script:
//      PuntosFlotantes.Mostrar(10, transform.position);
// ============================================================
public static class PuntosFlotantes
{
    // Se dibuja en el MUNDO (no en la UI) para que quede pegado a la moneda.
    private const float TAMANIO = 5f;          // tamaño del texto en unidades del mundo
    private const float SUBE = 1.6f;           // cuanto sube mientras se va
    private const float DURACION = 0.9f;
    private const float ESCALA_INICIAL = 0.55f;
    private const float ESCALA_PICO = 1.15f;   // el golpecito de entrada

    private static readonly Color COLOR_CHICO = new Color(1f, 0.95f, 0.6f);   // +1
    private static readonly Color COLOR_GRANDE = new Color(1f, 0.78f, 0.25f); // +10 o mas

    public static void Mostrar(int puntos, Vector3 posicion)
    {
        if (!Application.isPlaying || puntos == 0) return;

        GameObject go = new GameObject("+" + puntos);
        go.transform.position = posicion;

        TextMeshPro texto = go.AddComponent<TextMeshPro>();
        texto.text = (puntos > 0 ? "+" : "") + puntos;
        texto.fontSize = TAMANIO;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = puntos >= 10 ? COLOR_GRANDE : COLOR_CHICO;
        texto.sortingOrder = 1000; // por encima del escenario

        // El texto no tiene por que ocupar el ancho de media pantalla.
        RectTransform rect = texto.rectTransform;
        rect.sizeDelta = new Vector2(4f, 2f);

        // Un objeto suelto que se maneja solo y se destruye al terminar.
        go.AddComponent<Volador>().Arrancar(texto);
    }

    // El componente que hace la animacion. Vive en el cartelito y se va con el.
    private class Volador : MonoBehaviour
    {
        private TextMeshPro texto;

        public void Arrancar(TextMeshPro t)
        {
            texto = t;
            StartCoroutine(Rutina());
        }

        private IEnumerator Rutina()
        {
            Vector3 desde = transform.position;
            Vector3 hasta = desde + new Vector3(0f, SUBE, 0f);
            Color colorBase = texto.color;

            float t = 0f;
            while (t < DURACION)
            {
                t += Time.deltaTime;
                float avance = Mathf.Clamp01(t / DURACION);

                // Sube frenando: arranca rapido y se va deteniendo.
                transform.position = Vector3.Lerp(desde, hasta, 1f - Mathf.Pow(1f - avance, 3f));

                // Golpecito al aparecer: crece de golpe y vuelve a su tamaño.
                float escala = avance < 0.25f
                    ? Mathf.Lerp(ESCALA_INICIAL, ESCALA_PICO, avance / 0.25f)
                    : Mathf.Lerp(ESCALA_PICO, 1f, (avance - 0.25f) / 0.75f);
                transform.localScale = Vector3.one * escala;

                // Se desvanece sobre el final, no desde el principio.
                float alpha = avance < 0.6f ? 1f : 1f - (avance - 0.6f) / 0.4f;
                texto.color = new Color(colorBase.r, colorBase.g, colorBase.b, alpha);

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
