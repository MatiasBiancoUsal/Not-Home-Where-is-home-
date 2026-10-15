using System;
using System.Collections;
using TMPro;
using UnityEngine;

// ============================================================
//  ANIMACION DE TEXTO (compartida)
//
//  La entrada letra por letra que usan los carteles del juego: cada letra aparece con
//  fade y creciendo desde chiquita, y como cada una arranca un poco despues que la
//  anterior, el conjunto se lee como un barrido de izquierda a derecha.
//
//  Vive aca afuera para que la usen todos los que la necesiten (el cartel de habilidad,
//  el de "te faltan X puntos", las ayudas al jugador) sin copiar y pegar el codigo.
//  Los ajustes (velocidad, zoom, etc) viajan en un EntradaDeTexto.
// ============================================================
public static class AnimacionDeTexto
{
    // Hace entrar el texto letra por letra. 'saltear' es opcional: si devuelve true,
    // la animacion se corta y el texto queda completo.
    public static IEnumerator EntrarPorLetra(TMP_Text texto, EntradaDeTexto cfg, Func<bool> saltear = null)
    {
        if (texto == null || cfg == null) yield break;

        texto.ForceMeshUpdate();
        int total = texto.textInfo.characterCount;
        if (total <= 0) yield break;

        float retraso = Mathf.Max(0f, cfg.retrasoEntreLetras);
        float porLetra = Mathf.Max(0.01f, cfg.duracionPorLetra);
        float duracionTotal = retraso * (total - 1) + porLetra;

        float t = 0f;

        while (t < duracionTotal)
        {
            t += Time.unscaledDeltaTime;

            // Regeneramos la malla cada frame: asi partimos SIEMPRE de las posiciones
            // originales (y no se acumulan las deformaciones del frame anterior), y
            // ademas aguanta que el estilo cambie en vivo desde el Inspector.
            texto.ForceMeshUpdate();
            TMP_TextInfo info = texto.textInfo;

            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo c = info.characterInfo[i];
                if (!c.isVisible) continue; // los espacios no tienen malla

                // Cuanto avanzo ESTA letra. La de mas a la derecha arranca mas tarde.
                float avance = Mathf.Clamp01((t - i * retraso) / porLetra);
                float suave = 1f - Mathf.Pow(1f - avance, 3f); // arranca rapido, frena suave

                int vi = c.vertexIndex;
                int mi = c.materialReferenceIndex;

                Vector3[] vertices = info.meshInfo[mi].vertices;
                Color32[] colores = info.meshInfo[mi].colors32;

                // Zoom: escalamos los 4 vertices respecto al centro de la letra, asi
                // crece y se achica en su lugar en vez de correrse.
                Vector3 centro = (vertices[vi] + vertices[vi + 2]) * 0.5f;
                float escala = Mathf.Lerp(Mathf.Max(0.01f, cfg.zoomInicial), 1f, suave);
                Vector3 sube = new Vector3(0f, Mathf.Lerp(cfg.desplazamientoInicial, 0f, suave), 0f);

                for (int v = 0; v < 4; v++)
                {
                    vertices[vi + v] = centro + (vertices[vi + v] - centro) * escala + sube;

                    Color32 col = colores[vi + v];
                    col.a = (byte)Mathf.RoundToInt(col.a * suave);
                    colores[vi + v] = col;
                }
            }

            SubirMallaAlTexto(texto, info);
            yield return null;

            if (saltear != null && saltear()) break; // apretaron para apurar
        }

        // Cierre prolijo: la malla vuelve a su estado original.
        texto.ForceMeshUpdate();
    }

    public static void SubirMallaAlTexto(TMP_Text texto, TMP_TextInfo info)
    {
        for (int m = 0; m < info.meshInfo.Length; m++)
        {
            if (info.meshInfo[m].mesh == null) continue;

            info.meshInfo[m].mesh.vertices = info.meshInfo[m].vertices;
            info.meshInfo[m].mesh.colors32 = info.meshInfo[m].colors32;
            texto.UpdateGeometry(info.meshInfo[m].mesh, m);
        }
    }
}
