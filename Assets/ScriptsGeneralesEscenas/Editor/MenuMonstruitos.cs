#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// ============================================================
//  CREAR O COMPLETAR LOS PREFABS DE LOS MONSTRUITOS (solo editor)
//
//  Si el prefab NO existe, lo crea de cero (con un cuadrado de color).
//  Si YA existe (por ejemplo porque lo armaste arrastrando tus dibujos), NO te toca
//  el dibujo ni la animacion: solo le agrega lo que le falte para funcionar
//  (vida, collider, fisica, el script MiniBicho y la animacion de muerte).
//
//  Menu:  Not Home > Boss > Crear o completar los monstruitos
// ============================================================
public static class MenuMonstruitos
{
    private const string CARPETA = "Assets/--==BOSS FINAL/Monstruitos";
    private const string RUTA_MUERTE = "Assets/PreFabs/EfectoMuerte.prefab";

    // El mismo layer que usan los enemigos del juego (el que golpea el ataque de la niña).
    private const int LAYER_ENEMIGOS = 9;

    [MenuItem("Not Home/Boss/Crear o completar los monstruitos", false, 81)]
    private static void Preparar()
    {
        if (!AssetDatabase.IsValidFolder(CARPETA))
        {
            AssetDatabase.CreateFolder("Assets/--==BOSS FINAL", "Monstruitos");
        }

        GameObject efectoMuerte = AssetDatabase.LoadAssetAtPath<GameObject>(RUTA_MUERTE);
        if (efectoMuerte == null)
        {
            Debug.LogWarning("[Monstruitos] No encontre " + RUTA_MUERTE + ". Sigo sin animacion de muerte.");
        }

        Preparar(TipoDeBicho.Rastrero, "Rastrero", new Color(0.35f, 0.85f, 0.35f), new Vector2(0.9f, 0.6f), efectoMuerte);
        Preparar(TipoDeBicho.Volador,  "Volador",  new Color(0.35f, 0.8f, 1f),    new Vector2(0.6f, 0.6f), efectoMuerte);
        Preparar(TipoDeBicho.Saltarin, "Saltarin", new Color(1f, 0.6f, 0.2f),     new Vector2(0.7f, 0.8f), efectoMuerte);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Monstruitos] Listo. Los 3 prefabs de " + CARPETA + " ya tienen todo lo que necesitan.");
    }

    private static void Preparar(TipoDeBicho tipo, string nombre, Color color, Vector2 tamanio, GameObject efectoMuerte)
    {
        string ruta = CARPETA + "/" + nombre + ".prefab";
        GameObject existente = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);

        // ---------- Ya existe: lo completamos sin tocar el dibujo ----------
        if (existente != null)
        {
            GameObject copia = PrefabUtility.LoadPrefabContents(ruta);
            Configurar(copia, tipo, color, efectoMuerte, false);
            PrefabUtility.SaveAsPrefabAsset(copia, ruta);
            PrefabUtility.UnloadPrefabContents(copia);

            Debug.Log("[Monstruitos] '" + nombre + "': completado (tu dibujo y tu animacion quedan igual).");
            return;
        }

        // ---------- No existe: lo creamos con un cuadrado de color ----------
        GameObject go = new GameObject(nombre);
        go.transform.localScale = new Vector3(tamanio.x, tamanio.y, 1f);
        go.AddComponent<SpriteRenderer>().color = color;

        Configurar(go, tipo, color, efectoMuerte, true);

        PrefabUtility.SaveAsPrefabAsset(go, ruta);
        Object.DestroyImmediate(go);

        Debug.Log("[Monstruitos] '" + nombre + "': creado de cero (cuadrado de prueba).");
    }

    // Agrega SOLO lo que falte y deja los valores en su lugar.
    private static void Configurar(GameObject go, TipoDeBicho tipo, Color color, GameObject efectoMuerte, bool esNuevo)
    {
        go.layer = LAYER_ENEMIGOS;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();

        Collider2D col = go.GetComponent<Collider2D>();
        if (col == null)
        {
            BoxCollider2D caja = go.AddComponent<BoxCollider2D>();

            // Del tamaño del dibujo, si es que ya tiene uno.
            caja.size = sr.sprite != null ? (Vector2)sr.sprite.bounds.size : Vector2.one;
        }

        MiniBicho bicho = go.GetComponent<MiniBicho>();
        if (bicho == null) bicho = go.AddComponent<MiniBicho>();

        bicho.tipo = tipo;
        bicho.golpesQueAguanta = 3;
        bicho.prefabDeLaMuerte = efectoMuerte;
        bicho.mostrarPufAlMorir = esNuevo;   // con dibujo propio alcanza con la animacion
        if (esNuevo) bicho.colorDelPuf = color;

        HealthHandler vida = go.GetComponent<HealthHandler>();
        if (vida == null) vida = go.AddComponent<HealthHandler>();
        vida.maxHealth = 3;
        vida.destroyOnDeath = false;

        Damageable dan = go.GetComponent<Damageable>();
        if (dan == null) dan = go.AddComponent<Damageable>();
        dan.activeKnockBack = false;
        dan.activeInvulnerability = false;

        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb == null) rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = tipo == TipoDeBicho.Volador ? 0f : 3f;
    }
}
#endif
