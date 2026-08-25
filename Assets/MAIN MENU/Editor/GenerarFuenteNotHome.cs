using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

[InitializeOnLoad]
public static class GenerarFuenteNotHome
{
    private const string RutaTtf = "Assets/MAIN MENU/Font/Nothomefont-Regular.ttf";
    private const string RutaNueva = "Assets/MAIN MENU/Font/Nothomefont-Regular SDF Fixed.asset";

    static GenerarFuenteNotHome()
    {
        EditorApplication.delayCall += GenerarSiHaceFalta;
        EditorApplication.playModeStateChanged -= AlCambiarPlayMode;
        EditorApplication.playModeStateChanged += AlCambiarPlayMode;
    }

    private static void AlCambiarPlayMode(PlayModeStateChange estado)
    {
        if (estado == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += GenerarSiHaceFalta;
    }

    private static void GenerarSiHaceFalta()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RutaNueva) != null) return;

        Font fuenteOriginal = AssetDatabase.LoadAssetAtPath<Font>(RutaTtf);
        if (fuenteOriginal == null)
        {
            Debug.LogError("No se pudo cargar la fuente original Not Home.");
            return;
        }

        TMP_FontAsset fuenteNueva = TMP_FontAsset.CreateFontAsset(
            fuenteOriginal, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
            AtlasPopulationMode.Dynamic, true);

        if (fuenteNueva == null)
        {
            Debug.LogError("TextMeshPro no pudo generar el nuevo atlas de Not Home.");
            return;
        }

        const string caracteres =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            "[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "áéíóúüñÁÉÍÓÚÜÑ¿¡";

        if (!fuenteNueva.TryAddCharacters(caracteres, out string faltantes))
        {
            Debug.LogWarning("Not Home no contiene estos caracteres: " + faltantes);
        }

        fuenteNueva.name = "Nothomefont-Regular SDF Fixed";
        fuenteNueva.atlasPopulationMode = AtlasPopulationMode.Static;
        fuenteNueva.material.name = "Nothomefont-Regular Atlas Material Fixed";
        fuenteNueva.atlasTexture.name = "Nothomefont-Regular Atlas Fixed";
        fuenteNueva.atlasTexture.Apply(false, false);

        AssetDatabase.CreateAsset(fuenteNueva, RutaNueva);
        AssetDatabase.AddObjectToAsset(fuenteNueva.material, fuenteNueva);
        AssetDatabase.AddObjectToAsset(fuenteNueva.atlasTexture, fuenteNueva);
        EditorUtility.SetDirty(fuenteNueva);
        EditorUtility.SetDirty(fuenteNueva.material);
        EditorUtility.SetDirty(fuenteNueva.atlasTexture);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(RutaNueva, ImportAssetOptions.ForceUpdate);

        Debug.Log("Fuente Not Home reparada: se genero un atlas SDF nuevo y persistente.");
    }
}
