#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DecorarZona1Parallax
{
    private const string NombreRaiz = "DECORACION PARALLAX GENERADA";

    [MenuItem("Not Home/Decoracion/Decorar Zona 1 con parallax")]
    public static void Crear()
    {
        Scene escena = SceneManager.GetActiveScene();
        if (escena.name != "Zona 1")
        {
            Debug.LogWarning("Abri la escena Zona 1 antes de generar la decoracion.");
            return;
        }

        SpriteRenderer referencia = BuscarReferencia();
        if (referencia == null || referencia.sprite == null)
        {
            Debug.LogError("No se encontro un Recurso_0 con SpriteRenderer para reutilizar.");
            return;
        }

        GameObject anterior = GameObject.Find(NombreRaiz);
        if (anterior != null) Object.DestroyImmediate(anterior);

        GameObject raiz = new GameObject(NombreRaiz);
        GameObject fondo = new GameObject("FONDO - PARALLAX SUAVE");
        GameObject frente = new GameObject("FRENTE - APOYADO SIN PARALLAX");
        fondo.transform.SetParent(raiz.transform);
        frente.transform.SetParent(raiz.transform);

        Vector3[] posicionesFondo =
        {
            new(-148f, -184f, 0f), new(-116f, -132f, 0f),
            new(-82f, -184f, 0f),  new(-49f, -111f, 0f),
            new(-20f, -168f, 0f),  new(-139f, -43f, 0f),
            new(-75f, 18f, 0f),    new(-24f, -23f, 0f)
        };

        Vector2[] escalasFondo =
        {
            new(0.48f, 0.38f), new(0.35f, 0.52f), new(0.55f, 0.34f), new(0.38f, 0.46f),
            new(0.46f, 0.32f), new(0.33f, 0.48f), new(0.51f, 0.31f), new(0.36f, 0.45f)
        };

        for (int i = 0; i < posicionesFondo.Length; i++)
        {
            GameObject go = CrearMancha("Fondo difuminado " + (i + 1), referencia, fondo.transform,
                posicionesFondo[i], escalasFondo[i], "Elementos 2 Detras", -4, 0.32f + (i % 3) * 0.06f);
            ParallaxSuave parallax = go.AddComponent<ParallaxSuave>();
            parallax.intensidadX = 0.045f + (i % 3) * 0.018f;
            parallax.intensidadY = 0.018f + (i % 2) * 0.012f;
        }

        // Estas siluetas quedan tocando limites del nivel y NO llevan parallax.
        CrearMancha("Primer plano - piso izquierdo", referencia, frente.transform,
            new Vector3(-151f, -199f, 0f), new Vector2(0.62f, 0.30f),
            "Delante de todo foreground", 1, 0.20f);
        CrearMancha("Primer plano - piso derecho", referencia, frente.transform,
            new Vector3(-34f, -199f, 0f), new Vector2(0.70f, 0.27f),
            "Delante de todo foreground", 1, 0.18f);
        CrearMancha("Primer plano - techo", referencia, frente.transform,
            new Vector3(-91f, 51f, 0f), new Vector2(0.78f, 0.24f),
            "Delante de todo foreground", 1, 0.16f, true);
        CrearMancha("Primer plano - pared derecha", referencia, frente.transform,
            new Vector3(17f, -92f, 0f), new Vector2(0.28f, 0.66f),
            "Delante de todo foreground", 1, 0.17f);

        Selection.activeGameObject = raiz;
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("Decoracion de Zona 1 creada: fondo con parallax suave y primer plano apoyado.");
    }

    private static SpriteRenderer BuscarReferencia()
    {
        foreach (SpriteRenderer sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            if (sr.name.StartsWith("Recurso_0") && sr.sprite != null) return sr;
        return null;
    }

    private static GameObject CrearMancha(string nombre, SpriteRenderer referencia, Transform padre,
        Vector3 posicion, Vector2 escala, string sortingLayer, int orden, float alfa, bool invertirY = false)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre);
        go.transform.position = posicion;
        go.transform.localScale = new Vector3(escala.x, invertirY ? -escala.y : escala.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = referencia.sprite;
        sr.sharedMaterial = referencia.sharedMaterial;
        sr.color = new Color(0f, 0.025f, 0.06f, alfa);
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = orden;
        sr.flipX = nombre.GetHashCode() % 2 == 0;
        return go;
    }
}
#endif
