using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InicializarRuntime()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        PrepararTextos(Resources.FindObjectsOfTypeAll<TMP_Text>());
    }

    private static void PrepararTextos(IEnumerable<TMP_Text> textos)
    {
        HashSet<TMP_FontAsset> fuentesActualizadas = new HashSet<TMP_FontAsset>();

        foreach (TMP_Text texto in textos)
        {
            if (texto == null || texto.font == null || !EsFuenteNotHome(texto.font)) continue;

            if (fuentesActualizadas.Add(texto.font))
            {
                string caracteres = CaracteresUsadosPor(textos, texto.font);
                if (!string.IsNullOrEmpty(caracteres))
                {
                    texto.font.TryAddCharacters(caracteres, out _);
                }
            }

            texto.SetVerticesDirty();
            texto.SetLayoutDirty();
            texto.ForceMeshUpdate(true, true);
        }
    }

    private static string CaracteresUsadosPor(IEnumerable<TMP_Text> textos, TMP_FontAsset fuente)
    {
        HashSet<char> caracteres = new HashSet<char>();

        foreach (TMP_Text texto in textos)
        {
            if (texto == null || texto.font != fuente || string.IsNullOrEmpty(texto.text)) continue;
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
        PrepararTextos(Resources.FindObjectsOfTypeAll<TMP_Text>());
        SceneView.RepaintAll();
    }
#endif
}
