using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

[InitializeOnLoad]
public static class GenerarRobotoPuntaje
{
    private const string RutaTtf = "Assets/UI ESCENA JUEGO/SistemaPuntos/Font/Roboto-Regular.ttf";
    private const string RutaAsset = "Assets/UI ESCENA JUEGO/SistemaPuntos/Font/Roboto-Regular SDF.asset";
    private const string RutaPrefab = "Assets/PreFabs/BORRAR DESPUES Variant.prefab";

    static GenerarRobotoPuntaje()
    {
        EditorApplication.delayCall += PrepararFuenteYPuntaje;
    }

    private static void PrepararFuenteYPuntaje()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RutaAsset);
        if (fuente == null)
        {
            Font ttf = AssetDatabase.LoadAssetAtPath<Font>(RutaTtf);
            if (ttf == null) return;

            fuente = TMP_FontAsset.CreateFontAsset(
                ttf, 90, 9, GlyphRenderMode.SDFAA, 512, 512,
                AtlasPopulationMode.Static, true);

            if (fuente == null) return;

            fuente.name = "Roboto-Regular SDF";
            fuente.material.name = "Roboto-Regular Atlas Material";
            fuente.atlasTexture.name = "Roboto-Regular Atlas";
            AssetDatabase.CreateAsset(fuente, RutaAsset);
            AssetDatabase.AddObjectToAsset(fuente.material, fuente);
            AssetDatabase.AddObjectToAsset(fuente.atlasTexture, fuente);
            AssetDatabase.SaveAssets();
        }

        GameObject prefab = PrefabUtility.LoadPrefabContents(RutaPrefab);
        if (prefab == null) return;

        bool cambio = false;
        foreach (TMP_Text texto in prefab.GetComponentsInChildren<TMP_Text>(true))
        {
            if (texto.gameObject.name != "ScoreText" || texto.font == fuente) continue;
            texto.font = fuente;
            EditorUtility.SetDirty(texto);
            cambio = true;
        }

        if (cambio) PrefabUtility.SaveAsPrefabAsset(prefab, RutaPrefab);
        PrefabUtility.UnloadPrefabContents(prefab);
    }
}
