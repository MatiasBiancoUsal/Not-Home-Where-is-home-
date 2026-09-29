#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// ============================================================
//  CREAR EL DIBUJO DEL HAZ DE LUZ (solo editor)
//
//  Genera un PNG con un degradado: opaco abajo y transparente arriba, con los
//  costados suavizados. Sirve como "cookie" de una Light 2D de tipo Sprite, que es
//  la forma mas facil de hacer un faro con degradado.
//
//  Menu:  Not Home > Boss > Crear dibujo del haz de luz
// ============================================================
public static class MenuHazDeLuz
{
    private const string CARPETA = "Assets/--==BOSS FINAL";
    private const string RUTA = CARPETA + "/HazDeLuz.png";

    private const int ANCHO = 256;
    private const int ALTO = 512;

    [MenuItem("Not Home/Boss/Crear dibujo del haz de luz", false, 80)]
    private static void Crear()
    {
        Texture2D tex = new Texture2D(ANCHO, ALTO, TextureFormat.RGBA32, false);

        for (int y = 0; y < ALTO; y++)
        {
            // 1 abajo, 0 arriba. Al cuadrado para que el degradado se sienta suave.
            float alturaNormalizada = 1f - (y / (float)(ALTO - 1));
            float verticalidad = alturaNormalizada * alturaNormalizada;

            for (int x = 0; x < ANCHO; x++)
            {
                // Los costados tambien se desvanecen, asi el haz no tiene bordes duros.
                float centro = Mathf.Abs((x / (float)(ANCHO - 1)) - 0.5f) * 2f;   // 0 centro, 1 borde
                float horizontal = 1f - Mathf.SmoothStep(0f, 1f, centro);

                float alpha = verticalidad * horizontal;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();

        if (!AssetDatabase.IsValidFolder(CARPETA))
        {
            Debug.LogError("[Haz de luz] No encontre la carpeta " + CARPETA);
            return;
        }

        System.IO.File.WriteAllBytes(RUTA, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(RUTA, ImportAssetOptions.ForceUpdate);
        ConfigurarComoSprite();

        Object creado = AssetDatabase.LoadAssetAtPath<Object>(RUTA);
        Selection.activeObject = creado;
        EditorGUIUtility.PingObject(creado);

        Debug.Log("[Haz de luz] Listo: " + RUTA + ". Ponelo en una Light 2D con Light Type = Sprite.");
    }

    private static void ConfigurarComoSprite()
    {
        TextureImporter imp = AssetImporter.GetAtPath(RUTA) as TextureImporter;
        if (imp == null) return;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100f;
        imp.filterMode = FilterMode.Bilinear;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.wrapMode = TextureWrapMode.Clamp;

        imp.SaveAndReimport();
    }
}
#endif
