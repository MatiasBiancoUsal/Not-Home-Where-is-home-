using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

#if UNITY_EDITOR
using UnityEditor;
#endif

// La fuente Not Home usa un atlas dinamico. En una computadora nueva el atlas puede
// llegar vacio y TMP no siempre agrega los caracteres antes del primer render, dejando
// los botones funcionando pero sin texto. Este inicializador fuerza la carga de los
// caracteres realmente usados sin depender de la Library o del cache de cada equipo.
public static class NotHomeFontRuntimeFix
{
    private const string NombreFuente = "Nothomefont";
    private static readonly Dictionary<TMP_FontAsset, TMP_FontAsset> FuentesRegeneradas =
        new Dictionary<TMP_FontAsset, TMP_FontAsset>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InicializarRuntime()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;

        // Con "Reload Scene" desactivado la escena inicial no vuelve a cargarse y,
        // por lo tanto, sceneLoaded no se dispara. Preparamos lo que ya esta abierto.
        PrepararTextos(Resources.FindObjectsOfTypeAll<TMP_Text>(), true);
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        PrepararTextos(Resources.FindObjectsOfTypeAll<TMP_Text>(), true);
    }

    private static void PrepararTextos(IEnumerable<TMP_Text> textos, bool regenerarFuente)
    {
        List<TMP_Text> lista = new List<TMP_Text>(textos);
        HashSet<TMP_FontAsset> fuentesActualizadas = new HashSet<TMP_FontAsset>();

        foreach (TMP_Text texto in lista)
        {
            if (texto == null || !texto.gameObject.scene.IsValid() || texto.font == null ||
                !EsFuenteNotHome(texto.font)) continue;

            TMP_FontAsset fuente = texto.font;
            if (regenerarFuente)
            {
                fuente = ObtenerFuenteRegenerada(texto.font);
                if (fuente != null) texto.font = fuente;
            }

            if (fuentesActualizadas.Add(fuente))
            {
                string caracteres = CaracteresUsadosPor(lista);
                if (!string.IsNullOrEmpty(caracteres))
                {
                    fuente.TryAddCharacters(caracteres, out _);
                }
            }

            texto.SetVerticesDirty();
            texto.SetLayoutDirty();
            texto.ForceMeshUpdate(true, true);
        }
    }

    private static TMP_FontAsset ObtenerFuenteRegenerada(TMP_FontAsset original)
    {
        if (FuentesRegeneradas.TryGetValue(original, out TMP_FontAsset existente)) return existente;
        if (original.sourceFontFile == null) return original;

        TMP_FontAsset nueva = TMP_FontAsset.CreateFontAsset(
            original.sourceFontFile,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            true);

        if (nueva == null) return original;
        nueva.name = NombreFuente + " Runtime";
        FuentesRegeneradas.Add(original, nueva);
        return nueva;
    }

    private static string CaracteresUsadosPor(IEnumerable<TMP_Text> textos)
    {
        HashSet<char> caracteres = new HashSet<char>();

        foreach (TMP_Text texto in textos)
        {
            if (texto == null || string.IsNullOrEmpty(texto.text)) continue;
            foreach (char caracter in texto.text) caracteres.Add(caracter);
        }

        char[] resultado = new char[caracteres.Count];
        caracteres.CopyTo(resultado);
        return new string(resultado);
    }

    private static bool EsFuenteNotHome(TMP_FontAsset fuente)
    {
        return fuente.faceInfo.familyName == NombreFuente ||
               fuente.name.StartsWith(NombreFuente, System.StringComparison.OrdinalIgnoreCase);
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void InicializarEditor()
    {
        EditorApplication.delayCall += PrepararEscenaEnEditor;
        EditorApplication.hierarchyChanged -= PrepararEscenaEnEditor;
        EditorApplication.hierarchyChanged += PrepararEscenaEnEditor;
    }

    private static void PrepararEscenaEnEditor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        PrepararTextos(Resources.FindObjectsOfTypeAll<TMP_Text>(), false);
        SceneView.RepaintAll();
    }
#endif
}
